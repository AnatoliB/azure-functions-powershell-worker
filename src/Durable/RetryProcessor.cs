//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Durable
{
    using System;

    internal class RetryProcessor
    {
        public static bool Process(
            HistoryEvent[] history,
            HistoryEvent firstTaskScheduledEvent,
            int maxNumberOfAttempts,
            Action<object> onSuccess,
            Action<string> onFinalFailure)
        {
            var firstTaskScheduledEventIndex = FindEventIndex(history, firstTaskScheduledEvent);

            var attempts = 0;
            string firstFailureReason = null;
            for (var i = firstTaskScheduledEventIndex; i < history.Length; ++i)
            {
                var historyEvent = history[i];
                historyEvent.IsProcessed = true;

                switch (historyEvent.EventType)
                {
                    case HistoryEventType.TaskFailed:
                        firstFailureReason ??= historyEvent.Reason;
                        attempts++;
                        if (attempts >= maxNumberOfAttempts)
                        {
                            if (i + 2 < history.Length)
                            {
                                history[i + 1].IsProcessed = true;
                                history[i + 2].IsProcessed = true;
                            }
                            onFinalFailure(firstFailureReason);
                            return false;
                        }
                        break;

                    case HistoryEventType.TaskCompleted:
                        onSuccess(historyEvent.Result);
                        return false;
                }
            }

            return attempts < maxNumberOfAttempts;
        }

        private static int FindEventIndex(HistoryEvent[] orchestrationHistory, HistoryEvent historyEvent)
        {
            var result = 0;
            foreach (var e in orchestrationHistory)
            {
                if (ReferenceEquals(historyEvent, e))
                {
                    return result;
                }

                result++;
            }
            
            return -1;
        }
    }
}
