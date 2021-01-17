//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Test.Durable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Functions.PowerShellWorker.Durable;
    using Xunit;

    public class RetryProcessorTests
    {
        private int _nextEventId = 1;

        [Theory]
        [InlineData(2, 1)]
        [InlineData(3, 1)]
        [InlineData(3, 2)]
        [InlineData(100, 50)]
        [InlineData(100, 99)]
        public void RetriesAfterFailureWhenNotReachedMaxNumberOfAttempts(int maxNumberOfAttempts, int performedAttempts)
        {
            var history = CreateFailureHistory(performedAttempts, attempt => "failure reason", replay: false);

            var shouldRetry = RetryProcessor.Process(
                history,
                maxNumberOfAttempts,
                output: obj => { Assert.True(false, $"Unexpected output: {obj}"); },
                onFailure: reason => { Assert.True(false, $"Unexpected failure: {reason}"); });

            Assert.True(shouldRetry);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(3, false)]
        [InlineData(3, true)]
        [InlineData(100, false)]
        [InlineData(100, true)]
        public void ReportsFailureWhenReachedMaxNumberOfAttempts(int performedAttempts, bool replay)
        {
            var history = CreateFailureHistory(performedAttempts, attempt => $"failure reason {attempt}", replay);

            string actualFailureReason = null;

            var shouldRetry = RetryProcessor.Process(
                history,
                maxNumberOfAttempts: performedAttempts,
                output: obj => { Assert.True(false, $"Unexpected output: {obj}"); },
                onFailure: reason =>
                            {
                                Assert.Null(actualFailureReason);
                                actualFailureReason = reason;
                            });

            Assert.False(shouldRetry);
            Assert.Equal("failure reason 1", actualFailureReason);
        }

        [Theory]
        [InlineData(1, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(3, 1, false)]
        [InlineData(3, 1, true)]
        [InlineData(3, 2, false)]
        [InlineData(3, 2, true)]
        [InlineData(3, 3, false)]
        [InlineData(3, 3, true)]
        [InlineData(100, 50, false)]
        [InlineData(100, 50, true)]
        [InlineData(100, 100, false)]
        [InlineData(100, 100, true)]
        public void OutputsResultOnSuccess(int maxNumberOfAttempts, int performedAttempts, bool replay)
        {
            const string SuccessOutput = "success output";
            var history = CreateSuccessHistory(performedAttempts, SuccessOutput, replay);

            object actualOutput = null;

            var shouldRetry = RetryProcessor.Process(
                history,
                maxNumberOfAttempts,
                output: obj =>
                        {
                            Assert.Null(actualOutput);
                            actualOutput = obj;
                        },
                onFailure: reason => { Assert.True(false, $"Unexpected failure: {reason}"); });

            Assert.False(shouldRetry);
            Assert.Equal(SuccessOutput, actualOutput);
        }

        private HistoryEvent[] CreateFailureHistory(
            int performedAttempts,
            Func<int, string> getFailureReason,
            bool replay)
        {
            var result = new HistoryEvent[0];

            for (var attempt = 1; attempt <= performedAttempts; ++attempt)
            {
                bool isLastAttempt = attempt == performedAttempts;
                bool includeTimerEvents = replay || !isLastAttempt;

                var next = CreateSingleFailureHistory(includeTimerEvents, getFailureReason(attempt));

                result = DurableTestUtilities.MergeHistories(result, next);
            }

            return result;
        }

        private HistoryEvent[] CreateSingleFailureHistory(bool includeTimerEvents, string failureReason)
        {
            var taskScheduledEventId = GetUniqueEventId();

            var history =
                new List<HistoryEvent>
                    {
                        new HistoryEvent
                            {
                                EventType = HistoryEventType.TaskScheduled,
                                EventId = taskScheduledEventId,
                                IsProcessed = false
                            },
                        new HistoryEvent
                            {
                                EventType = HistoryEventType.TaskFailed,
                                EventId = -1,
                                TaskScheduledId = taskScheduledEventId,
                                Reason = failureReason,
                                IsProcessed = false
                            }
                    };

            if (includeTimerEvents)
            {
                int timerCreatedEventId = GetUniqueEventId();
                history.Add(
                    new HistoryEvent
                        {
                            EventType = HistoryEventType.TimerCreated,
                            EventId = timerCreatedEventId,
                            IsProcessed = false
                        });

                history.Add(
                    new HistoryEvent
                        {
                            EventType = HistoryEventType.TimerFired,
                            EventId = -1,
                            TimerId = timerCreatedEventId,
                            IsProcessed = false
                        });
            }

            return history.ToArray();
        }

        private HistoryEvent[] CreateSuccessHistory(
            int performedAttempts,
            string successOutput,
            bool replay)
        {
            var result = new HistoryEvent[0];

            for (var attempt = 1; attempt <= performedAttempts; ++attempt)
            {
                bool isLastAttempt = attempt == performedAttempts;

                var next = isLastAttempt
                                ? CreateSingleSuccessHistory(successOutput)
                                : CreateSingleFailureHistory(includeTimerEvents: true, "dummy failure reason");

                result = DurableTestUtilities.MergeHistories(result, next);
            }

            return result;
        }

        private HistoryEvent[] CreateSingleSuccessHistory(string output)
        {
            var taskScheduledEventId = GetUniqueEventId();

            var history =
                new List<HistoryEvent>
                    {
                        new HistoryEvent
                            {
                                EventType = HistoryEventType.TaskScheduled,
                                EventId = taskScheduledEventId,
                                IsProcessed = false
                            },
                        new HistoryEvent
                            {
                                EventType = HistoryEventType.TaskCompleted,
                                EventId = -1,
                                TaskScheduledId = taskScheduledEventId,
                                Result = output,
                                IsProcessed = false
                            }
                    };

            return history.ToArray();
        }

        private int GetUniqueEventId()
        {
            return _nextEventId++;
        }
    }
}
