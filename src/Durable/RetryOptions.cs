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
        public readonly double firstRetryIntervalInMilliseconds;

        public readonly int maxNumberOfAttempts;

        public readonly double backoffCoefficient;

        public readonly double maxRetryIntervalInMilliseconds;

        public readonly double retryTimeoutInMilliseconds;

        public RetryOptions(
            TimeSpan firstRetryInterval,
            int maxNumberOfAttempts,
            double? backoffCoefficient,
            TimeSpan? maxRetryInterval,
            TimeSpan? retryTimeout)
        {
            this.firstRetryIntervalInMilliseconds = firstRetryInterval.TotalMilliseconds;
            this.maxNumberOfAttempts = maxNumberOfAttempts;
            this.backoffCoefficient = backoffCoefficient ?? 0;
            this.maxRetryIntervalInMilliseconds = maxRetryInterval.HasValue ? maxRetryInterval.Value.TotalMilliseconds : 0;
            this.retryTimeoutInMilliseconds = retryTimeout.HasValue ? retryTimeout.Value.TotalMilliseconds : 0;
        }
    }
}
