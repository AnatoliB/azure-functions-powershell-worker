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
        public TimeSpan FirstRetryInterval { get; }

        public RetryOptions(TimeSpan firstRetryInterval, int maxNumberOfAttempts)
        {
            FirstRetryInterval = firstRetryInterval;
            MaxNumberOfAttempts = maxNumberOfAttempts;
        }

        public int MaxNumberOfAttempts { get; }
    }
}
