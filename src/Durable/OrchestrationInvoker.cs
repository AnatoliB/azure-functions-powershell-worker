﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Durable
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;

    using PowerShellWorker.Utility;

    internal class OrchestrationInvoker : IOrchestrationInvoker
    {
        public Hashtable Invoke(OrchestrationBindingInfo orchestrationBindingInfo, IPowerShellServices pwsh)
        {
            try
            {
                var outputBuffer = new PSDataCollection<object>();
                var asyncResult = pwsh.BeginInvoke(outputBuffer);

                var (shouldStop, actions) =
                    orchestrationBindingInfo.Context.OrchestrationActionCollector.WaitForActions(asyncResult.AsyncWaitHandle);

                if (shouldStop)
                {
                    // The orchestration function should be stopped and restarted
                    pwsh.StopInvoke();
                    //var orchestrationFailure = pwsh.GetErrorStream().FirstOrDefault(e => e.Exception is OrchestrationFailureException);
                    //if (orchestrationFailure == null)
                    //{
                    //    return CreateOrchestrationResult(isDone: false, actions, output: null);
                    //}
                    //else
                    //{
                    //    var result = FunctionReturnValueBuilder.CreateReturnValueFromFunctionOutput(outputBuffer);
                    //    return CreateOrchestrationResult(isDone: true, actions, output: result);
                    //}
                }
                else
                {
                    // The orchestration function completed
                    try
                    {
                        pwsh.EndInvoke(asyncResult);
                    }
                    catch (CmdletInvocationException ex)
                    {
                        if (!(ex.InnerException is OrchestrationFailureException))
                        {
                            throw;
                        }

                        var result2 = FunctionReturnValueBuilder.CreateReturnValueFromFunctionOutput(outputBuffer);
                        return CreateOrchestrationResult(isDone: true, actions, output: result2);
                    }

                    var result = FunctionReturnValueBuilder.CreateReturnValueFromFunctionOutput(outputBuffer);
                    return CreateOrchestrationResult(isDone: true, actions, output: result);
                }
            }
            finally
            {
                pwsh.ClearStreamsAndCommands();
            }
        }

        private static Hashtable CreateOrchestrationResult(
            bool isDone,
            List<OrchestrationAction> actions,
            object output)
        {
            var orchestrationMessage = new OrchestrationMessage(isDone, new List<List<OrchestrationAction>> { actions }, output);
            return new Hashtable { { AzFunctionInfo.DollarReturn, orchestrationMessage } };
        }
    }
}
