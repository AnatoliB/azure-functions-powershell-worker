//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Durable
{
    using System;
    using System.Management.Automation;

    internal class OrchestrationFailureException : RuntimeException
    {
        public OrchestrationFailureException()
        {
        }

        public OrchestrationFailureException(string message) : base(message)
        {
        }

        public OrchestrationFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
