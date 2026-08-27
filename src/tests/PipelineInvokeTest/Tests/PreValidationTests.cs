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
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{

    [TestClass]
    public class PreValidationTests
    {

        private const string PreValidationRejectedText =
            "Pre-execution validation failed, break out from the pipeline";

        private const string PreValidationThrewText =
            "Pre-execution validation threw an exception, break out from the pipeline";

        private IServiceCollection _serviceCollection;

        [TestInitialize]
        public void Init() => _serviceCollection = BuildServiceCollection();

        [TestMethod]
        public async Task PreValidation_OnRetriedStep_IsEvaluatedExactlyOncePerStep_NotOncePerAttempt()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var step = new CountingPreValidationPipelineStep(2);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(step);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(3, step.ExecutionCount,
                "The step body must consume the whole retry budget: RetryIterations(2) + 1 = 3 executions. "
                + "This assertion exists so the next one cannot pass trivially.");
            Assert.AreEqual(1, step.PreValidationCallCount,
                "Pre-execution validation is evaluated ONCE PER STEP, outside the retry loop. Evaluating it "
                + "inside the retry loop would produce one call per attempt (3 here) and would re-run any side "
                + "effect the validator performs — reservations, locks, billed calls.");
            Assert.IsFalse(result.IsSuccess,
                "The step failed every attempt, so the pipeline must report failure.");
        }

        [TestMethod]
        public async Task PreValidation_ReturningFalse_StopsPipeline_WithoutExecutingTheStep()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var step = new FailingPreValidationPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(step);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, step.ExecutionCount,
                "A step whose pre-execution validation returned false must never have its body executed.");
            Assert.AreEqual("TestName", person.Name,
                "The step body mutates the pipeline item, so an unchanged item proves the body never ran.");
            Assert.IsFalse(result.IsSuccess,
                "A failed pre-execution validation must fail the pipeline.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "A failed pre-execution validation must end the pipeline with the Fail status.");
            Assert.AreEqual(PipelineStateType.Finish, result.State,
                "A failed pre-execution validation must end the pipeline in the Finish state.");

            Assert.IsFalse(
                result.StepResults.Any(x => x.StepName == nameof(FailingPreValidationPipelineStep)),
                "A step rejected by pre-execution validation produces no step result at all — it is absent "
                + "from StepResults rather than present as a failed entry.");

            Assert.IsTrue(result.Events.Any(x => x.EventLevel == LogLevel.Error),
                "A rejected pre-execution validation must be surfaced as an Error-level flow event.");

            Assert.AreEqual(PreValidationRejectedText, result.Message,
                "A pipeline stopped by a rejected precondition must say so on the result Message. Since the "
                + "step produces no step result, the Message is the only place a caller can learn WHY nothing "
                + "ran without walking the event stream.");
        }

        [TestMethod]
        public async Task PreValidation_ThatThrows_StopsPipeline_WithAMessageDistinctFromARejectedPrecondition()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var throwingStep = new ThrowingPreValidationPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(throwingStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, throwingStep.ExecutionCount,
                "A step whose pre-execution validation threw must never have its body executed.");
            Assert.AreEqual("TestName", person.Name,
                "The step body mutates the pipeline item, so an unchanged item proves the body never ran.");
            Assert.IsFalse(result.IsSuccess,
                "A pre-execution validation that threw must fail the pipeline.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "A pre-execution validation that threw must end the pipeline with the Fail status.");

            Assert.IsNotNull(result.Message,
                "A pipeline stopped by a faulted precondition must expose a diagnostic message.");
            StringAssert.Contains(result.Message, PreValidationThrewText,
                "A precondition that COULD NOT BE EVALUATED is a different failure from one that evaluated and "
                + "REJECTED the item: the first usually means a broken dependency and warrants investigation, "
                + "the second is an expected business outcome. The Message must let the caller tell them apart.");
            StringAssert.Contains(result.Message, ThrowingPreValidationPipelineStep.ThrownMessage,
                "The message must embed the text of the exception raised by the precondition, otherwise the "
                + "caller learns that evaluation failed but never learns why.");
            Assert.AreNotEqual(PreValidationRejectedText, result.Message,
                "A faulted precondition must never reuse the message of a rejected one.");

            Assert.IsTrue(result.Events.Any(x => x.EventLevel == LogLevel.Critical),
                "A precondition that threw is recorded at Critical, above the Error level used for a rejected "
                + "one: an unevaluable precondition is an operational fault, not a business decision.");

            var criticalEvent = result.Events.First(x => x.EventLevel == LogLevel.Critical);

            Assert.IsNotNull(criticalEvent.Exception,
                "The Critical event must carry the originating exception so the stack trace reaches the sink.");
            Assert.AreEqual(ThrowingPreValidationPipelineStep.ThrownMessage, criticalEvent.Exception.Message,
                "The Critical event must carry the exception raised by the precondition itself.");

            Assert.IsFalse(
                result.StepResults.Any(x => x.StepName == nameof(ThrowingPreValidationPipelineStep)),
                "A step never entered because its precondition faulted produces no step result at all.");
        }

        [TestMethod]
        public async Task PreValidation_ReturningFalse_UnderStepMoveToNext_StillStopsThePipeline()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var rejectedStep = new FailingPreValidationPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextMoveToNext>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(rejectedStep);
            invoker.AddPipelineStep(lateStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, rejectedStep.ExecutionCount,
                "A step whose pre-execution validation returned false must never have its body executed.");
            Assert.AreEqual(0, lateStep.ExecutionCount,
                "Pre-execution validation bypasses the FailExecutionStrategy entirely: even under "
                + "StepMoveToNext, a rejected precondition stops the WHOLE pipeline and later steps never run.");
            Assert.IsFalse(result.IsSuccess,
                "A failed pre-execution validation must fail the pipeline even under StepMoveToNext.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "A failed pre-execution validation must end the pipeline with the Fail status even under "
                + "StepMoveToNext.");
        }

        [TestMethod]
        public async Task PreValidation_ReturningFalse_UnderStepSkip_SkipsOnlyThatStep_AndLaterStepsStillRun()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var skippedStep = new SkippablePreValidationPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(skippedStep);
            invoker.AddPipelineStep(lateStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, skippedStep.ExecutionCount,
                "A step whose precondition returned false must never have its body executed, whatever it "
                + "asked the pipeline to do afterwards.");
            Assert.AreEqual("TestName", person.Name,
                "The skipped step body mutates the pipeline item, so an unchanged item proves the body never "
                + "ran.");
            Assert.AreEqual(1, lateStep.ExecutionCount,
                "A step that declares StepSkip removes only ITSELF from the run; every later step must still "
                + "execute exactly once, otherwise one optional step silently cancels the rest of the work.");
            Assert.IsTrue(result.IsSuccess,
                "Skipping a step that declared itself skippable is an expected outcome, not a failure, so the "
                + "caller must still see a successful pipeline.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                "A pipeline whose only unmet precondition belonged to a skippable step finishes with the "
                + "Success status.");
        }

        [TestMethod]
        public async Task PreValidation_SkippedStep_IsRecordedInStepResults_AsSkippedByPreValidation()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var skippedStep = new SkippablePreValidationPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(skippedStep);
            invoker.AddPipelineStep(lateStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(1,
                result.StepResults.Count(x =>
                    x.StepIteration == PipelineFlowStepIterationType.SkippedByPreValidation),
                "The skip must leave exactly one trace in StepResults. Without it a run in which an optional "
                + "step was skipped looks byte-for-byte like a run in which every step did its work, and a "
                + "caller reconciling what happened has no way to tell the two apart.");

            var skippedEntry = result.StepResults
                .Single(x => x.StepIteration == PipelineFlowStepIterationType.SkippedByPreValidation);

            Assert.AreEqual(nameof(SkippablePreValidationPipelineStep), skippedEntry.StepName,
                "The entry must name the step that was skipped; a skip trace that does not identify its step "
                + "is unusable once a pipeline holds more than one skippable step.");
            Assert.AreEqual(PipelineStateType.Skip, skippedEntry.StepResult.State,
                "The recorded step result must carry the Skip state, so the entry is not mistaken for a step "
                + "that ran to completion.");
            Assert.AreEqual(PipelineStatusType.Undefined, skippedEntry.StepResult.Status,
                "A step that never ran has no outcome to report, so its status stays Undefined rather than "
                + "claiming Success or Fail.");
            Assert.IsFalse(skippedEntry.StepResult.IsSuccess,
                "A skipped step did no work, so its own entry must not claim success even though the pipeline "
                + "as a whole succeeded.");

            Assert.AreEqual(1,
                result.StepResults.Count(x => x.StepName == nameof(ExecutionCountingPipelineStep)),
                "The step following the skipped one must still report its own ordinary result.");
        }

        [TestMethod]
        public async Task PreValidation_SkippedStep_RaisesWarningNotError_SoThePipelineStillReportsSuccess()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var skippedStep = new SkippablePreValidationPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(skippedStep);

            var result = await invoker.InvokeAsync(person);

            Assert.IsFalse(
                result.Events.Any(x => x.EventLevel == LogLevel.Error || x.EventLevel == LogLevel.Critical),
                "A skip must never be recorded at Error or Critical. The flow result treats a single event at "
                + "either level as a permanent failure, so one such event would leave IsSuccess false even "
                + "though the pipeline reached the Success status and every remaining step completed.");
            Assert.IsTrue(result.Events.Any(x => x.EventLevel == LogLevel.Warning),
                "The skip must still be visible in the event stream at Warning: a step that quietly did not "
                + "run and left no trace anywhere is indistinguishable from a step that was never registered.");
            Assert.IsTrue(result.IsSuccess,
                "With no Error or Critical event recorded, the pipeline reports success.");
        }

        [TestMethod]
        public async Task PreValidation_ThatThrows_UnderStepSkip_StillHaltsThePipeline()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var throwingStep = new ThrowingSkippablePreValidationPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(throwingStep);
            invoker.AddPipelineStep(lateStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, throwingStep.ExecutionCount,
                "A step whose precondition threw must never have its body executed.");
            Assert.IsFalse(result.IsSuccess,
                "StepSkip means 'skip me when the precondition says no'. A precondition that threw said "
                + "nothing at all, so the pipeline must fail closed instead of assuming the step was optional.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "An unevaluable precondition ends the pipeline with the Fail status even for a skippable step.");
            StringAssert.Contains(result.Message, PreValidationThrewText,
                "The result must report the failure as a precondition that could not be evaluated, which is a "
                + "broken dependency to investigate rather than a business rule that declined the item.");
            Assert.IsTrue(result.Events.Any(x => x.EventLevel == LogLevel.Critical),
                "An unevaluable precondition is an operational fault and is recorded at Critical.");

            Assert.IsFalse(
                result.StepResults.Any(x =>
                    x.StepIteration == PipelineFlowStepIterationType.SkippedByPreValidation),
                "Nothing was skipped here — the pipeline stopped. Recording a skip entry would tell the caller "
                + "the run continued past this step, which it did not.");
            Assert.AreEqual(0, lateStep.ExecutionCount,
                "Once a precondition could not be evaluated the pipeline halts, so later steps never run.");
        }

        [TestMethod]
        public void PreValidation_DefaultFailStrategy_IsPipelineStop_WhenNotOverridden()
        {

            var step = new FailingPreValidationPipelineStep();

            var failStrategy = step.PreValidationFailStrategy;

            Assert.AreEqual(PipelineStepPreValidationFailStrategyType.PipelineStop, failStrategy,
                "A step that says nothing about what should happen when its precondition rejects the item "
                + "stops the pipeline. Defaulting to StepSkip instead would let every existing step silently "
                + "drop itself out of a flow that used to fail loudly.");
        }

        [TestMethod]
        public async Task PreValidation_UnderStepSkip_BehavesTheSameForEveryFailExecutionStrategy()
        {

            await AssertSkippedUnderContextAsync<PersonPipelineContextPipelineStop>(
                nameof(PipelineStepFailExecutionStrategyType.PipelineStop));
            await AssertSkippedUnderContextAsync<PersonPipelineContextMoveToNext>(
                nameof(PipelineStepFailExecutionStrategyType.StepMoveToNext));
            await AssertSkippedUnderContextAsync<PersonPipelineContext2>(
                nameof(PipelineStepFailExecutionStrategyType.StepRetry));
        }

        [TestMethod]
        public async Task PreValidation_ObservingTheCancellationToken_PropagatesCancellation()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var waitingStep = new CancellationObservingPreValidationPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(waitingStep);

            using var cancellationTokenSource = new CancellationTokenSource();

            var invocation = invoker.InvokeAsync(person, cancellationTokenSource.Token);
            await waitingStep.PreValidationEntered;
            cancellationTokenSource.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                () => invocation,
                "A caller who cancels must get a cancellation back. Turning the abort into a failed pipeline "
                + "result would report the precondition as having rejected the item, and the caller would "
                + "conclude a business rule declined work it had itself called off.");

            Assert.AreEqual(0, waitingStep.ExecutionCount,
                "The precondition never returned, so the step body must never have run.");
        }

        [TestMethod]
        public async Task PreValidation_ThrowingForeignCancellation_HaltsAsAFaultedPrecondition()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var foreignCancellationStep = new ForeignCancellationPreValidationPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(foreignCancellationStep);

            using var cancellationTokenSource = new CancellationTokenSource();

            var result = await invoker.InvokeAsync(person, cancellationTokenSource.Token);

            Assert.IsFalse(cancellationTokenSource.IsCancellationRequested,
                "The caller never cancelled; only the step's own token was cancelled.");
            Assert.IsFalse(result.IsSuccess,
                "A precondition aborted by its own timeout produced no answer, so the pipeline must fail "
                + "instead of running the step.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "The pipeline ends with the Fail status.");
            StringAssert.Contains(result.Message, PreValidationThrewText,
                "A precondition that timed out internally is a precondition that could not be evaluated, and "
                + "must be reported as such. Mistaking it for a cancellation of the pipeline would throw a "
                + "cancellation at a caller who never asked for one and hand it no result at all.");
            Assert.AreEqual(0, foreignCancellationStep.ExecutionCount,
                "The step body must never run once its precondition faulted.");
        }

        [TestMethod]
        public async Task PreValidation_ReceivesTheTokenPassedToInvokeAsync()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var waitingStep = new CancellationObservingPreValidationPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(waitingStep);

            using var cancellationTokenSource = new CancellationTokenSource();

            var invocation = invoker.InvokeAsync(person, cancellationTokenSource.Token);
            await waitingStep.PreValidationEntered;

            Assert.IsTrue(waitingStep.ObservedToken.CanBeCanceled,
                "The precondition must receive the caller's token, not CancellationToken.None. A precondition "
                + "is where long-running work such as a remote lookup happens, and one handed a token that "
                + "can never be cancelled keeps running after the caller walked away.");
            Assert.IsFalse(waitingStep.ObservedToken.IsCancellationRequested,
                "Nothing was cancelled yet, so the observed token must still be live.");

            cancellationTokenSource.Cancel();

            Assert.IsTrue(waitingStep.ObservedToken.IsCancellationRequested,
                "Cancelling the caller's source must be visible on the very token the precondition holds, "
                + "which is what makes it the caller's token rather than a copy.");

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => invocation);
        }

        [TestMethod]
        public async Task PreValidation_SkippedStep_ProducesNoStepResult_WhenCollectorDisabled()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var skippedStep = new SkippablePreValidationPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextNoCollector>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(skippedStep);
            invoker.AddPipelineStep(lateStep);

            var result = await invoker.InvokeAsync(person);

            Assert.IsFalse(result.StepResults.Any(),
                "Switching the step result collector off suppresses the skip entry exactly like every other "
                + "step result; the collector flag must not become a second way of turning skipping on.");
            Assert.IsTrue(result.Events.Any(x => x.EventLevel == LogLevel.Warning),
                "The Warning event is not part of the collected step results, so an operator keeps the only "
                + "trace of the skip even with the collector switched off.");
            Assert.AreEqual(0, skippedStep.ExecutionCount,
                "Which steps run must not depend on whether their results are collected.");
            Assert.AreEqual(1, lateStep.ExecutionCount,
                "The step after the skipped one still runs exactly once with the collector switched off.");
            Assert.IsTrue(result.IsSuccess,
                "The pipeline still succeeds.");
        }

        [TestMethod]
        public async Task PreValidation_ThatThrows_EmitsExactlyOneCriticalEventAndNoError()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var throwingStep = new ThrowingPreValidationPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(throwingStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(1, result.Events.Count(x => x.EventLevel == LogLevel.Critical),
                "One faulted precondition is one incident and must be recorded once. A duplicate entry doubles "
                + "the alert count of whatever consumes these events.");
            Assert.AreEqual(0, result.Events.Count(x => x.EventLevel == LogLevel.Error),
                "The same failure must not also be recorded at Error. Reported at two levels it reads as two "
                + "separate problems, and the Error copy carries no exception to diagnose either of them.");
        }

        [TestMethod]
        public async Task PreValidation_ReturningFalse_EmitsExactlyOneErrorEvent()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var rejectedStep = new FailingPreValidationPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(rejectedStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(1, result.Events.Count(x => x.EventLevel == LogLevel.Error),
                "A rejected precondition is one business decision and must be recorded once.");
            Assert.AreEqual(0, result.Events.Count(x => x.EventLevel == LogLevel.Critical),
                "A precondition that evaluated and said no is not an operational fault, so nothing may be "
                + "recorded at Critical; doing so would page someone for an expected outcome.");
        }

        [TestMethod]
        public async Task PreValidation_ReturningFalse_UnderUndefinedFailStrategy_StopsThePipeline()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var undefinedStrategyStep = new UndefinedPreValidationFailStrategyPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(undefinedStrategyStep);
            invoker.AddPipelineStep(lateStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, undefinedStrategyStep.ExecutionCount,
                "A step whose pre-execution validation returned false must never have its body executed.");
            Assert.AreEqual("TestName", person.Name,
                "The step body mutates the pipeline item, so an unchanged item proves the body never ran.");
            Assert.AreEqual(0, lateStep.ExecutionCount,
                "Undefined is not a third behaviour: a step that says nothing about what to do when its "
                + "precondition rejects the item stops the WHOLE pipeline, exactly as PipelineStop does, so no "
                + "later step may run. Treating it as a skip instead would let a step drop itself out of a flow "
                + "that must have halted.");
            Assert.IsFalse(result.IsSuccess,
                "A precondition rejected under the Undefined strategy must fail the pipeline.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "A precondition rejected under the Undefined strategy must end the pipeline with the Fail "
                + "status.");
            Assert.AreEqual(PreValidationRejectedText, result.Message,
                "The halt must be reported with the same message a step declaring PipelineStop produces; a "
                + "caller reading the result cannot be expected to know which of the two the step declared.");
            Assert.IsTrue(result.Events.Any(x => x.EventLevel == LogLevel.Error),
                "The halt must be surfaced as an Error-level flow event, like any other rejected precondition.");

            Assert.IsFalse(
                result.StepResults.Any(x =>
                    x.StepIteration == PipelineFlowStepIterationType.SkippedByPreValidation),
                "Nothing was skipped here — the pipeline stopped. A skip entry would tell the caller the run "
                + "continued past this step, which it did not.");
        }

        [TestMethod]
        public async Task PreValidation_ReturningFalse_UnderStepSkip_SkipsAScheduledStep_WithoutSchedulingIt()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var scheduledSkippableStep = new SkippableScheduledPipelineStep();
            var lateStep = new ExecutionCountingPipelineStep(90);

            var faultingScheduler = new FaultingMethodScheduler();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var logger = localServiceProvider.GetRequiredService<ILogger<PipelineFlowInvoker<PersonDto>>>();

            var invoker = new PipelineFlowInvoker<PersonDto>(
                new PersonPipelineContextPipelineStop(),
                logger,
                new List<IPipelineFlowStep<PersonDto>> { scheduledSkippableStep, lateStep },
                faultingScheduler);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, faultingScheduler.ScheduleCallCount,
                "A skipped step must never reach the scheduler. Scheduling it first and skipping it afterwards "
                + "would book a job, an interval and a background execution for work the precondition already "
                + "declined.");
            Assert.AreEqual(0, scheduledSkippableStep.ExecutionCount,
                "The body of a skipped scheduled step must never run.");
            Assert.AreEqual("TestName", person.Name,
                "The skipped step body mutates the pipeline item, so an unchanged item proves the body never "
                + "ran.");
            Assert.AreEqual(1,
                result.StepResults.Count(x =>
                    x.StepIteration == PipelineFlowStepIterationType.SkippedByPreValidation),
                "A skipped scheduled step leaves the same single skip trace as a skipped simple one; how a step "
                + "would have been executed does not change how its skip is reported.");

            var skippedEntry = result.StepResults
                .Single(x => x.StepIteration == PipelineFlowStepIterationType.SkippedByPreValidation);

            Assert.AreEqual(nameof(SkippableScheduledPipelineStep), skippedEntry.StepName,
                "The entry must name the scheduled step that was skipped.");
            Assert.IsFalse(
                result.StepResults.Any(x => x.StepIteration == PipelineFlowStepIterationType.Dispatched),
                "No dispatch may be recorded for a step that was never handed to the scheduler.");
            Assert.AreEqual(1, lateStep.ExecutionCount,
                "The step ordered after the skipped scheduled one must still execute exactly once.");
            Assert.IsTrue(result.IsSuccess,
                "Skipping a scheduled step that declared itself skippable is an expected outcome, so the "
                + "pipeline still reports success.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                "The pipeline finishes with the Success status.");
        }

        private static async Task AssertSkippedUnderContextAsync<TContext>(string failExecutionStrategyName)
            where TContext : class, IPipelineFlowContext<PersonDto>
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var skippedStep = new SkippablePreValidationPipelineStep();

            var serviceCollection = BuildServiceCollection();
            serviceCollection.RegisterPipelineFlowEngine<PersonDto, TContext>();

            var localServiceProvider = serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(skippedStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, skippedStep.ExecutionCount,
                $"Under the {failExecutionStrategyName} fail execution strategy the skipped step body must "
                + "still never run. The two settings answer different questions — one what to do when a step "
                + "FAILS, the other what to do when a step must not be ENTERED — and neither may override the "
                + "other.");
            Assert.AreEqual(1,
                result.StepResults.Count(x =>
                    x.StepIteration == PipelineFlowStepIterationType.SkippedByPreValidation),
                $"Under the {failExecutionStrategyName} fail execution strategy the skip is recorded exactly "
                + "once, identically to every other strategy.");
            Assert.IsTrue(result.IsSuccess,
                $"Under the {failExecutionStrategyName} fail execution strategy a skipped step still leaves "
                + "the pipeline successful.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                $"Under the {failExecutionStrategyName} fail execution strategy the pipeline still finishes "
                + "with the Success status.");
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
