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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class ThrowingStepFailStrategyTests
    {

        private const int RunawayGuardMilliseconds = 30000;

        private IServiceCollection _serviceCollection;

        [TestInitialize]
        public void Init() => _serviceCollection = BuildServiceCollection();

        [TestMethod]
        public async Task ThrowingStep_UnderStepRetry_IsRetriedRetryIterationsPlusOneTimes()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var throwingStep = new ThrowingCountingPipelineStep(3);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(throwingStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(4, throwingStep.ExecutionCount,
                "A thrown failure must be routed through StepRetry, so RetryIterations(3) + 1 = 4 executions are expected.");
            Assert.IsFalse(result.IsSuccess,
                "All retries were consumed by a throwing step, so the pipeline must report failure.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "All retries were consumed by a throwing step, so the pipeline status must be Fail.");

            var stepResults = result.StepResults
                .Where(x => x.StepName == nameof(ThrowingCountingPipelineStep))
                .ToList();

            Assert.AreEqual(4, stepResults.Count,
                $"Every attempt of the throwing step must be collected; got {stepResults.Count}.");
            Assert.AreEqual(PipelineFlowStepIterationType.FirstExecution, stepResults[0].StepIteration,
                "The first attempt of a throwing step must be labelled FirstExecution.");

            for (var i = 1; i < stepResults.Count; i++)
            {
                Assert.AreEqual(PipelineFlowStepIterationType.RetryExecution, stepResults[i].StepIteration,
                    $"Attempt index {i} of a throwing step must be labelled RetryExecution.");
            }
        }

        [TestMethod]
        public async Task ThrowingStep_ComparedWithReturnedFailureStep_ReceivesTheSameNumberOfExecutions()
        {

            const int retryIterations = 3;

            var throwingPerson = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var returningPerson = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            var throwingStep = new ThrowingCountingPipelineStep(retryIterations);
            var returningStep = new AlwaysFailCountingPipelineStep(retryIterations);

            var throwingServiceCollection = BuildServiceCollection();
            throwingServiceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();
            var throwingInvoker = throwingServiceCollection.BuildServiceProvider()
                .GetPipelineFlowEngineInvoker<PersonDto>();
            throwingInvoker.AddPipelineStep(throwingStep);

            var returningServiceCollection = BuildServiceCollection();
            returningServiceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();
            var returningInvoker = returningServiceCollection.BuildServiceProvider()
                .GetPipelineFlowEngineInvoker<PersonDto>();
            returningInvoker.AddPipelineStep(returningStep);

            var throwingResult = await throwingInvoker.InvokeAsync(throwingPerson);
            var returningResult = await returningInvoker.InvokeAsync(returningPerson);

            Assert.AreEqual(returningStep.ExecutionCount, throwingStep.ExecutionCount,
                "A THROWN failure and a RETURNED failure must consume the retry budget identically.");
            Assert.AreEqual(returningResult.StepResults.Count(), throwingResult.StepResults.Count(),
                "A THROWN failure and a RETURNED failure must produce the same number of collected step results.");
            Assert.AreEqual(returningResult.IsSuccess, throwingResult.IsSuccess,
                "A THROWN failure and a RETURNED failure must produce the same pipeline outcome.");
        }

        [TestMethod]
        public async Task ThrowingStep_UnderStepMoveToNext_IsAbsorbedAndLaterStepStillExecutes()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var throwingStep = new ThrowingCountingPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextMoveToNext>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(throwingStep);
            invoker.AddPipelineStep(lateStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(1, throwingStep.ExecutionCount,
                "StepMoveToNext must not retry, so the throwing step runs exactly once.");
            Assert.AreEqual(1, lateStep.ExecutionCount,
                "The step ordered after the throwing one must still execute under StepMoveToNext.");
            Assert.IsTrue(result.IsSuccess,
                "StepMoveToNext absorbed the thrown failure, so the pipeline must report success.");
        }

        [TestMethod]
        public async Task ThrowingStep_UnderPipelineStop_StopsPipelineAndRecordsTheFailedStepResult()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var throwingStep = new ThrowingCountingPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(throwingStep);
            invoker.AddPipelineStep(lateStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(1, throwingStep.ExecutionCount,
                "PipelineStop must not retry, so the throwing step runs exactly once.");
            Assert.AreEqual(0, lateStep.ExecutionCount,
                "PipelineStop must prevent every step ordered after the throwing one from executing.");
            Assert.IsFalse(result.IsSuccess,
                "A thrown failure under PipelineStop must fail the pipeline.");

            var throwingStepResult = result.StepResults
                .SingleOrDefault(x => x.StepName == nameof(ThrowingCountingPipelineStep));

            Assert.IsNotNull(throwingStepResult,
                "The thrown failure must be represented in StepResults, exactly like a returned failure.");
            Assert.IsFalse(throwingStepResult.StepResult.IsSuccess,
                "The step result built from the thrown exception must be a failed one.");
            Assert.AreEqual(ThrowingCountingPipelineStep.ThrownMessage, throwingStepResult.StepResult.Message,
                "The step result must carry the message of the exception thrown by the step.");
        }

        [TestMethod]
        [Timeout(RunawayGuardMilliseconds)]
        public async Task ThrowOnceThenSucceedStep_UnderStepRetry_RecoversAndReportsSuccess()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var recoveringStep = new ThrowsOnceThenSucceedsPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(recoveringStep);

            using var runawayGuard = new CancellationTokenSource(TimeSpan.FromMilliseconds(RunawayGuardMilliseconds));
            var result = await invoker.InvokeAsync(person, runawayGuard.Token);

            Assert.AreEqual(2, recoveringStep.ExecutionCount,
                "The step throws once and succeeds on the first retry, so exactly 2 executions are expected.");
            Assert.IsTrue(result.IsSuccess,
                "A pipeline that recovered on retry must report success — the caught exception must be recorded "
                + "at Warning, because an Error/Critical event keeps IsSuccess false forever.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                "A pipeline that recovered on retry must end with the Success status.");
            Assert.IsNotNull(result.FlowResponse,
                "A successful pipeline must expose the processed item.");
            Assert.AreEqual(ThrowsOnceThenSucceedsPipelineStep.RecoveredName, result.FlowResponse.Name,
                "The mutation applied by the successful retry attempt must be visible in the flow response.");
        }

        [TestMethod]
        public async Task StepCancellingThePipelineToken_UnderStepRetry_PropagatesOperationCanceledException()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            using var cts = new CancellationTokenSource();
            var cancelingStep = new CancelingPipelineStep(cts);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(cancelingStep);

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => invoker.InvokeAsync(person, cts.Token));

            Assert.AreEqual(1, cancelingStep.ExecutionCount,
                "Cancellation of the pipeline's own token must abort immediately and must never be retried.");
        }

        [TestMethod]
        public async Task StepThrowingForeignTokenCancellation_UnderStepRetry_IsTreatedAsStepFailureAndRetried()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var foreignCancellingStep = new ThrowingCountingPipelineStep(2, true);

            using var cts = new CancellationTokenSource();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(foreignCancellingStep);

            var result = await invoker.InvokeAsync(person, cts.Token);

            Assert.AreEqual(3, foreignCancellingStep.ExecutionCount,
                "A cancellation raised by a FOREIGN token is a step failure by design, so it is retried "
                + "RetryIterations(2) + 1 = 3 times.");
            Assert.IsFalse(result.IsSuccess,
                "The foreign cancellation exhausted the retries, so the pipeline must report failure.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "The foreign cancellation exhausted the retries, so the pipeline status must be Fail.");
        }

        private static IServiceCollection BuildServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory, LoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug));

            return serviceCollection;
        }
    }
}
