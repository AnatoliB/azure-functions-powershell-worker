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

        [Fact]
        public void RetriesOnFirstFailureIfRetryOptionsAllow()
        {
            var history = CreateSingleAttemptHistory(succeeded: false);

            var shouldRetry = RetryProcessor.Process(
                history,
                maxNumberOfAttempts: 2,
                output: obj => { Assert.True(false, $"Unexpected output: {obj}"); },
                onFailure: reason => { Assert.True(false, $"Unexpected failure: {reason}"); });

            Assert.True(shouldRetry);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void DoesNotRetryOnFirstFailureIfRetryOptionsDoNotAllow(int maxNumberOfAttempts)
        {
            const string FailureReason = "failure reason";
            var history = CreateSingleAttemptHistory(succeeded: false, failureReason: FailureReason);

            string actualFailureReason = null;

            var shouldRetry = RetryProcessor.Process(
                history,
                maxNumberOfAttempts,
                output: obj => { Assert.True(false, $"Unexpected output: {obj}"); },
                onFailure: reason =>
                            {
                                Assert.Null(actualFailureReason);
                                actualFailureReason = reason;
                            });

            Assert.False(shouldRetry);
            Assert.Equal(FailureReason, actualFailureReason);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(2, 2)]
        [InlineData(100, 50)]
        [InlineData(100, 100)]
        public void DoesNotRetryOnSuccess(int maxNumberOfAttempts, int performedAttempts)
        {
            const string SuccessOutput = "success output";
            var history = CreateMultipleAttemptsHistory(
                            performedAttempts: performedAttempts,
                            lastAttemptSucceeded: true,
                            successOutput: SuccessOutput,
                            getFailureReason: _ => "dummy failure reason");

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

        private HistoryEvent[] CreateMultipleAttemptsHistory(
            int performedAttempts,
            bool lastAttemptSucceeded,
            Func<int, string> getFailureReason,
            string successOutput = null,
            bool processed = false)
        {
            var result = new HistoryEvent[0];

            for (var attempt = 1; attempt <= performedAttempts; ++attempt)
            {
                var isLastAttempt = attempt == performedAttempts;

                var next = CreateSingleAttemptHistory(
                                succeeded: isLastAttempt && lastAttemptSucceeded,
                                successOutput: successOutput,
                                failureReason: getFailureReason(attempt),
                                addTimerEvents: !isLastAttempt,
                                processed: processed);

                result = DurableTestUtilities.MergeHistories(result, next);
            }

            return result;
        }

        private HistoryEvent[] CreateSingleAttemptHistory(
            bool succeeded,
            string successOutput = null,
            string failureReason = null,
            bool addTimerEvents = false,
            bool processed = false)
        {
            var history = new List<HistoryEvent>();

            var taskScheduledEventId = GetUniqueEventId();

            history.Add(
                new HistoryEvent
                {
                    EventType = HistoryEventType.TaskScheduled,
                    EventId = taskScheduledEventId,
                    Name = "dummy activity name",
                    IsProcessed = processed
                });

            if (succeeded)
            {
                history.Add(
                    new HistoryEvent
                    {
                        EventType = HistoryEventType.TaskCompleted,
                        EventId = -1,
                        TaskScheduledId = taskScheduledEventId,
                        Result = successOutput,
                        IsProcessed = processed
                    });
            }
            else
            {
                history.Add(
                    new HistoryEvent
                    {
                        EventType = HistoryEventType.TaskFailed,
                        EventId = -1,
                        TaskScheduledId = taskScheduledEventId,
                        Reason = failureReason,
                        IsProcessed = processed
                    });

                if (addTimerEvents)
                {
                    int timerCreatedEventId = GetUniqueEventId();
                    history.Add(
                        new HistoryEvent
                        {
                            EventType = HistoryEventType.TimerCreated,
                            EventId = timerCreatedEventId,
                            IsProcessed = processed
                        });

                    history.Add(
                        new HistoryEvent
                        {
                            EventType = HistoryEventType.TimerFired,
                            EventId = -1,
                            TimerId = timerCreatedEventId,
                            IsProcessed = processed
                        });
                }
            }

            return history.ToArray();
        }

        private int GetUniqueEventId()
        {
            return _nextEventId++;
        }
    }
}
