//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Durable
{
    /// <summary>
    /// An orchestration action that represents calling an activity function with retry.
    /// </summary>
    internal class CallActivityWithRetryAction : OrchestrationAction
    {
        /// <summary>
        /// The activity function name.
        /// </summary>
        public readonly string FunctionName;
        
        /// <summary>
        /// The input to the activity function.
        /// </summary>
        public readonly object Input;

        public readonly RetryOptions RetryOptions;

        public CallActivityWithRetryAction(string functionName, object input, RetryOptions retryOptions)
            : base(ActionType.CallActivityWithRetry)
        {
            FunctionName = functionName;
            Input = input;
            RetryOptions = retryOptions;
        }
    }
}
