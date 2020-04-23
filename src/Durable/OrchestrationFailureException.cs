//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Durable
{
    using System;

    internal class OrchestrationFailureException : Exception
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
