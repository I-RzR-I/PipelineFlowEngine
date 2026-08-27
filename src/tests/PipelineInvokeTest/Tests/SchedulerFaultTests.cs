#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Pipelines;
using PipelineInvokeTest.Pipelines.Steps.Person;
using PipelineInvokeTest.TestDoubles;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Pipeline;
using RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{

    [TestClass]
    public class SchedulerFaultTests
    {

        private const string PipelineStoppedText = "pipeline stopped execution";

        private const string NoObservedOutcomeText =
            "completed without producing an observed execution outcome";

        private const string StepThrewExceptionText = "threw an exception during execution";

        private IServiceCollection _serviceCollection;
        private RecordingLoggerProvider _recordingLoggerProvider;

        [TestInitialize]
        public void Init()
        {
            _recordingLoggerProvider = new RecordingLoggerProvider();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory, LoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder
                .AddConsole()
                .AddProvider(_recordingLoggerProvider)
                .SetMinimumLevel(LogLevel.Debug));

            _serviceCollection = serviceCollection;
        }

        [TestMethod]
        public async Task ScheduledWaitStep_ThatThrows_IsConvertedToAFailedStepResult_AndLoggedAtWarning()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var scheduledStep = new ThrowingScheduledWaitPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(scheduledStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(1, scheduledStep.ExecutionCount,
                "MaxIterations is bound to RetryIterations(1) and the job stops on failure, so the scheduled "
                + "step body must run exactly once.");
            Assert.IsFalse(result.IsSuccess,
                "A scheduled step that threw is a step FAILURE; under PipelineStop the pipeline must fail.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "A scheduled step that threw under PipelineStop must leave the pipeline status at Fail.");
            Assert.IsTrue(result.Message.Contains(ThrowingScheduledWaitPipelineStep.ThrownMessage),
                "The PipelineStop message must carry the message of the exception thrown by the scheduled step; "
                + $"got '{result.Message}'.");

            var stepResult = result.StepResults
                .SingleOrDefault(x => x.StepName == nameof(ThrowingScheduledWaitPipelineStep));

            Assert.IsNotNull(stepResult,
                "The thrown scheduled failure must be represented in StepResults, exactly like a returned one.");
            Assert.IsFalse(stepResult.StepResult.IsSuccess,
                "The step result built from the thrown exception must be a failed one.");
            Assert.IsTrue(stepResult.StepResult.Message.Contains(ThrowingScheduledWaitPipelineStep.ThrownMessage),
                "The step result must carry the message of the exception thrown by the scheduled step; "
                + $"got '{stepResult.StepResult.Message}'.");

            var logEntries = _recordingLoggerProvider.Snapshot();

            var warningEntry = logEntries.FirstOrDefault(x =>
                x.Level == LogLevel.Warning
                && x.Message != null
                && x.Message.Contains(ThrowingScheduledWaitPipelineStep.ThrownMessage));

            Assert.IsNotNull(warningEntry,
                "The wait-branch catch must CONVERT the thrown scheduled failure and report it at Warning. "
                + "An Error- or Critical-level event makes IsSuccess false even when a later strategy recovers.");
            Assert.IsNotNull(warningEntry.Exception,
                "The step-failure log entry must carry the originating exception object, not only its text: "
                + "the message alone reaches the sink without a stack trace, which is what an operator needs to "
                + "locate the throwing frame inside the scheduled step.");
            StringAssert.Contains(warningEntry.Exception.Message, ThrowingScheduledWaitPipelineStep.ThrownMessage,
                "The exception attached to the log entry must be the one raised by the scheduled step. A "
                + "containment check is used because whether the scheduler rethrows the original exception or "
                + "an aggregate wrapper is the scheduler package's contract, not this engine's; got "
                + $"'{warningEntry.Exception.Message}'.");

            Assert.IsFalse(logEntries.Any(x => x.Level == LogLevel.Critical),
                "A scheduled step that throws must NOT reach the invoker's outer Critical handler — that handler "
                + "is reserved for infrastructure failures, not step failures.");
            Assert.IsTrue(logEntries.Any(x => x.Level == LogLevel.Error),
                "PipelineStop itself logs the terminal decision at Error, so exactly one loud level is expected "
                + "here: Error from the strategy, never Critical from the catch-all.");

            var flowEvent = result.Events.FirstOrDefault(x =>
                x.EventLevel == LogLevel.Warning
                && x.Exception != null);

            Assert.IsNotNull(flowEvent,
                "The flow event stream and the logger are two different sinks; both must expose the exception "
                + "so a caller inspecting only the result is not blind to it.");
            Assert.IsTrue(flowEvent.Exception.Message.Contains(ThrowingScheduledWaitPipelineStep.ThrownMessage),
                "The event stream must carry the exception raised by the scheduled step; got "
                + $"'{flowEvent.Exception.Message}'.");
        }

        [TestMethod]
        public async Task ScheduledWaitStep_ThatThrows_UnderStepRetry_StopsWithAMessageCarryingTheExceptionText()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var scheduledStep = new ThrowingScheduledWaitPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(scheduledStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(1, scheduledStep.ExecutionCount,
                "A scheduled step owns its own iteration budget through the scheduler, so StepRetry must not "
                + "schedule it a second time.");
            Assert.IsFalse(result.IsSuccess,
                "A scheduled step that threw must fail the pipeline under StepRetry as well.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "A scheduled step that threw under StepRetry must leave the pipeline status at Fail.");

            Assert.IsNotNull(result.Message,
                "The scheduled StepRetry branch is terminal, so it must expose a diagnostic message.");
            StringAssert.Contains(result.Message, PipelineStoppedText,
                "The scheduled StepRetry branch stops the pipeline, so it reports the pipeline-stop message.");
            StringAssert.Contains(result.Message, ThrowingScheduledWaitPipelineStep.ThrownMessage,
                "When an exception was captured, the terminal message must embed its text. Falling back to the "
                + "plain variant would leave the caller with 'the pipeline stopped' and no cause at all, since "
                + "a scheduled step runs on a background thread the caller cannot observe.");
        }

        [TestMethod]
        public async Task ScheduledWaitStep_ThatThrows_UnderStepMoveToNext_IsAbsorbed_AndLaterStepsStillRun()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var scheduledStep = new ThrowingScheduledWaitPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextMoveToNext>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(scheduledStep);
            invoker.AddPipelineStep(lateStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(1, scheduledStep.ExecutionCount,
                "StepMoveToNext must not re-schedule, so the scheduled step body runs exactly once.");
            Assert.AreEqual(1, lateStep.ExecutionCount,
                "The step ordered after the throwing scheduled one must still execute under StepMoveToNext.");
            Assert.IsTrue(result.IsSuccess,
                "StepMoveToNext absorbs a THROWN scheduled failure and the pipeline reports success. This holds "
                + "only while the conversion is recorded at Warning: a single Error- or Critical-level event "
                + "makes IsSuccess false even when Status is Success.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                "An absorbed scheduled failure must leave the pipeline status at Success.");

            var logEntries = _recordingLoggerProvider.Snapshot();

            Assert.IsTrue(logEntries.Any(x =>
                    x.Level == LogLevel.Warning
                    && x.Message != null
                    && x.Message.Contains(ThrowingScheduledWaitPipelineStep.ThrownMessage)),
                "The absorbed failure must still be visible at Warning; absorbing it silently would make a "
                + "throwing scheduled step indistinguishable from a healthy one.");
            Assert.IsFalse(logEntries.Any(x => x.Level == LogLevel.Error || x.Level == LogLevel.Critical),
                "StepMoveToNext must not emit any Error/Critical entry — one would flip IsSuccess to false and "
                + "contradict the absorbed outcome asserted above.");
        }

        [TestMethod]
        public async Task SchedulerThatThrowsOnSchedule_FailsThePipelineAtCriticalLevel_NotAsAStepFailure()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var scheduledStep = new ThrowingScheduledWaitPipelineStep();
            var faultingScheduler = new FaultingMethodScheduler();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var logger = localServiceProvider.GetRequiredService<ILogger<PipelineFlowInvoker<PersonDto>>>();

            var invoker = new PipelineFlowInvoker<PersonDto>(
                new PersonPipelineContextPipelineStop(),
                logger,
                new List<IPipelineFlowStep<PersonDto>> { scheduledStep },
                faultingScheduler);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(1, faultingScheduler.ScheduleCallCount,
                "The invoker must attempt to schedule the step exactly once before the plumbing failure aborts it.");
            Assert.AreEqual(0, scheduledStep.ExecutionCount,
                "The step body can never run when scheduling itself failed.");
            Assert.IsFalse(result.IsSuccess,
                "A scheduling infrastructure failure must fail the pipeline.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "A scheduling infrastructure failure must leave the pipeline status at Fail.");
            Assert.AreEqual(FaultingMethodScheduler.DefaultThrownMessage, result.Message,
                "The outer handler assigns the raw exception message to the pipeline result message.");

            Assert.IsFalse(result.StepResults.Any(x => x.StepName == nameof(ThrowingScheduledWaitPipelineStep)),
                "A plumbing failure is NOT a step failure: no step result may be recorded for it. If one appears, "
                + "the failure was silently reclassified as a step failure and became subject to the fail strategy.");

            var logEntries = _recordingLoggerProvider.Snapshot();

            var criticalEntry = logEntries.FirstOrDefault(x => x.Level == LogLevel.Critical);

            Assert.IsNotNull(criticalEntry,
                "A scheduling infrastructure failure must be reported at Critical by the invoker's outer handler. "
                + "Downgrading it to Warning would turn a LOUD failure into a QUIET one.");
            Assert.AreSame(faultingScheduler.ToThrow, criticalEntry.Exception,
                "The Critical entry must carry the ORIGINAL scheduler exception instance, not a re-wrapped one.");
        }

        [TestMethod]
        public async Task SchedulerThatThrowsOnSchedule_UnderStepMoveToNext_StillFailsThePipeline()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var scheduledStep = new ThrowingScheduledWaitPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);
            var faultingScheduler = new FaultingMethodScheduler();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var logger = localServiceProvider.GetRequiredService<ILogger<PipelineFlowInvoker<PersonDto>>>();

            var invoker = new PipelineFlowInvoker<PersonDto>(
                new PersonPipelineContextMoveToNext(),
                logger,
                new List<IPipelineFlowStep<PersonDto>> { scheduledStep, lateStep },
                faultingScheduler);

            var result = await invoker.InvokeAsync(person);

            Assert.IsFalse(result.IsSuccess,
                "An INFRASTRUCTURE failure is raised outside the per-step try/catch, so no step fail strategy "
                + "can absorb it. If StepMoveToNext ever swallowed this, a pipeline whose work never ran would "
                + "report success.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "A scheduling infrastructure failure must leave the pipeline status at Fail under any strategy.");
            Assert.AreEqual(FaultingMethodScheduler.DefaultThrownMessage, result.Message,
                "The outer handler assigns the raw exception message to the pipeline result message.");
            Assert.AreEqual(0, lateStep.ExecutionCount,
                "The plumbing failure aborts the whole step loop, so no later step may run — this is the "
                + "behavioural difference from an absorbed STEP failure, where the later step does run.");
            Assert.AreEqual(0, scheduledStep.ExecutionCount,
                "The step body can never run when scheduling itself failed.");
            Assert.AreEqual(1, faultingScheduler.ScheduleCallCount,
                "The plumbing failure must not be retried by the fail strategy.");

            Assert.IsTrue(_recordingLoggerProvider.Snapshot().Any(x => x.Level == LogLevel.Critical),
                "The infrastructure failure must be reported at Critical even under StepMoveToNext.");
        }

        [TestMethod]
        public async Task ScheduledWaitStep_WhoseJobCompletesWithoutRunningIt_FailsWithTheNoObservedOutcomeMessage()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var scheduledStep = new ScheduledWaitCountingPipelineStep();
            var nonInvokingScheduler = new NonInvokingMethodScheduler();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var logger = localServiceProvider.GetRequiredService<ILogger<PipelineFlowInvoker<PersonDto>>>();

            var invoker = new PipelineFlowInvoker<PersonDto>(
                new PersonPipelineContextPipelineStop(),
                logger,
                new List<IPipelineFlowStep<PersonDto>> { scheduledStep },
                nonInvokingScheduler);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(1, nonInvokingScheduler.ScheduleCallCount,
                "The step must have been scheduled exactly once; this failure belongs to a job that was "
                + "accepted, not to one that was never created.");
            Assert.AreEqual(0, scheduledStep.ExecutionCount,
                "The job completed without ever invoking the step, which is the situation under test.");
            Assert.AreEqual("TestName", person.Name,
                "The step body applies its own name to the pipeline item, so an unchanged item proves the body "
                + "never ran.");
            Assert.IsFalse(result.IsSuccess,
                "A job that finished without the step ever reporting a result did not do the work the pipeline "
                + "waited for, so the pipeline must fail rather than pass work off as done.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "The pipeline ends with the Fail status.");

            var stepResult = result.StepResults
                .SingleOrDefault(x => x.StepName == nameof(ScheduledWaitCountingPipelineStep));

            Assert.IsNotNull(stepResult,
                "The outcome must be represented in StepResults, exactly like a step that ran and failed.");
            Assert.AreEqual(PipelineFlowStepIterationType.FirstExecution, stepResult.StepIteration,
                "The entry belongs to the first and only attempt made for this step.");
            Assert.IsFalse(stepResult.StepResult.IsSuccess,
                "A step that never reported a result must not be recorded as successful.");
            Assert.IsFalse(string.IsNullOrEmpty(stepResult.StepResult.Message),
                "Without a message the entry is an empty failure, and a caller reading it cannot tell whether "
                + "the step ran and failed silently or never ran at all — the two call for opposite responses, "
                + "fixing the step versus fixing the schedule.");
            StringAssert.Contains(stepResult.StepResult.Message, NoObservedOutcomeText,
                "The step result must state that the job produced no observed outcome; got "
                + $"'{stepResult.StepResult.Message}'.");
            StringAssert.Contains(stepResult.StepResult.Message, nameof(ScheduledWaitCountingPipelineStep),
                "The message must name the scheduled step it concerns, otherwise a pipeline holding several "
                + "scheduled steps gives no clue which schedule went unfulfilled.");

            Assert.IsTrue(result.Events.Any(x =>
                    x.EventLevel == LogLevel.Warning
                    && x.Message != null
                    && x.Message.Contains(NoObservedOutcomeText)),
                "The event stream must carry the same diagnosis at Warning. It is recorded at Warning rather "
                + "than Error so a fail strategy that recovers from the step failure still leaves the pipeline "
                + "successful, exactly as it does for a step that ran and failed.");

            Assert.IsFalse(string.IsNullOrEmpty(result.Message),
                "The terminal result must expose a message.");
            StringAssert.Contains(result.Message, PipelineStoppedText,
                "Under PipelineStop the result reports the pipeline-stop decision.");
            StringAssert.Contains(result.Message, NoObservedOutcomeText,
                "The terminal message must also name the cause. A step whose scheduled job never ran it is a "
                + "different failure from one that ran and failed, and a caller reading only Message would "
                + "otherwise be unable to tell them apart.");
        }

        [TestMethod]
        public async Task ScheduledWaitStep_WhoseLastIterationThrows_ReportsTheFailure_NotTheEarlierSuccess()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            var scheduledStep = new SucceedsThenThrowsScheduledWaitPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(scheduledStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(2, scheduledStep.ExecutionCount,
                "The iteration budget must have been spent in full; with a single iteration the successful "
                + "one would be the last one and this test would prove nothing.");

            var stepResult = result.StepResults
                .SingleOrDefault(x => x.StepName == nameof(SucceedsThenThrowsScheduledWaitPipelineStep));

            Assert.IsNotNull(stepResult,
                "The scheduled step the pipeline waited for must contribute a step result.");
            Assert.IsFalse(stepResult.StepResult.IsSuccess,
                "The step result must describe the LAST iteration. Keeping the earlier successful one hands "
                + "the caller a success for work whose final attempt failed, and the caller then stops "
                + "looking: nothing else reports that failure, because a waited step raises no dispatch "
                + "outcome.");
            StringAssert.Contains(stepResult.StepResult.Message,
                SucceedsThenThrowsScheduledWaitPipelineStep.ThrownMessage,
                "The step result must carry the cause of the last failure and not the message of the "
                + $"iteration that succeeded. Got '{stepResult.StepResult.Message}'.");

            Assert.IsFalse(result.IsSuccess,
                "Under PipelineStop a scheduled step whose last iteration threw must fail the pipeline.");
            StringAssert.Contains(result.Message, SucceedsThenThrowsScheduledWaitPipelineStep.ThrownMessage,
                "The terminal message must name the background failure, otherwise the caller learns that the "
                + $"pipeline stopped without learning why. Got '{result.Message}'.");

            Assert.AreEqual(1, result.Events.Count(x => x.Exception != null),
                "The failure must be reported exactly once, and on this configuration the absorbed route is "
                + "its only possible reporter: the scheduler swallowed the throw, so the job completed "
                + "normally and the job-completion catch never ran. A second event here would mean the "
                + "absorbed route reported the same throw twice on its own.");
        }

        [TestMethod]
        public async Task ScheduledWaitStep_WhoseLastIterationThrows_AndIsSurfacedByTheScheduler_IsReportedOnce()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            var scheduledStep = new SucceedsThenThrowsScheduledWaitPipelineStep(true);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(scheduledStep);

            var result = await invoker.InvokeAsync(person);

            Assert.IsFalse(result.IsSuccess,
                "A scheduled step whose failure the scheduler surfaces must still fail the pipeline under "
                + "PipelineStop.");

            Assert.AreEqual(1, result.Events.Count(x => x.Exception != null),
                "One throw must produce one report. When the scheduler surfaces the failure on the job "
                + "completion, the execution state holds the very same exception; reporting both turns a "
                + "single failure into two Warning events and two log lines.");

            Assert.AreEqual(1, _recordingLoggerProvider.Snapshot().Count(x =>
                    x.Message != null
                    && x.Message.Contains(StepThrewExceptionText)
                    && x.Message.Contains(SucceedsThenThrowsScheduledWaitPipelineStep.ThrownMessage)),
                "The logger sink is a different sink from the flow event stream and must agree with it: one "
                + "throw, one entry carrying it. The predicate is pinned to the ENGINE's own conversion "
                + "message rather than counting every entry that carries an exception, because the scheduler "
                + "package logs into this same container and an entry it may start writing for a surfaced "
                + "iteration failure would break this count for a reason that has nothing to do with the "
                + "engine.");
        }

        [TestMethod]
        public async Task ScheduledWaitStep_WhoseEarlierIterationThrows_ButRecovers_ReportsTheFinalSuccess()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var scheduledStep = new ThrowsThenSucceedsScheduledPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(scheduledStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(2, scheduledStep.ExecutionCount,
                "The throwing iteration and the recovering one must both have run; with a single iteration "
                + "there is no earlier failure to be carried forward and this test would prove nothing.");

            var stepResult = result.StepResults
                .SingleOrDefault(x => x.StepName == nameof(ThrowsThenSucceedsScheduledPipelineStep));

            Assert.IsNotNull(stepResult,
                "A scheduled step the pipeline waited for must contribute a step result.");
            Assert.AreEqual(PipelineFlowStepIterationType.FirstExecution, stepResult.StepIteration,
                "A waited scheduled step contributes exactly ONE entry, tagged FirstExecution, however many "
                + "scheduler iterations it ran: the iterations belong to the scheduler, not to the pipeline's "
                + "own retry loop, and the two must never be conflated in what the caller reads.");
            Assert.IsTrue(stepResult.StepResult.IsSuccess,
                "The step recovered on its last iteration, so it must be reported as successful. The failure "
                + "of an earlier attempt is exactly what a scheduled retry budget exists to absorb; leaving "
                + "it recorded turns every successful recovery into a reported failure, and an operator can "
                + "then no longer tell a transient blip from a permanent outage.");

            Assert.IsTrue(result.IsSuccess,
                "A scheduled step that ultimately succeeded must leave the pipeline successful, even under "
                + "PipelineStop.");
            Assert.AreEqual(ThrowsThenSucceedsScheduledPipelineStep.RecoveredName, result.FlowResponse.Name,
                "The recovering iteration's work must be visible on the item the caller receives; without "
                + "this the test could pass on a run where the recovery never happened.");
        }

        [TestMethod]
        public async Task ScheduledStep_WithZeroRetryIterations_IsRejectedByTheScheduler_AndFailsThePipelineAtCritical()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var scheduledStep = new ZeroIterationScheduledPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(scheduledStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, scheduledStep.ExecutionCount,
                "A zero iteration budget leaves the step body unexecuted; the schedule is rejected before any "
                + "iteration is attempted.");
            Assert.AreEqual("TestName", person.Name,
                "The step body applies its own name to the pipeline item, so an unchanged item is a second, "
                + "independent proof that the body never ran.");

            Assert.IsFalse(result.IsSuccess,
                "RetryIterations maps onto the scheduler's MaxIterations, which the scheduler validates as "
                + "greater than or equal to one. Zero is therefore a configuration error, not a quiet no-op: "
                + "it is raised outside the per-step catch, reaches the invoker's outer handler and fails the "
                + "whole pipeline. This is what the README and docs/usage.md state, and it must not drift.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "The rejected schedule leaves the pipeline status at Fail.");
            StringAssert.Contains(result.Message, "MaxIterations",
                "The result message must name the rejected option, otherwise a zero iteration budget looks "
                + $"like an unexplained pipeline failure. Got '{result.Message}'.");

            Assert.IsTrue(result.Events.Any(x => x.EventLevel == LogLevel.Critical),
                "A rejected schedule is an infrastructure failure and is reported at Critical by the outer "
                + "handler, exactly like a scheduler that throws from Schedule.");
            Assert.IsFalse(result.StepResults.Any(x => x.StepName == nameof(ZeroIterationScheduledPipelineStep)),
                "A schedule that was never accepted is not a step failure, so no step result may be recorded "
                + "for it; one would make the failure subject to the fail strategy.");
        }
    }
}
