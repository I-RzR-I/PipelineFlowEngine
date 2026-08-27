#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Pipelines;
using PipelineInvokeTest.Pipelines.Steps.Person;
using RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{

    [TestClass]
    public class EventStreamTests
    {

        private const int RunawayGuardMilliseconds = 30000;

        private const string InitStepsText = "Initialize supplied steps";

        private const string TotalRegisteredStepsText = "In pipeline was registered";

        private const string StartExecutionStepText = "Start execution step";

        private const string PipelineFinishedText = "Pipeline finished work";

        private const string PipelineStoppedText = "pipeline stopped execution";

        private const string ExceptionMarkerText = "Error: [";

        private const string RetryAlreadyUsedText = "retry action was already used";

        private IServiceCollection _serviceCollection;

        [TestInitialize]
        public void Init() => _serviceCollection = BuildServiceCollection();

        [TestMethod]
        [Timeout(RunawayGuardMilliseconds)]
        public async Task RecoveredRetry_ProducesNoErrorOrCriticalFlowEvent()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var recoveringStep = new ThrowsOnceThenSucceedsPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(recoveringStep);

            using var runawayGuard = new CancellationTokenSource(TimeSpan.FromMilliseconds(RunawayGuardMilliseconds));
            var result = await invoker.InvokeAsync(person, runawayGuard.Token);

            Assert.IsTrue(result.IsSuccess,
                "A pipeline that recovered on retry must report success.");
            Assert.IsFalse(HasErrorOrCriticalEvent(result),
                "A RECOVERABLE failure must be recorded at Warning. IsSuccess is computed as "
                + "'Status == Success AND no Error/Critical event exists', so a single Error event would make "
                + "IsSuccess false for a run that actually succeeded.");
        }

        [TestMethod]
        public async Task MoveToNextAbsorbedFailure_ProducesNoErrorOrCriticalFlowEvent()
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

            Assert.IsTrue(result.IsSuccess,
                "StepMoveToNext absorbed the failure, so the pipeline must report success.");
            Assert.IsFalse(HasErrorOrCriticalEvent(result),
                "An ABSORBED failure must be recorded at Warning. IsSuccess is computed as "
                + "'Status == Success AND no Error/Critical event exists', so a single Error event would make "
                + "IsSuccess false for a run the fail strategy deliberately let continue.");
        }

        [TestMethod]
        public async Task ThrownStepFailure_IsRecordedAsWarningCarryingTheException()
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

            var exceptionEvents = result.Events
                .Where(x => x.Exception != null)
                .ToList();

            Assert.AreEqual(1, exceptionEvents.Count,
                $"Exactly one flow event must carry the exception thrown by the step; got {exceptionEvents.Count}.");
            Assert.AreEqual(LogLevel.Warning, exceptionEvents[0].EventLevel,
                "The event that carries a caught step exception is recorded at Warning: an Error- or "
                + "Critical-level event makes IsSuccess false even when the pipeline later recovers.");
            Assert.AreEqual(ThrowingCountingPipelineStep.ThrownMessage, exceptionEvents[0].Exception.Message,
                "The recorded event must carry the ORIGINAL exception instance thrown by the step.");
            Assert.IsFalse(HasErrorOrCriticalEvent(result),
                "A caught and absorbed step exception must not raise the event stream above Warning.");
        }

        [TestMethod]
        public async Task SuccessfulPipeline_EventStream_IsOrderedFromInitialisationToCompletion()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var firstStep = new ExecutionCountingPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(firstStep);
            invoker.AddPipelineStep(lateStep);

            var result = await invoker.InvokeAsync(person);

            var messages = result.Events.Select(x => x.Message).ToList();

            var initIndex = messages.FindIndex(x => x.Contains(InitStepsText));
            var registeredIndex = messages.FindIndex(x => x.Contains(TotalRegisteredStepsText));
            var firstStepIndex = messages.FindIndex(x => x.Contains(StartExecutionStepText));
            var finishedIndex = messages.FindIndex(x => x.Contains(PipelineFinishedText));

            Assert.IsTrue(initIndex >= 0,
                $"The event stream must contain the initialisation event '{InitStepsText}'.");
            Assert.IsTrue(registeredIndex >= 0,
                $"The event stream must contain the registration summary event '{TotalRegisteredStepsText}'.");
            Assert.IsTrue(firstStepIndex >= 0,
                $"The event stream must contain at least one step start event '{StartExecutionStepText}'.");
            Assert.IsTrue(finishedIndex >= 0,
                $"The event stream must contain the completion event '{PipelineFinishedText}'.");

            Assert.IsTrue(initIndex < registeredIndex,
                "The initialisation event must precede the registration summary — the invoker events are "
                + "appended to the result in the order they were produced.");
            Assert.IsTrue(registeredIndex < firstStepIndex,
                "The registration summary must precede the first step start event.");
            Assert.IsTrue(firstStepIndex < finishedIndex,
                "The first step start event must precede the pipeline completion event.");

            Assert.AreEqual(1, messages.Count(x => x.Contains(PipelineFinishedText)),
                "The pipeline completion event must be emitted exactly once per invocation.");
        }

        [TestMethod]
        public async Task FailedPipeline_UnderPipelineStop_EmitsExactlyOneErrorLevelEvent()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var failingStep = new AlwaysFailCountingPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(failingStep);

            var result = await invoker.InvokeAsync(person);

            var errorEvents = result.Events
                .Where(x => x.EventLevel == LogLevel.Error)
                .ToList();

            Assert.AreEqual(1, errorEvents.Count,
                $"A RETURNED failure under PipelineStop must raise exactly one Error event; got {errorEvents.Count}.");
            StringAssert.Contains(errorEvents[0].Message, PipelineStoppedText,
                "The single Error event must be the pipeline-stop event.");
            Assert.IsFalse(result.Events.Any(x => x.EventLevel == LogLevel.Critical),
                "A handled step failure must never be escalated to Critical; Critical is reserved for "
                + "exceptions escaping the pipeline loop itself.");
        }

        [TestMethod]
        public async Task PipelineStop_OnStepThatThrew_MessageIncludesExceptionText()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var throwingStep = new ThrowingCountingPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(throwingStep);

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result.Message,
                "A pipeline stopped by a THROWN failure must expose a diagnostic message.");
            StringAssert.Contains(result.Message, PipelineStoppedText,
                "The pipeline message must state that the pipeline stopped execution.");
            StringAssert.Contains(result.Message, ExceptionMarkerText,
                "A THROWN failure must select the 'WithError' message variant, which appends the exception text.");
            StringAssert.Contains(result.Message, ThrowingCountingPipelineStep.ThrownMessage,
                "The pipeline message must carry the message of the exception thrown by the step.");
        }

        [TestMethod]
        public async Task PipelineStop_OnStepThatReturnedFailure_MessageExcludesExceptionText()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var failingStep = new AlwaysFailCountingPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(failingStep);

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result.Message,
                "A pipeline stopped by a RETURNED failure must expose a diagnostic message.");
            StringAssert.Contains(result.Message, PipelineStoppedText,
                "The pipeline message must state that the pipeline stopped execution.");
            Assert.IsFalse(result.Message.Contains(ExceptionMarkerText),
                "A RETURNED failure carries no exception, so the plain message variant must be selected and no "
                + "exception text may be appended. This is the distinction between ExecStepFailedPipelineStop "
                + "and ExecStepFailedPipelineStopWithError.");
        }

        [TestMethod]
        public async Task StepRetry_OnExhaustedBudget_SetsRetryAlreadyUsedMessage()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var failingStep = new AlwaysFailCountingPipelineStep(1);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(failingStep);

            var result = await invoker.InvokeAsync(person);

            Assert.IsFalse(result.IsSuccess,
                "A step that failed every retry attempt must fail the pipeline.");
            Assert.IsNotNull(result.Message,
                "A pipeline that exhausted its retry budget must expose a diagnostic message.");
            StringAssert.Contains(result.Message, RetryAlreadyUsedText,
                "An exhausted retry budget must be reported through the retry-exhausted message, NOT through "
                + "the generic pipeline-stop message.");
            Assert.IsFalse(result.Message.Contains(PipelineStoppedText),
                "The retry-exhausted message is a distinct message from the pipeline-stop one.");
        }

        private static bool HasErrorOrCriticalEvent(RzR.PipelineFlowEngine.Models.Result.PipeLineResult<PersonDto> result)
            => result.Events.Any(x => x.EventLevel == LogLevel.Error || x.EventLevel == LogLevel.Critical);

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
