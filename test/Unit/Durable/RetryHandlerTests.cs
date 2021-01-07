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

        [Fact]
        public void DoesNotRetryIfReachedMaxNumberOfAttempts()
        {
            const int MaxNumberOfAttempts = 3;
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(1), MaxNumberOfAttempts, null, null, null);
            var history = CreateActivityRetriesHistory("ActivityName", MaxNumberOfAttempts, lastAttemptSucceeded: false, "Reason");
            var shouldRetry = RetryHandler.ShouldRetry(history, retryOptions);
            Assert.False(shouldRetry);
        }

        private HistoryEvent[] CreateActivityRetriesHistory(string name, int performedAttempts, bool lastAttemptSucceeded, string output)
        {
            var result = new HistoryEvent[0];

            for (var attempt = 0; attempt < performedAttempts; ++attempt)
            {
                var isLastAttempt = attempt == performedAttempts - 1;
                var next = CreateActivityAttemptHistory(name, isLastAttempt && lastAttemptSucceeded, output);
                result = DurableTestUtilities.MergeHistories(result, next);
            }

            return result;
        }

        private HistoryEvent[] CreateActivityAttemptHistory(string name, bool succeeded, string output)
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
                        Result = output
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
                        Reason = output
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
