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
            var (history, firstEventIndex, numberOfEvents) = CreateFailureHistory(performedAttempts, attempt => "failure reason", replay: false);

            AssertRetryProcessorReportsRetry(history, firstEventIndex, maxNumberOfAttempts);
            AssertRelevantEventsProcessed(history, firstEventIndex, numberOfEvents);
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
            var (history, firstEventIndex, numberOfEvents) = CreateFailureHistory(performedAttempts, attempt => $"failure reason {attempt}", replay);

            AssertRetryProcessorReportsFailure(history, firstEventIndex, performedAttempts, "failure reason 1");
            AssertRelevantEventsProcessed(history, firstEventIndex, numberOfEvents);
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
            var (history, firstEventIndex, numberOfEvents) = CreateSuccessHistory(performedAttempts, SuccessOutput, replay);

            AssertRetryProcessorReportsSuccess(history, firstEventIndex, maxNumberOfAttempts, SuccessOutput);
            AssertRelevantEventsProcessed(history, firstEventIndex, numberOfEvents);
        }

        private readonly HistoryEvent[] InterleavingHistory = new[]
        {
            new HistoryEvent { EventType = HistoryEventType.TaskScheduled, EventId = 1 },                                        // A
            new HistoryEvent { EventType = HistoryEventType.TaskScheduled, EventId = 2 },                                        // B
            new HistoryEvent { EventType = HistoryEventType.TaskScheduled, EventId = 3 },                                        // C
            new HistoryEvent { EventType = HistoryEventType.TaskFailed,    EventId = -1, TaskScheduledId = 1, Reason = "A1" },   // A
            new HistoryEvent { EventType = HistoryEventType.TimerCreated,  EventId = 4 },                                        // A
            new HistoryEvent { EventType = HistoryEventType.TaskFailed,    EventId = -1, TaskScheduledId = 2, Reason = "B1" },   // B
            new HistoryEvent { EventType = HistoryEventType.TimerCreated,  EventId = 5 },                                        // B
            new HistoryEvent { EventType = HistoryEventType.TimerFired,    EventId = -1, TimerId = 4 },                          // A
            new HistoryEvent { EventType = HistoryEventType.TaskScheduled, EventId = 6 },                                        // A
            new HistoryEvent { EventType = HistoryEventType.TimerFired,    EventId = -1, TimerId = 5 },                          // B
            new HistoryEvent { EventType = HistoryEventType.TaskScheduled, EventId = 7 },                                        // B
            new HistoryEvent { EventType = HistoryEventType.TaskCompleted, EventId = -1, TaskScheduledId = 6, Result = "OK" },   // A
            new HistoryEvent { EventType = HistoryEventType.TaskFailed,    EventId = -1, TaskScheduledId = 7, Reason = "B2" },   // B
            new HistoryEvent { EventType = HistoryEventType.TimerCreated,  EventId = 8 },                                        // B
            new HistoryEvent { EventType = HistoryEventType.TimerFired,    EventId = -1, TimerId = 8 },                          // B
            new HistoryEvent { EventType = HistoryEventType.TaskFailed,    EventId = -1, TaskScheduledId = 3, Reason = "C1" },   // C
        };

        [Fact]
        public void InterleavingRetries_RequestsRetry()
        {
            AssertRetryProcessorReportsRetry(InterleavingHistory, firstEventIndex: 2, maxNumberOfAttempts: 2);
        }

        [Fact]
        public void InterleavingRetries_ReportsFailure()
        {
            AssertRetryProcessorReportsFailure(InterleavingHistory, firstEventIndex: 0, maxNumberOfAttempts: 2, "A1");
        }

        [Fact]
        public void InterleavingRetries_ReportsSuccess()
        {
            AssertRetryProcessorReportsSuccess(InterleavingHistory, firstEventIndex: 1, maxNumberOfAttempts: 2, "OK");
        }

        private Tuple<HistoryEvent[], int, int> CreateFailureHistory(
            int performedAttempts,
            Func<int, string> getFailureReason,
            bool replay)
        {
            var preHistory = CreateIrrelevantHistory();

            var relevantHistory = new HistoryEvent[0];
            for (var attempt = 1; attempt <= performedAttempts; ++attempt)
            {
                bool isLastAttempt = attempt == performedAttempts;
                bool includeTimerEvents = replay || !isLastAttempt;

                var next = CreateSingleFailureHistory(includeTimerEvents, getFailureReason(attempt));

                relevantHistory = DurableTestUtilities.MergeHistories(relevantHistory, next);
            }

            var postHistory = replay ? CreateIrrelevantHistory() : new HistoryEvent[0];

            var history = DurableTestUtilities.MergeHistories(preHistory, relevantHistory, postHistory);
            return Tuple.Create(history, preHistory.Length, relevantHistory.Length);
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

        private Tuple<HistoryEvent[], int, int> CreateSuccessHistory(
            int performedAttempts,
            string successOutput,
            bool replay)
        {
            var preHistory = CreateIrrelevantHistory();

            var relevantHistory = new HistoryEvent[0];
            for (var attempt = 1; attempt <= performedAttempts; ++attempt)
            {
                bool isLastAttempt = attempt == performedAttempts;

                var next = isLastAttempt
                                ? CreateSingleSuccessHistory(successOutput)
                                : CreateSingleFailureHistory(includeTimerEvents: true, "dummy failure reason");

                relevantHistory = DurableTestUtilities.MergeHistories(relevantHistory, next);
            }

            var postHistory = replay ? CreateIrrelevantHistory() : new HistoryEvent[0];

            var history = DurableTestUtilities.MergeHistories(preHistory, relevantHistory, postHistory);
            return Tuple.Create(history, preHistory.Length, relevantHistory.Length);
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

        private HistoryEvent[] CreateIrrelevantHistory()
        {
            var history = new HistoryEvent[0];
            for (var i = 0; i < 5; ++i)
            {
                history = DurableTestUtilities.MergeHistories(
                                history,
                                CreateSingleFailureHistory(includeTimerEvents: true, "failure reason"));
            }

            return history;
        }

        private int GetUniqueEventId()
        {
            return _nextEventId++;
        }

        private static void AssertRetryProcessorReportsRetry(HistoryEvent[] history, int firstEventIndex, int maxNumberOfAttempts)
        {
            var shouldRetry = RetryProcessor.Process(
                history,
                history[firstEventIndex],
                maxNumberOfAttempts,
                onSuccess: obj => { Assert.True(false, $"Unexpected output: {obj}"); },
                onFinalFailure: reason => { Assert.True(false, $"Unexpected failure: {reason}"); });

            Assert.True(shouldRetry);
        }

        private static void AssertRetryProcessorReportsFailure(HistoryEvent[] history, int firstEventIndex, int maxNumberOfAttempts, string ExpectedFailureReason)
        {
            string actualFailureReason = null;

            var shouldRetry = RetryProcessor.Process(
                history,
                history[firstEventIndex],
                maxNumberOfAttempts,
                onSuccess: obj => { Assert.True(false, $"Unexpected output: {obj}"); },
                onFinalFailure: reason =>
                {
                    Assert.Null(actualFailureReason);
                    actualFailureReason = reason;
                });

            Assert.False(shouldRetry);
            Assert.Equal(ExpectedFailureReason, actualFailureReason);
        }

        private static void AssertRetryProcessorReportsSuccess(HistoryEvent[] history, int firstEventIndex, int maxNumberOfAttempts, string expectedOutput)
        {
            object actualOutput = null;

            var shouldRetry = RetryProcessor.Process(
                history,
                history[firstEventIndex],
                maxNumberOfAttempts,
                onSuccess: obj =>
                {
                    Assert.Null(actualOutput);
                    actualOutput = obj;
                },
                onFinalFailure: reason => { Assert.True(false, $"Unexpected failure: {reason}"); });

            Assert.False(shouldRetry);
            Assert.Equal(expectedOutput, actualOutput);
        }

        private static void AssertRelevantEventsProcessed(HistoryEvent[] history, int firstEventIndex, int numberOfEvents)
        {
            // Expect all the relevant events to be processed
            AssertEventsProcessed(history, firstEventIndex, numberOfEvents, expectedProcessed: true);

            // Expect all the subsequent events NOT to be processed
            var firstIrrelevantEventIndex = firstEventIndex + numberOfEvents;
            var numberOfIrrelevantEvents = history.Length - firstIrrelevantEventIndex;
            AssertEventsProcessed(history, firstIrrelevantEventIndex, numberOfIrrelevantEvents, expectedProcessed: false);
        }

        private static void AssertEventsProcessed(HistoryEvent[] history, int firstEventIndex, int numberOfEvents, bool expectedProcessed)
        {
            Assert.True(history.Skip(firstEventIndex).Take(numberOfEvents).All(e => e.IsProcessed == expectedProcessed));
        }
    }
}
