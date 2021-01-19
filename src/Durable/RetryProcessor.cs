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

            var attempt = 1;
            HistoryEvent taskScheduled = null;
            HistoryEvent taskFailed = null;
            HistoryEvent taskRetryTimer = null;
            for (var i = firstTaskScheduledEventIndex; i < history.Length; i++)
            {
                var historyEvent = history[i];
                if (historyEvent.IsProcessed)
                {
                    continue;
                }

                if (taskScheduled == null)
                {
                    if (historyEvent.EventType == HistoryEventType.TaskScheduled)
                    {
                        taskScheduled = historyEvent;
                    }
                    continue;
                }

                if (historyEvent.EventType == HistoryEventType.TaskCompleted)
                {
                    if (historyEvent.TaskScheduledId == taskScheduled.EventId)
                    {
                        taskScheduled.IsProcessed = true;
                        historyEvent.IsProcessed = true;
                        onSuccess(historyEvent.Result);
                        return false;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (taskFailed == null)
                {
                    if (historyEvent.EventType == HistoryEventType.TaskFailed)
                    {
                        if (historyEvent.TaskScheduledId == taskScheduled.EventId)
                        {
                            taskFailed = historyEvent;
                        }
                    }
                    continue;
                }

                if (taskRetryTimer == null)
                {
                    if (historyEvent.EventType == HistoryEventType.TimerCreated)
                    {
                        taskRetryTimer = historyEvent;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (historyEvent.EventType == HistoryEventType.TimerFired)
                {
                    if (historyEvent.TimerId == taskRetryTimer.EventId)
                    {
                        taskScheduled.IsProcessed = true;
                        taskFailed.IsProcessed = true;
                        taskRetryTimer.IsProcessed = true;
                        historyEvent.IsProcessed = true;
                        if (attempt >= maxNumberOfAttempts)
                        {
                            onFinalFailure(taskFailed.Reason);
                            return false;
                        }
                        else
                        {
                            attempt++;
                            taskScheduled = null;
                            taskFailed = null;
                            taskRetryTimer = null;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            return true;
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
