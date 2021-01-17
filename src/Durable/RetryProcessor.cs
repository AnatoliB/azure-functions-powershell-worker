//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Durable
{
    using System;
    using System.Linq;

    internal class RetryProcessor
    {
        public static bool Process(
            HistoryEvent[] history,
            int maxNumberOfAttempts,
            Action<object> onSuccess,
            Action<string> onFailure)
        {
            var firstUnprocessedTaskScheduledEvent =
                history.First(e => !e.IsProcessed && e.EventType == HistoryEventType.TaskScheduled);

            return Process(history, firstUnprocessedTaskScheduledEvent, maxNumberOfAttempts, onSuccess, onFailure);
        }

        public static bool Process(
            HistoryEvent[] history,
            HistoryEvent firstTaskScheduledEvent,
            int maxNumberOfAttempts,
            Action<object> onSuccess,
            Action<string> onFailure)
        {
            var firstTaskScheduledEventIndex = FindEventIndex(history, firstTaskScheduledEvent);

            var attempts = 0;
            string firstFailureReason = null;
            for (var i = firstTaskScheduledEventIndex; i < history.Length; ++i)
            {
                var historyEvent = history[i];

                switch (historyEvent.EventType)
                {
                    case HistoryEventType.TaskFailed:
                        firstFailureReason ??= historyEvent.Reason;
                        attempts++;
                        if (attempts >= maxNumberOfAttempts)
                        {
                            onFailure(firstFailureReason);
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
