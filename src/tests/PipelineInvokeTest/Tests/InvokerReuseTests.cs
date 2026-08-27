#region U S I N G

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Pipelines;
using PipelineInvokeTest.Pipelines.Steps.Person;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class InvokerReuseTests
    {

        private const int RegisteredStepCount = 2;

        private const int ExpectedEventsPerInvocation = 2 + 3 + (2 * RegisteredStepCount) + 2;

        private IServiceCollection _serviceCollection;

        [TestInitialize]
        public void Init()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory, LoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug));

            _serviceCollection = serviceCollection;
        }

        [TestMethod]
        public async Task SameInvoker_InvokedTwice_DoesNotAccumulateStepResultsOrEvents()
        {

            var firstPerson = new PersonDto { Id = Guid.Empty, Name = "FirstName", IsActive = true };
            var secondPerson = new PersonDto { Id = Guid.Empty, Name = "SecondName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep), typeof(PersonSetIdPipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var firstResult = await invoker.InvokeAsync(firstPerson);
            var secondResult = await invoker.InvokeAsync(secondPerson);

            Assert.AreEqual(RegisteredStepCount, firstResult.StepResults.Count(),
                $"{RegisteredStepCount} steps were registered and both succeed on their first attempt, so the "
                + "first invocation must report exactly one step result per step.");
            Assert.AreEqual(RegisteredStepCount, secondResult.StepResults.Count(),
                "Per-invocation step results must be reset, not accumulated: the second invocation must report "
                + "its own step results only.");

            Assert.AreEqual(ExpectedEventsPerInvocation, firstResult.Events.Count(),
                $"A successful invocation of {RegisteredStepCount} steps must record exactly "
                + $"{ExpectedEventsPerInvocation} flow events.");
            Assert.AreEqual(ExpectedEventsPerInvocation, secondResult.Events.Count(),
                "Per-invocation events must be reset, not accumulated: the second invocation must record its "
                + "own events only.");
            Assert.AreEqual(firstResult.Events.Count(), secondResult.Events.Count(),
                "Both invocations executed the same steps with the same outcome, so they must record the same "
                + "number of flow events.");
            Assert.IsTrue(secondResult.IsSuccess,
                "The second invocation of the same invoker must still succeed.");
        }

        [TestMethod]
        public async Task PriorityStep_OnRepeatedInvocations_ExecutesExactlyOncePerInvocation()
        {

            var firstPerson = new PersonDto { Id = Guid.Empty, Name = "FirstName", IsActive = true };
            var secondPerson = new PersonDto { Id = Guid.Empty, Name = "SecondName", IsActive = true };
            var priorityStep = new ExecutionCountingPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(priorityStep, PipelineStepExecutionStrategyType.PriorityExecute);

            await invoker.InvokeAsync(firstPerson);
            var executionsAfterFirstInvocation = priorityStep.ExecutionCount;

            await invoker.InvokeAsync(secondPerson);
            var executionsAfterSecondInvocation = priorityStep.ExecutionCount;

            Assert.AreEqual(1, executionsAfterFirstInvocation,
                "The priority step must execute exactly once during the first invocation.");
            Assert.AreEqual(2, executionsAfterSecondInvocation,
                "The priority step must execute exactly once per invocation; a value of 3 means the priority "
                + "steps were duplicated into the queued collection by the first invocation.");
        }

        [TestMethod]
        public async Task SameInvoker_AfterAFailedInvocation_ReportsSuccessOnTheNextSuccessfulInvocation()
        {

            var firstPerson = new PersonDto { Id = Guid.Empty, Name = "FirstName", IsActive = true };
            var secondPerson = new PersonDto { Id = Guid.Empty, Name = "SecondName", IsActive = true };

            var failingOnceStep = new AlwaysFailCountingPipelineStep(0, 1);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(failingOnceStep);

            var failedResult = await invoker.InvokeAsync(firstPerson);
            var successfulResult = await invoker.InvokeAsync(secondPerson);

            Assert.IsFalse(failedResult.IsSuccess,
                "The first invocation must fail: a successful one would leave nothing for the second "
                + "invocation to be contaminated by.");
            Assert.AreEqual(PipelineStatusType.Success, successfulResult.Status,
                "The second invocation completed every step successfully, so its status must be Success.");
            Assert.IsTrue(successfulResult.IsSuccess,
                "Error events of a previous failed invocation must not leak into a later successful one.");
        }

        [TestMethod]
        public async Task SameInvoker_ReenteredWhileAnInvocationIsInFlight_ThrowsInvalidOperationException()
        {

            var firstPerson = new PersonDto { Id = Guid.Empty, Name = "FirstName", IsActive = true };
            var secondPerson = new PersonDto { Id = Guid.Empty, Name = "SecondName", IsActive = true };
            var reentrantStep = new ExecutionCountingPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(reentrantStep);

            Exception reentrantException = null;
            reentrantStep.WhileInFlightAsync = async () =>
            {
                try
                {
                    await invoker.InvokeAsync(secondPerson);
                }
                catch (Exception exception)
                {
                    reentrantException = exception;
                }
            };

            var result = await invoker.InvokeAsync(firstPerson);

            Assert.IsNotNull(reentrantException,
                "A second InvokeAsync started while the first one is in flight must not be allowed to run.");
            Assert.IsInstanceOfType(reentrantException, typeof(InvalidOperationException),
                $"Concurrent invocation must throw InvalidOperationException; got {reentrantException.GetType().Name}.");
            Assert.AreEqual(1, reentrantStep.ExecutionCount,
                "The rejected re-entrant invocation must not have executed any step.");
            Assert.IsTrue(result.IsSuccess,
                "The in-flight invocation must complete normally after the re-entrant one was rejected.");
        }

        [TestMethod]
        public async Task SameInvoker_AfterAnInvocationThrew_CanBeInvokedAgain()
        {

            var firstPerson = new PersonDto { Id = Guid.Empty, Name = "FirstName", IsActive = true };
            var secondPerson = new PersonDto { Id = Guid.Empty, Name = "SecondName", IsActive = true };

            using var cancellationSource = new CancellationTokenSource();
            var cancelingStep = new CancelingPipelineStep(cancellationSource);
            var succeedingStep = new ExecutionCountingPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(cancelingStep);

            var cancellation = await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                () => invoker.InvokeAsync(firstPerson, cancellationSource.Token));

            invoker.AddPipelineStep(succeedingStep, PipelineStepExecutionStrategyType.ForceExecute);

            using var secondCancellationSource = new CancellationTokenSource();
            var secondResult = await invoker.InvokeAsync(secondPerson, secondCancellationSource.Token);

            Assert.IsNotNull(cancellation,
                "The first invocation must throw: only a throwing invocation releases the re-entrancy guard "
                + "through `finally` instead of through the normal return path.");
            Assert.AreEqual(1, cancelingStep.ExecutionCount,
                "The canceling step must have run exactly once, during the first (throwing) invocation.");

            Assert.IsNotNull(secondResult,
                "The same invoker instance must accept a new invocation after a previous one threw: the "
                + "re-entrancy guard is released in a `finally`, so a throwing invocation must not leave it "
                + "acquired. A leaked guard would permanently brick the instance with InvalidOperationException "
                + "on every later, unrelated invocation.");
            Assert.AreEqual(1, succeedingStep.ExecutionCount,
                "The second invocation must actually execute its steps, not merely return: this proves the "
                + "pipeline body was entered again and not short-circuited by a leaked guard.");
            Assert.IsTrue(secondResult.IsSuccess,
                "The invocation following a thrown one must complete normally; no state from the cancelled "
                + "invocation may leak into it.");
            Assert.AreEqual(PipelineStatusType.Success, secondResult.Status,
                "The invocation following a thrown one completed every step successfully, so its status must "
                + "be Success.");
        }
    }
}
