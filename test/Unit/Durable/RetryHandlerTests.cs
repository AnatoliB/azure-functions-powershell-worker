//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Test.Durable
{
    using System;
    using Microsoft.Azure.Functions.PowerShellWorker.Durable;
    using Xunit;

    public class RetryHandlerTests
    {
        [Fact]
        public void ShouldNotRetryIfNoRetryOptions()
        {
            var shouldRetry = RetryHandler.ShouldRetry(new HistoryEvent[0], retryOptions: null);
            Assert.False(shouldRetry);
        }

        [Fact]
        public void ShouldRetry()
        {
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(1), maxNumberOfAttempts: 3, null, null, null);
            var shouldRetry = RetryHandler.ShouldRetry(new HistoryEvent[0], retryOptions);
            Assert.True(shouldRetry);
        }
    }
}
