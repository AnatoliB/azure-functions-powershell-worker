//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Durable
{
    using System.Linq;

    internal class RetryHandler
    {
        public static bool ShouldRetry(HistoryEvent[] orchestrationHistory, RetryOptions retryOptions)
        {
            var attempts = orchestrationHistory.Count(e => e.EventType == HistoryEventType.TaskFailed);
            return !(retryOptions == null || attempts >= retryOptions.MaxNumberOfAttempts);
        }
    }
}
