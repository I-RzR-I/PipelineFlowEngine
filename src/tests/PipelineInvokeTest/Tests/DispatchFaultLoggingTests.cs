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
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Pipeline;
using RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{

    [TestClass]
    public class DispatchFaultLoggingTests
    {

        private const string DispatchFaultedText = "faulted in the background";

        private const int FaultObservationTimeoutMs = 5000;

        private const int DispatchFaultedEventId = 3002;

        private const string DispatchFaultedEventName = "ScheduledStepDispatchFaulted";

        private const int DispatchedWithoutScopeEventId = 3006;

        private const string DispatchedWithoutScopeEventName = "ScheduledStepDispatchedWithoutScope";

        private const int DispatchedEventId = 3001;

        private const string DispatchedEventName = "ScheduledStepDispatched";

        private const int StopFaultedEventId = 3003;

        private const string StopFaultedEventName = "ScheduledStepStopFaulted";

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
        public async Task DispatchedStep_ThatFaultsAfterTheInvocationReturned_IsLoggedAtError_AndLeavesTheResultSuccessful()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new ThrowingFireAndForgetProbePipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            var faultEntryAwaiter = _recordingLoggerProvider.WaitForAsync(x =>
                x.Level == LogLevel.Error
                && x.Message != null
                && x.Message.Contains(DispatchFaultedText));

            var result = await invoker.InvokeAsync(person);

            var observed = await Task.WhenAny(faultEntryAwaiter, Task.Delay(FaultObservationTimeoutMs));

            Assert.AreSame(faultEntryAwaiter, observed,
                "The invoker must observe the dispatched job's completion and report the fault to the logger. "
                + "If nothing arrives, the background failure is unobserved: the step threw, the pipeline "
                + "reported success, and no sink anywhere carries a trace of the work that did not happen.");

            var faultEntry = await faultEntryAwaiter;

            Assert.AreEqual(LogLevel.Error, faultEntry.Level,
                "A dispatched step that faulted did not do its work, so it must be reported loudly. At Warning "
                + "or below it blends into the routine dispatch chatter the same run already produces.");
            StringAssert.Contains(faultEntry.Message, nameof(ThrowingFireAndForgetProbePipelineStep),
                "The fault message must name the step it concerns. The entry is written after the invocation "
                + "ended, so it carries no result and no event stream an operator could correlate it with; "
                + $"without the step name a pipeline holding several dispatched steps gives no lead. Got "
                + $"'{faultEntry.Message}'.");
            Assert.IsNotNull(faultEntry.Exception,
                "The fault entry must carry the exception object, not only its text. The message alone reaches "
                + "the sink without a stack trace, and a background throw has no other route to the caller.");
            StringAssert.Contains(faultEntry.Exception.ToString(), ThrowingFireAndForgetProbePipelineStep.ThrownMessage,
                "The attached exception must be the one raised inside the dispatched step. The whole exception "
                + "text is examined because whether the job completion surfaces the original exception or an "
                + "aggregate wrapper is the scheduler package's contract, not this engine's.");

            Assert.AreEqual(DispatchFaultedEventId, faultEntry.EventId.Id,
                "The fault must be tagged with its own event identifier. Message text gets reworded, wrapped and "
                + "translated on the way to a sink, so an alert that has to match on wording breaks quietly; the "
                + "identifier is the part an operator can safely build a rule on.");
            Assert.AreEqual(DispatchFaultedEventName, faultEntry.EventId.Name,
                "The identifier must carry its name as well as its number, so an entry read by a human states "
                + "what happened without a lookup table.");

            Assert.IsTrue(result.IsSuccess,
                "A fault observed after InvokeAsync returned must never travel back into the result. The caller "
                + "already received and acted upon it, so a result mutated later would contradict the decision "
                + "that was made from it.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                "The dispatched step's outcome is not evaluated by the pipeline, so the status stays Success "
                + "even under the PipelineStop fail strategy applied here.");
        }

        [TestMethod]
        public async Task DispatchedFault_ThatWasObserved_NeverAppearsInTheResultTheCallerAlreadyReceived()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new ThrowingFireAndForgetProbePipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            var faultEntryAwaiter = _recordingLoggerProvider.WaitForAsync(x =>
                x.EventId.Id == DispatchFaultedEventId);

            var result = await invoker.InvokeAsync(person);

            var observed = await Task.WhenAny(faultEntryAwaiter, Task.Delay(FaultObservationTimeoutMs));

            Assert.AreSame(faultEntryAwaiter, observed,
                "The background fault must have happened and been observed before anything is asserted about "
                + "the result; otherwise this test would merely prove that the fault had not occurred yet.");

            var events = result.Events.ToList();

            Assert.IsFalse(
                events.Any(x => x.Message != null && x.Message.Contains(DispatchFaultedText)),
                "The result is a snapshot taken while the dispatched work was still running, and the caller has "
                + "already read it and acted on it. Appending the later fault to its event stream would rewrite "
                + "history underneath that caller, and would do so on a background thread while the caller may "
                + "still be enumerating the very same collection.");
            Assert.IsFalse(
                events.Any(x => x.Exception != null
                    && x.Exception.ToString().Contains(ThrowingFireAndForgetProbePipelineStep.ThrownMessage)),
                "The exception raised by the dispatched step must not reach the caller's event stream either; "
                + "the observer and the log are its only routes out.");
            Assert.IsFalse(
                events.Any(x => x.EventLevel == LogLevel.Error || x.EventLevel == LogLevel.Critical),
                "An Error or Critical event keeps a flow result unsuccessful forever, so a fault leaking into "
                + "the stream would flip a result the caller was already told to trust.");

            Assert.IsTrue(result.IsSuccess,
                "The invocation dispatched its step and finished; a failure that happened afterwards belongs to "
                + "the dispatch, never to the invocation the caller received an answer for.");
        }

        [TestMethod]
        public async Task DispatchWithoutAScopeFactory_IsReportedOnce_AtWarning_WithItsOwnEventId()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new PersonSetNameFireAndForgetPipelineStep();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var logger = localServiceProvider.GetRequiredService<ILogger<PipelineFlowInvoker<PersonDto>>>();

            var invoker = new PipelineFlowInvoker<PersonDto>(
                new PersonPipelineContext(),
                logger,
                new List<IPipelineFlowStep<PersonDto>> { dispatchedStep });

            var result = await invoker.InvokeAsync(person);

            var withoutScopeEntries = _recordingLoggerProvider.Snapshot()
                .Where(x => x.EventId.Id == DispatchedWithoutScopeEventId)
                .ToList();

            Assert.AreEqual(1, withoutScopeEntries.Count,
                "The branch that dispatches a step against the caller's dying scope must report itself exactly "
                + "once per dispatch. Reporting nothing is how it behaved before: the strictly milder fallback "
                + "that merely reuses a step instance already warns, while the branch with the real "
                + "ObjectDisposedException hazard stayed silent. Reporting more than once means it is written "
                + $"from the work delegate instead of the invoker thread. Got {withoutScopeEntries.Count}.");

            var withoutScopeEntry = withoutScopeEntries[0];

            Assert.AreEqual(LogLevel.Warning, withoutScopeEntry.Level,
                "Warning, never Error: running without a scope factory is a supported opt-out, and an Error "
                + "flow event would permanently flip IsSuccess to false for every application that constructs "
                + "the invoker directly.");
            Assert.AreEqual(DispatchedWithoutScopeEventName, withoutScopeEntry.EventId.Name,
                "The identifier must carry its name as well as its number, so an entry read by a human states "
                + "what happened without a lookup table.");
            StringAssert.Contains(withoutScopeEntry.Message, nameof(PersonSetNameFireAndForgetPipelineStep),
                "The entry must name the dispatched step it concerns, otherwise a pipeline holding several "
                + $"dispatched steps gives no lead. Got '{withoutScopeEntry.Message}'.");

            Assert.IsTrue(result.IsSuccess,
                "The warning describes how the step was dispatched, not a failure of the run; the pipeline "
                + "must stay successful.");
        }

        [TestMethod]
        public async Task Dispatch_IsRecordedWithItsOwnEventId_NamingTheStepSoAFaultCanBeCorrelatedToIt()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new PersonSetNameFireAndForgetPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            await invoker.InvokeAsync(person);

            var dispatchEntries = _recordingLoggerProvider.Snapshot()
                .Where(x => x.EventId.Id == DispatchedEventId)
                .ToList();

            Assert.AreEqual(1, dispatchEntries.Count,
                "Dispatching a step must record exactly one entry carrying the dispatch identifier. Pinning "
                + "the declaration of an identifier is not the same as pinning that anything ever writes it: "
                + $"an identifier nobody emits is a rule that silently never matches. Got {dispatchEntries.Count}.");

            var dispatchEntry = dispatchEntries[0];

            Assert.AreEqual(DispatchedEventName, dispatchEntry.EventId.Name,
                "The identifier must carry its name as well as its number.");
            StringAssert.Contains(dispatchEntry.Message, nameof(PersonSetNameFireAndForgetPipelineStep),
                "The dispatch entry must name the STEP, the same correlation key every other dispatch-path "
                + "entry uses. Rendering something else — an execution order index, say — leaves an operator "
                + "unable to join a later fault back to the dispatch it came from, which is the one thing "
                + $"this entry exists for. Got '{dispatchEntry.Message}'.");
        }

        [TestMethod]
        public async Task DispatchedStep_WhoseScopeCannotBeBuilt_IsReportedAsFaulted_NotAsAJobThatNeverRan()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            var dispatchedStep = new PersonSetNameFireAndForgetPipelineStep();
            var scopeFactory = new ThrowingServiceScopeFactory();
            var observer = new RecordingDispatchObserver();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var logger = localServiceProvider.GetRequiredService<ILogger<PipelineFlowInvoker<PersonDto>>>();

            var invoker = new PipelineFlowInvoker<PersonDto>(
                new PersonPipelineContext(),
                logger,
                new List<IPipelineFlowStep<PersonDto>> { dispatchedStep },
                null,
                scopeFactory,
                observer);

            var faultEntryAwaiter = _recordingLoggerProvider.WaitForAsync(x =>
                x.EventId.Id == DispatchFaultedEventId);

            var result = await invoker.InvokeAsync(person);

            var signaled = await Task.WhenAny(observer.Completed, Task.Delay(FaultObservationTimeoutMs));

            Assert.AreSame(observer.Completed, signaled,
                "A dispatch whose scope could not be built must still reach a terminal outcome and be "
                + $"reported; nothing arrived within {FaultObservationTimeoutMs}ms.");

            var outcome = await observer.Completed;

            Assert.IsTrue(scopeFactory.CreateScopeCallCount >= 1,
                "The dispatch must have attempted to build a scope; otherwise the failure under test never "
                + "happened.");

            Assert.AreEqual(PipelineDispatchStatusType.Faulted, outcome.Status,
                "A dispatch that could not build its scope, step or context FAILED, and must be reported as "
                + "faulted. Reporting NotObserved instead is true only in the narrowest sense — the body "
                + "indeed never ran — while telling the operator the opposite of what happened: NotObserved "
                + "means the schedule was not honoured and should be re-driven, whereas re-driving a broken "
                + "DI registration simply fails again. An unregistered scoped dependency is the commonest "
                + "wiring mistake there is, and this is the difference between a one-line diagnosis and an "
                + "invisible one.");
            Assert.IsNotNull(outcome.Exception,
                "The resolution failure must be carried as an exception. NotObserved deliberately carries "
                + "none, so misclassifying this case discards the only description of what actually broke.");
            StringAssert.Contains(outcome.Exception.ToString(), ThrowingServiceScopeFactory.ThrownMessage,
                "The reported exception must be the one raised while building the scope, which is what names "
                + "the missing registration.");
            Assert.AreEqual(nameof(PersonSetNameFireAndForgetPipelineStep), outcome.StepName,
                $"The outcome must name the step whose dispatch failed. Got '{outcome.StepName}'.");

            var observed = await Task.WhenAny(faultEntryAwaiter, Task.Delay(FaultObservationTimeoutMs));

            Assert.AreSame(faultEntryAwaiter, observed,
                "The failure must reach the LOG as well as the observer. An application that registers no "
                + "observer has only this channel, and a wiring failure invisible on it is a background step "
                + "that never runs and never says so.");

            var faultEntry = await faultEntryAwaiter;

            Assert.AreEqual(LogLevel.Error, faultEntry.Level,
                "A dispatch that never reached its body did none of its work, so it must be reported loudly.");

            Assert.IsTrue(result.IsSuccess,
                "The failure happens after the invocation returned, so it must not travel back into the "
                + "result the caller already acted upon.");
        }

        [TestMethod]
        public async Task ScheduledJob_ThatFailsToStopAfterCancellation_IsReportedWithTheStopFaultedEventId()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new PersonSetNameFireAndForgetPipelineStep();

            var scheduler = new StopFaultingMethodScheduler();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var logger = localServiceProvider.GetRequiredService<ILogger<PipelineFlowInvoker<PersonDto>>>();

            var invoker = new PipelineFlowInvoker<PersonDto>(
                new PersonPipelineContext(),
                logger,
                new List<IPipelineFlowStep<PersonDto>> { dispatchedStep },
                scheduler);

            var stopFaultEntryAwaiter = _recordingLoggerProvider.WaitForAsync(x =>
                x.EventId.Id == StopFaultedEventId);

            using (var pipelineCancellation = new CancellationTokenSource())
            {

                await invoker.InvokeAsync(person, pipelineCancellation.Token);

                pipelineCancellation.Cancel();

                var observed = await Task.WhenAny(stopFaultEntryAwaiter, Task.Delay(FaultObservationTimeoutMs));

                Assert.AreEqual(1, scheduler.Job.StopCallCount,
                    "Cancelling the pipeline must request the dispatched job to stop exactly once.");

                Assert.AreSame(stopFaultEntryAwaiter, observed,
                    "A stop that fails must be reported. It fails asynchronously here, which is how a real "
                    + "stop fails, and the returned task is never awaited — so without a faulted continuation "
                    + "the exception is observed by nobody and a job left running after shutdown says "
                    + "nothing at all.");

                var stopFaultEntry = await stopFaultEntryAwaiter;

                Assert.AreEqual(LogLevel.Error, stopFaultEntry.Level,
                    "A job still running after the pipeline asked it to stop must be reported at Error.");
                Assert.AreEqual(StopFaultedEventName, stopFaultEntry.EventId.Name,
                    "The identifier must carry its name as well as its number.");
                StringAssert.Contains(stopFaultEntry.Message, nameof(PersonSetNameFireAndForgetPipelineStep),
                    $"The entry must name the job's step. Got '{stopFaultEntry.Message}'.");

                scheduler.Job.Finish();
            }
        }

        [TestMethod]
        public void DispatchLogEventIdentifiers_AreAPublishedContract_AndKeepTheirNumbers()
        {

            var expectedEventIds = new Dictionary<string, int>
            {
                { "ScheduledStepDispatched", 3001 },
                { "ScheduledStepDispatchFaulted", 3002 },
                { "ScheduledStepStopFaulted", 3003 },
                { "ScheduledStepNotResolvable", 3004 },
                { "DispatchObserverFaulted", 3005 },
                { "ScheduledStepDispatchedWithoutScope", 3006 }
            };

            var eventIdsType = typeof(PipelineDispatchOutcome).Assembly
                .GetType("RzR.PipelineFlowEngine.Helpers.PipelineFlowEventIds", false);

            Assert.IsNotNull(eventIdsType,
                "The event identifiers of the dispatch path must stay in one place. Spreading them back over "
                + "the call sites is how two situations end up sharing a number.");

            var declaredEventIds = eventIdsType
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => x.FieldType == typeof(EventId))
                .ToList();

            foreach (var expected in expectedEventIds)
            {
                var declaredField = declaredEventIds.FirstOrDefault(x => x.Name == expected.Key);

                Assert.IsNotNull(declaredField,
                    $"The identifier '{expected.Key}' must keep existing. Removing it silences every rule "
                    + "written against the situation it reports.");

                var declaredEventId = (EventId)declaredField.GetValue(null);

                Assert.AreEqual(expected.Value, declaredEventId.Id,
                    $"'{expected.Key}' must keep the number {expected.Value}. Renumbering it makes every alert "
                    + "and dashboard built on it match the wrong situation, or nothing at all, without a single "
                    + "error anywhere.");
                Assert.AreEqual(expected.Key, declaredEventId.Name,
                    $"'{expected.Key}' must carry its own name, so an entry stays readable where the number "
                    + "alone means nothing.");
            }

            var distinctIds = declaredEventIds
                .Select(x => ((EventId)x.GetValue(null)).Id)
                .Distinct()
                .Count();

            Assert.AreEqual(declaredEventIds.Count, distinctIds,
                "Every situation must keep a number of its own. Two situations sharing one identifier make a "
                + "rule fire for a case it was never written for, which is worse than not firing at all.");
        }
    }
}
