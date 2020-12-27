//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member 'member'

using System;

namespace Microsoft.Azure.Functions.PowerShellWorker.Durable
{
    public class RetryOptions
    {
        public readonly int firstRetryIntervalInMilliseconds;

        public readonly int maxNumberOfAttempts;

        public readonly int backoffCoefficient;

        public readonly int maxRetryIntervalInMilliseconds;

        public readonly int retryTimeoutInMilliseconds;

        public RetryOptions(
            TimeSpan firstRetryInterval,
            int maxNumberOfAttempts,
            int backoffCoefficient,
            int maxRetryIntervalInMilliseconds,
            int retryTimeoutInMilliseconds)
        {
            firstRetryIntervalInMilliseconds = (int)firstRetryInterval.TotalMilliseconds;
            this.maxNumberOfAttempts = maxNumberOfAttempts;
            this.backoffCoefficient = backoffCoefficient;
            this.maxRetryIntervalInMilliseconds = maxRetryIntervalInMilliseconds;
            this.retryTimeoutInMilliseconds = retryTimeoutInMilliseconds;
        }
    }
}
