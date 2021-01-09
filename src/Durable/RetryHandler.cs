//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Durable
{
    using System;
    using System.Linq;

    internal class RetryHandler
    {
        public static bool ShouldRetry(
            HistoryEvent[] history,
            int maxNumberOfAttempts,
            Action<object> output,
            Action<string> onFailure)
        {
            var firstUnprocessedTaskScheduledEvent =
                history.First(e => !e.IsProcessed && e.EventType == HistoryEventType.TaskScheduled);

            return ShouldRetry(history, firstUnprocessedTaskScheduledEvent, maxNumberOfAttempts, output, onFailure);
        }

        public static bool ShouldRetry(
            HistoryEvent[] history,
            HistoryEvent firstTaskScheduledEvent,
            int maxNumberOfAttempts,
            Action<object> output,
            Action<string> onFailure)
        {
            var firstTaskScheduledEventIndex = FindEventIndex(history, firstTaskScheduledEvent);

            var attempts = 0;
            for (var i = firstTaskScheduledEventIndex; i < history.Length; ++i)
            {
                var historyEvent = history[i];

                switch (historyEvent.EventType)
                {
                    case HistoryEventType.TaskFailed:
                        attempts++;
                        if (attempts >= maxNumberOfAttempts)
                        {
                            onFailure(historyEvent.Reason);
                            return false;
                        }
                        break;

                    case HistoryEventType.TaskCompleted:
                        output(historyEvent.Result);
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
