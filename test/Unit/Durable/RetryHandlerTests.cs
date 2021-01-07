//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Test.Durable
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Functions.PowerShellWorker.Durable;
    using Xunit;

    public class RetryHandlerTests
    {
        private int _nextEventId = 1;

        [Fact]
        public void DoesNotRetryIfNoRetryOptions()
        {
            var shouldRetry = RetryHandler.ShouldRetry(new HistoryEvent[0], retryOptions: null);
            Assert.False(shouldRetry);
        }

        [Fact]
        public void RetriesIfRetryOptionsProvided()
        {
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(1), maxNumberOfAttempts: 3, null, null, null);
            var shouldRetry = RetryHandler.ShouldRetry(new HistoryEvent[0], retryOptions);
            Assert.True(shouldRetry);
        }

        [Theory]
        [InlineData(0, 0, false)]
        [InlineData(1, 0, false)]
        [InlineData(0, 1, true)]
        [InlineData(1, 1, false)]
        [InlineData(2, 1, false)]
        [InlineData(0, 2, true)]
        [InlineData(1, 2, true)]
        [InlineData(2, 2, false)]
        [InlineData(3, 2, false)]
        [InlineData(99, 100, true)]
        [InlineData(100, 100, false)]
        public void RetriesUntilMaxNumberOfAttempts(int performedAttempts, int maxAttempts, bool expectedRetry)
        {
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(1), maxAttempts, null, null, null);
            var history = CreateActivityRetriesHistory("ActivityName", performedAttempts, lastAttemptSucceeded: false);
            var shouldRetry = RetryHandler.ShouldRetry(history, retryOptions);
            Assert.Equal(expectedRetry, shouldRetry);
        }

        [Fact]
        public void MarksRelevantEventsAsProcessed()
        {
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(1), maxNumberOfAttempts: 3, null, null, null);
            var history = CreateActivityRetriesHistory("ActivityName", 2, lastAttemptSucceeded: false);
            RetryHandler.ShouldRetry(history, retryOptions);

            foreach (var historyEvent in history)
            {
                Assert.True(historyEvent.IsProcessed);
            }
        }

        private HistoryEvent[] CreateActivityRetriesHistory(string name, int performedAttempts, bool lastAttemptSucceeded)
        {
            var result = new HistoryEvent[0];

            for (var attempt = 0; attempt < performedAttempts; ++attempt)
            {
                var isLastAttempt = attempt == performedAttempts - 1;
                var next = CreateActivityAttemptHistory(name, isLastAttempt && lastAttemptSucceeded);
                result = DurableTestUtilities.MergeHistories(result, next);
            }

            return result;
        }

        private HistoryEvent[] CreateActivityAttemptHistory(string name, bool succeeded)
        {
            var history = new List<HistoryEvent>();

            var taskScheduledEventId = GetUniqueEventId();

            history.Add(
                new HistoryEvent
                {
                    EventType = HistoryEventType.TaskScheduled,
                    EventId = taskScheduledEventId,
                    Name = name
                });

            if (succeeded)
            {
                history.Add(
                    new HistoryEvent
                    {
                        EventType = HistoryEventType.TaskCompleted,
                        EventId = -1,
                        TaskScheduledId = taskScheduledEventId,
                        Result = "dummy result"
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
                        Reason = "dummy reason"
                    });
            }

            return history.ToArray();
        }

        private int GetUniqueEventId()
        {
            return _nextEventId++;
        }
    }
}
