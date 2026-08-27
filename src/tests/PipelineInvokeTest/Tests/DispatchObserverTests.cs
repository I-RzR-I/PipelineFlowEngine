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
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{

    [TestClass]
    public class DispatchObserverTests
    {

        private const int DispatchNotificationTimeoutMilliseconds = 5000;

        private const int NotificationSettleWindowMilliseconds = 2000;

        private const int DispatchObserverFaultedEventId = 3005;

        private const int DispatchFaultedEventId = 3002;

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
        public async Task DispatchObserver_WhenTheDispatchedStepSucceeds_ReportsCompleted()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new PersonSetNameFireAndForgetPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowDispatchObserver<PersonDto, RecordingDispatchObserver>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var observer = (RecordingDispatchObserver)localServiceProvider
                .GetRequiredService<IPipelineDispatchObserver<PersonDto>>();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            await invoker.InvokeAsync(person);

            var outcome = await AwaitDispatchOutcomeAsync(observer,
                "A dispatched step that ran to completion must still be reported. Without that notification an "
                + "application has no way of learning that the background half of its pipeline actually "
                + "happened, and cannot tell it apart from work that was silently dropped.");

            Assert.AreEqual(PipelineDispatchStatusType.Completed, outcome.Status,
                "The step body ran and did not throw, so the dispatch must be reported as completed.");
            Assert.AreEqual(nameof(PersonSetNameFireAndForgetPipelineStep), outcome.StepName,
                "The outcome must name the step it concerns. It arrives with no result and no event stream to "
                + $"correlate it with, so a pipeline holding several dispatched steps gives no lead without the "
                + $"name. Got '{outcome.StepName}'.");
            Assert.IsNull(outcome.Exception,
                "A completed dispatch must carry no exception; an application that routes outcomes by looking "
                + "for one would otherwise treat healthy background work as a failure.");
        }

        [TestMethod]
        public async Task DispatchObserver_WhenTheDispatchedStepFaults_ReportsFaultedWithTheException()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new ThrowingFireAndForgetProbePipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowDispatchObserver<PersonDto, RecordingDispatchObserver>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var observer = (RecordingDispatchObserver)localServiceProvider
                .GetRequiredService<IPipelineDispatchObserver<PersonDto>>();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            var result = await invoker.InvokeAsync(person);

            var outcome = await AwaitDispatchOutcomeAsync(observer,
                "A dispatched step that threw must be reported. The invocation already returned success, so "
                + "this notification is the only route by which the failure can reach the application.");

            Assert.AreEqual(PipelineDispatchStatusType.Faulted, outcome.Status,
                "The dispatched step threw, so its outcome must be reported as faulted and not merged into the "
                + "states used for work that succeeded or never ran.");
            Assert.AreEqual(nameof(ThrowingFireAndForgetProbePipelineStep), outcome.StepName,
                $"A faulted dispatch must name the step that failed. Got '{outcome.StepName}'.");
            Assert.IsNotNull(outcome.Exception,
                "The failure must be carried as an exception object, not only as a status. An application that "
                + "reports background failures has no stack trace and no message to forward without it.");
            StringAssert.Contains(outcome.Exception.ToString(), ThrowingFireAndForgetProbePipelineStep.ThrownMessage,
                "The reported exception must be the one raised inside the step. The whole exception text is "
                + "examined because whether the job completion surfaces the original exception or an aggregate "
                + "wrapper is the scheduler package's contract, not this engine's.");

            Assert.IsTrue(result.IsSuccess,
                "Reporting the fault to the observer must not retroactively change the result the caller was "
                + "already given; the caller has acted on it long before the background step ended.");
        }

        [TestMethod]
        public async Task DispatchObserver_WhenTheDispatchedStepThrowsAndTheSchedulerAbsorbsIt_StillReportsFaulted()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            var dispatchedStep = new ThrowingFireAndForgetPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowDispatchObserver<PersonDto, RecordingDispatchObserver>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var observer = (RecordingDispatchObserver)localServiceProvider
                .GetRequiredService<IPipelineDispatchObserver<PersonDto>>();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            var faultEntryAwaiter = _recordingLoggerProvider.WaitForAsync(x =>
                x.EventId.Id == DispatchFaultedEventId);

            var result = await invoker.InvokeAsync(person);

            var outcome = await AwaitDispatchOutcomeAsync(observer,
                "A dispatched step must be reported whatever the scheduler did with its exception; the "
                + "observer is the only channel left once the invocation returned.");

            Assert.AreEqual(PipelineDispatchStatusType.Faulted, outcome.Status,
                "The step body RAN and THREW, so the dispatch is faulted. Reading the job task alone would "
                + "report NotObserved here — documented as 'the job completed but the step body never ran' — "
                + "which sends an application looking for a broken schedule instead of a broken step, and "
                + "hides the failure entirely from anyone who only alerts on Faulted.");
            Assert.AreEqual(nameof(ThrowingFireAndForgetPipelineStep), outcome.StepName,
                $"A faulted dispatch must name the step that failed. Got '{outcome.StepName}'.");
            Assert.IsNotNull(outcome.Exception,
                "An absorbed failure must still be carried as an exception object; the status alone gives an "
                + "application nothing to log, forward or diagnose with.");
            Assert.IsInstanceOfType(outcome.Exception, typeof(AggregateException),
                "The exception must always arrive in the same shape. A job task exposes an AggregateException, "
                + "so an absorbed exception is wrapped in one too; two shapes on one property would make every "
                + "consumer branch on which route the failure happened to take.");
            StringAssert.Contains(outcome.Exception.ToString(), ThrowingFireAndForgetPipelineStep.ThrownMessage,
                "The reported exception must be the one raised inside the step body.");

            var loggedFault = await Task.WhenAny(faultEntryAwaiter, Task.Delay(DispatchNotificationTimeoutMilliseconds));

            Assert.AreSame(faultEntryAwaiter, loggedFault,
                "The absorbed fault must reach the LOG as well as the observer. Registering an observer is "
                + "optional by design, so for every application that registers none this entry is the single "
                + "channel by which an absorbed background failure escapes at all; if it does not fire, such "
                + "an application has no way whatsoever of learning the work did not happen.");

            var faultEntry = await faultEntryAwaiter;

            Assert.AreEqual(LogLevel.Error, faultEntry.Level,
                "A dispatched step that faulted did not do its work, so it must be reported at Error rather "
                + "than blending into the routine dispatch chatter the same run already produces.");
            StringAssert.Contains(faultEntry.Message, nameof(ThrowingFireAndForgetPipelineStep),
                "The entry must name the step it concerns; written after the invocation ended, it carries no "
                + $"result and no event stream to correlate it with. Got '{faultEntry.Message}'.");
            Assert.IsNotNull(faultEntry.Exception,
                "The entry must carry the exception object. The scheduler absorbed the throw, so this entry "
                + "is the only place the stack trace of an absorbed failure survives.");

            Assert.IsTrue(result.IsSuccess,
                "Reporting the absorbed fault must not change the result the caller already received.");
        }

        [TestMethod]
        public async Task DispatchObserver_WhenTheDispatchedStepFailsWithAForeignCancellation_ReportsFaultedNotCanceled()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new ForeignCancellationFireAndForgetPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowDispatchObserver<PersonDto, RecordingDispatchObserver>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var observer = (RecordingDispatchObserver)localServiceProvider
                .GetRequiredService<IPipelineDispatchObserver<PersonDto>>();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            var faultEntryAwaiter = _recordingLoggerProvider.WaitForAsync(x =>
                x.EventId.Id == DispatchFaultedEventId);

            await invoker.InvokeAsync(person);

            var outcome = await AwaitDispatchOutcomeAsync(observer,
                "A dispatched step that failed must be reported whatever exception type it failed with.");

            Assert.AreEqual(PipelineDispatchStatusType.Faulted, outcome.Status,
                "A TaskCanceledException raised by the step's OWN token — an HttpClient timeout is exactly "
                + "this — is a permanent FAILURE of the step, not a shutdown. Classifying it on the exception "
                + "TYPE instead of on the pipeline token reports it as Canceled, which carries no exception "
                + "and, because it is not Faulted, also suppresses the faulted log entry. A step that times "
                + "out on every run would then be invisible on both channels at once, which is precisely the "
                + "ambiguous silence this whole outcome contract exists to remove.");
            Assert.IsNotNull(outcome.Exception,
                "A foreign cancellation must arrive WITH its exception; a Canceled outcome deliberately "
                + "carries none, so losing the exception is how the misclassification hides the cause.");
            Assert.IsInstanceOfType(outcome.Exception, typeof(AggregateException),
                "The exception keeps the same shape as every other faulted dispatch.");
            Assert.IsInstanceOfType(outcome.Exception.GetBaseException(), typeof(OperationCanceledException),
                "The originating exception must still be the cancellation the step actually raised; it is "
                + "reclassified, never replaced.");
            Assert.AreEqual(nameof(ForeignCancellationFireAndForgetPipelineStep), outcome.StepName,
                $"The outcome must name the step that failed. Got '{outcome.StepName}'.");

            var loggedFault = await Task.WhenAny(faultEntryAwaiter, Task.Delay(DispatchNotificationTimeoutMilliseconds));

            Assert.AreSame(faultEntryAwaiter, loggedFault,
                "The second channel must agree with the first. An application that registers no observer has "
                + "only the log, so a fault classified correctly for the observer but never logged would "
                + "still leave that application blind.");

            var faultEntry = await faultEntryAwaiter;

            Assert.AreEqual(LogLevel.Error, faultEntry.Level,
                "A step that fails on every timeout must be reported loudly, not filed as routine shutdown.");
            StringAssert.Contains(faultEntry.Message, nameof(ForeignCancellationFireAndForgetPipelineStep),
                $"The logged fault must name the step it concerns. Got '{faultEntry.Message}'.");
        }

        [TestMethod]
        public async Task DispatchObserver_WhenThePipelineItselfIsCanceled_StillReportsCanceledWithoutAnException()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new GatedCancellationFireAndForgetPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowDispatchObserver<PersonDto, RecordingDispatchObserver>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var observer = (RecordingDispatchObserver)localServiceProvider
                .GetRequiredService<IPipelineDispatchObserver<PersonDto>>();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            using (var pipelineCancellation = new CancellationTokenSource())
            {

                await invoker.InvokeAsync(person, pipelineCancellation.Token);

                var entered = await Task.WhenAny(dispatchedStep.Entered,
                    Task.Delay(DispatchNotificationTimeoutMilliseconds));

                Assert.AreSame(dispatchedStep.Entered, entered,
                    "The dispatched body must actually be running before the pipeline is cancelled, otherwise "
                    + "this test would assert against a job that was stopped before it ever started.");

                pipelineCancellation.Cancel();
                dispatchedStep.Release();

                var outcome = await AwaitDispatchOutcomeAsync(observer,
                    "A dispatched execution abandoned by a genuine pipeline cancellation must still be "
                    + "reported; shutting down is not a reason to go quiet about work that did not finish.");

                Assert.AreEqual(PipelineDispatchStatusType.Canceled, outcome.Status,
                    "The pipeline's OWN token was cancelled, so abandoning the work is an expected shutdown "
                    + "and must stay Canceled. Making the foreign-cancellation case Faulted must not sweep "
                    + "genuine cancellations into the fault channel, or every orderly shutdown would raise "
                    + "Error entries and page whoever is on call.");
                Assert.IsNull(outcome.Exception,
                    "A cancellation is expected, so no exception is carried; inventing one would have an "
                    + "application treat an orderly shutdown as a failure to investigate.");
                Assert.AreEqual(nameof(GatedCancellationFireAndForgetPipelineStep), outcome.StepName,
                    $"The outcome must name the step that was abandoned. Got '{outcome.StepName}'.");
            }
        }

        [TestMethod]
        public async Task DispatchObserver_WhenAnEarlierIterationThrowsButTheStepRecovers_ReportsCompleted()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new ThrowsThenSucceedsScheduledPipelineStep(false);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowDispatchObserver<PersonDto, RecordingDispatchObserver>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var observer = (RecordingDispatchObserver)localServiceProvider
                .GetRequiredService<IPipelineDispatchObserver<PersonDto>>();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            await invoker.InvokeAsync(person);

            var outcome = await AwaitDispatchOutcomeAsync(observer,
                "A dispatched job that ended must be reported, including when it ended by recovering.");

            Assert.AreEqual(2, dispatchedStep.ExecutionCount,
                "The throwing iteration and the recovering one must both have run, otherwise there is no "
                + "earlier failure that could have been carried forward.");

            Assert.AreEqual(PipelineDispatchStatusType.Completed, outcome.Status,
                "The LAST iteration ran the body through without throwing, so the dispatch is Completed. "
                + "Carrying the earlier iteration's failure forward would report Faulted for work that "
                + "ultimately succeeded — it would contradict the documented meaning of Completed, and it "
                + "would have an application compensate for, alert on, or re-drive work that is already "
                + "done, which is worse than not reporting it at all.");
            Assert.IsNull(outcome.Exception,
                "A recovered dispatch carries no exception; handing back the exception of an attempt that "
                + "was already superseded points an operator at a failure that no longer exists.");
            Assert.AreEqual(nameof(ThrowsThenSucceedsScheduledPipelineStep), outcome.StepName,
                $"The outcome must name the step it concerns. Got '{outcome.StepName}'.");
        }

        [TestMethod]
        public async Task DispatchObserver_WhenTheJobCompletesWithoutRunningTheStep_ReportsNotObserved()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new PersonSetNameFireAndForgetPipelineStep();
            var observer = new RecordingDispatchObserver();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var logger = localServiceProvider.GetRequiredService<ILogger<PipelineFlowInvoker<PersonDto>>>();

            var invoker = new PipelineFlowInvoker<PersonDto>(
                new PersonPipelineContext(),
                logger,
                new List<IPipelineFlowStep<PersonDto>> { dispatchedStep },
                new NonInvokingMethodScheduler(),
                null,
                observer);

            await invoker.InvokeAsync(person);

            var outcome = await AwaitDispatchOutcomeAsync(observer,
                "A dispatched job that finished must be reported even when the step body never ran; leaving it "
                + "unreported is precisely the silence this outcome exists to remove.");

            Assert.AreEqual(PipelineDispatchStatusType.NotObserved, outcome.Status,
                "A job that completed without ever running the step must be reported as such and never as a "
                + "completed dispatch. This is what makes silence unambiguous: the application can tell work "
                + "that was done from work that was scheduled and then quietly never happened, and only the "
                + "second case needs someone to re-drive it.");
            Assert.AreEqual(nameof(PersonSetNameFireAndForgetPipelineStep), outcome.StepName,
                $"The outcome must name the step whose body never ran. Got '{outcome.StepName}'.");
            Assert.IsNull(outcome.Exception,
                "Nothing threw here, so no exception may be invented; the status alone reports that the body "
                + "never ran.");
        }

        [TestMethod]
        public async Task DispatchObserver_ThatThrows_DoesNotAffectThePipelineResult()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new PersonSetNameFireAndForgetPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowDispatchObserver<PersonDto, ThrowingDispatchObserver>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var observer = (ThrowingDispatchObserver)localServiceProvider
                .GetRequiredService<IPipelineDispatchObserver<PersonDto>>();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            var observerFaultEntryAwaiter = _recordingLoggerProvider.WaitForAsync(x =>
                x.EventId.Id == DispatchObserverFaultedEventId);

            var result = await invoker.InvokeAsync(person);

            var invoked = await Task.WhenAny(observer.Invoked, Task.Delay(DispatchNotificationTimeoutMilliseconds));

            Assert.AreSame(observer.Invoked, invoked,
                "The observer must actually have been invoked; otherwise this test would report a contained "
                + "exception while in truth nothing ever reached the observer at all.");

            Assert.IsTrue(result.IsSuccess,
                "A broken observer is the application's own defect and it runs after the invocation returned, so "
                + "it must never be able to turn a successful pipeline into a failed one.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                "The status handed to the caller must stay untouched by anything the observer does.");

            var observerFaulted = await Task.WhenAny(
                observerFaultEntryAwaiter,
                Task.Delay(DispatchNotificationTimeoutMilliseconds));

            Assert.AreSame(observerFaultEntryAwaiter, observerFaulted,
                "Swallowing the observer exception must not mean hiding it: the only reason the application is "
                + "not told through an exception is that it is told through the log instead.");

            var observerFaultEntry = await observerFaultEntryAwaiter;

            Assert.AreEqual(LogLevel.Error, observerFaultEntry.Level,
                "An observer that throws loses the outcome it was handed, so it must be reported loudly rather "
                + "than blending into routine dispatch chatter.");
            Assert.IsNotNull(observerFaultEntry.Exception,
                "The entry must carry the exception object; the swallowed throw has no other route to whoever "
                + "operates the application.");
            StringAssert.Contains(observerFaultEntry.Exception.ToString(), ThrowingDispatchObserver.ThrownMessage,
                "The reported exception must be the one the observer threw, otherwise the entry points at the "
                + "wrong culprit.");
            StringAssert.Contains(observerFaultEntry.Message, nameof(PersonSetNameFireAndForgetPipelineStep),
                "The entry must name the dispatched step whose outcome was lost, so it is known which work is "
                + $"now unaccounted for. Got '{observerFaultEntry.Message}'.");
        }

        [TestMethod]
        public async Task DispatchObserver_IsNotRequired_PipelineRunsWithoutOne()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            var dispatchedStep = new ThrowingFireAndForgetProbePipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            var faultEntryAwaiter = _recordingLoggerProvider.WaitForAsync(x =>
                x.Level == LogLevel.Error
                && x.Message != null
                && x.Message.Contains(nameof(ThrowingFireAndForgetProbePipelineStep)));

            var result = await invoker.InvokeAsync(person);

            var observed = await Task.WhenAny(faultEntryAwaiter, Task.Delay(DispatchNotificationTimeoutMilliseconds));

            Assert.AreSame(faultEntryAwaiter, observed,
                "The dispatched job's completion must be handled the same way whether or not an application "
                + "supplied an observer; observing outcomes is an opt-in, never a prerequisite for dispatching.");

            Assert.IsTrue(result.IsSuccess,
                "An application that never registers an observer must keep the behaviour it had before the "
                + "feature existed: a dispatched step neither fails nor blocks its pipeline.");
            Assert.IsTrue(dispatchedStep.ExecutionCount >= 1,
                "The dispatched step must still run its body when no observer is registered.");
        }

        [TestMethod]
        public async Task WaitSchedulerExecutionStep_DoesNotNotifyTheDispatchObserver()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var waitedStep = new ScheduledWaitCountingPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowDispatchObserver<PersonDto, RecordingDispatchObserver>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var observer = (RecordingDispatchObserver)localServiceProvider
                .GetRequiredService<IPipelineDispatchObserver<PersonDto>>();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(waitedStep);

            var result = await invoker.InvokeAsync(person);

            var signaled = await Task.WhenAny(observer.Completed, Task.Delay(NotificationSettleWindowMilliseconds));

            Assert.AreNotSame(observer.Completed, signaled,
                "A scheduled step the pipeline waits for is not a dispatch: its outcome is already part of the "
                + "result the caller receives. Reporting it a second time through the observer would have an "
                + "application handle the same execution twice, once in the flow response and once in whatever "
                + "compensating routine the observer drives.");
            Assert.AreEqual(0, observer.OutcomeCount,
                "Nothing at all may be reported for a waited step, whatever its status.");

            Assert.IsTrue(result.IsSuccess,
                "A waited scheduled step that succeeds must leave the pipeline successful.");
            Assert.AreEqual(ScheduledWaitCountingPipelineStep.AppliedName, result.FlowResponse.Name,
                "The waited step must have been executed and applied to the item the caller receives; without "
                + "that this test would prove nothing more than that the step never ran.");
        }

        [TestMethod]
        public async Task DispatchObserver_RegisteredThroughTheServiceCollection_IsResolvedAndNotified()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var dispatchedStep = new PersonSetNameFireAndForgetPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowDispatchObserver<PersonDto, RecordingDispatchObserver>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var firstScope = localServiceProvider.CreateScope();
            var secondScope = localServiceProvider.CreateScope();

            var observerOfFirstScope = firstScope.ServiceProvider
                .GetRequiredService<IPipelineDispatchObserver<PersonDto>>();
            var observerOfSecondScope = secondScope.ServiceProvider
                .GetRequiredService<IPipelineDispatchObserver<PersonDto>>();

            var invoker = firstScope.ServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(dispatchedStep);

            await invoker.InvokeAsync(person);

            firstScope.Dispose();
            secondScope.Dispose();

            var outcome = await AwaitDispatchOutcomeAsync((RecordingDispatchObserver)observerOfFirstScope,
                "An observer registered through the service collection must be picked up by an invoker resolved "
                + "from the container; a registration that is never wired up leaves the application believing "
                + "its background work is being watched when it is not.");

            Assert.AreSame(observerOfFirstScope, observerOfSecondScope,
                "The registration must be a singleton. A scoped observer is captured from the scope that "
                + "resolved the invoker, yet it is only called once the dispatched execution ends — after that "
                + "scope has been disposed. It would then be invoked through a dead scope, or keep that scope "
                + "and its connections alive for the whole background execution.");

            Assert.AreEqual(PipelineDispatchStatusType.Completed, outcome.Status,
                "The container-resolved observer must receive the same outcome a directly supplied one does.");
            Assert.AreEqual(nameof(PersonSetNameFireAndForgetPipelineStep), outcome.StepName,
                $"The outcome must name the dispatched step. Got '{outcome.StepName}'.");
        }

        private static async Task<PipelineDispatchOutcome> AwaitDispatchOutcomeAsync(
            RecordingDispatchObserver observer,
            string failureMessage)
        {
            var signaled = await Task.WhenAny(
                observer.Completed,
                Task.Delay(DispatchNotificationTimeoutMilliseconds));

            Assert.AreSame(observer.Completed, signaled,
                failureMessage + $" Nothing was reported within {DispatchNotificationTimeoutMilliseconds}ms.");

            return await observer.Completed;
        }
    }
}
