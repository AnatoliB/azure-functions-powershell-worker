//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Azure.Functions.PowerShellWorker.Test.Durable
{
    using System;
    using System.Linq;
    using System.Threading;
    using Microsoft.Azure.Functions.PowerShellWorker.Durable;
    using Microsoft.Azure.Functions.PowerShellWorker.Durable.Actions;
    using Xunit;

    public class OrchestrationActionCollectorTests
    {
        [Fact]
        public void IndicatesShouldNotStopOnSignalledCompletionWaitHandle()
        {
            var orchestrationActionCollector = new OrchestrationActionCollector();
            var (shouldStop, _) = orchestrationActionCollector.WaitForActions(new AutoResetEvent(initialState: true));
            Assert.False(shouldStop);
        }

        [Fact]
        public void IndicatesShouldStopOnStopEvent()
        {
            var orchestrationActionCollector = new OrchestrationActionCollector();
            orchestrationActionCollector.Stop();
            var (shouldStop, _) = orchestrationActionCollector.WaitForActions(new AutoResetEvent(initialState: false));
            Assert.True(shouldStop);
        }

        [Fact]
        public void ReturnsNoActionsWhenNoneAdded()
        {
            var orchestrationActionCollector = new OrchestrationActionCollector();
            var (_, actions) = orchestrationActionCollector.WaitForActions(new AutoResetEvent(initialState: true));
            Assert.Empty(actions);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(100)]
        public void ReturnsActionsWhenAdded(int numberOfActions)
        {
            var orchestrationActionCollector = new OrchestrationActionCollector();
            
            var addedActions = Enumerable.Range(0, numberOfActions).Select(i => new CallActivityAction($"Function{i}", "Input{i}")).ToArray();
            foreach (var action in addedActions)
            {
                orchestrationActionCollector.Add(action);
            }

            var (_, actions) = orchestrationActionCollector.WaitForActions(new AutoResetEvent(initialState: true));

            Assert.Single(actions);
            Assert.Equal(addedActions, actions.Single());
        }
    }
}
