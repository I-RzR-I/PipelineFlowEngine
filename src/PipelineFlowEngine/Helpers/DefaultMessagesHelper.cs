// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-25 14:44
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-27 12:13
// ***********************************************************************
//  <copyright file="DefaultMessagesHelper.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

namespace RzR.PipelineFlowEngine.Helpers
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A default messages helper.
    /// </summary>
    /// =================================================================================================
    internal static class DefaultMessagesHelper
    {
        internal const string FormatEventLog = "['{0}'] - {1}";

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A flow result event messages.
        /// </summary>
        /// =================================================================================================
        internal static class FlowResultEventMessage
        {
            internal const string SetSuccess = "['{0}'] - Set flow success";
            internal const string SetSuccessWithResult = "[{'0}'] - Set flow success with result/response";
            internal const string SetFailure = "['{0}'] - Set flow failure";
            internal const string SetFailureWithMessage = "['{0}'] - Set flow failure with message";
            internal const string SetResult = "['{0}'] - Set flow result";
            internal const string SeMessage = "['{0}'] - Changed flow message from ['{1}'] => ['{2}']";
            internal const string SetStatus = "['{0}'] - Changed flow status from ['{1}'] => ['{2}']";
            internal const string SetState = "['{0}'] - Changed flow state from ['{1}'] => ['{2}']";
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A pipeline flow step registration messages.
        /// </summary>
        /// =================================================================================================
        internal static class PipelineFlowStepRegistrationMessage
        {
            internal const string TypeIsNotPipelineStep = "The type ['{0}'] is not an instantiable implementation of the pipeline step contract for the pipeline item type ['{1}']!";
            internal const string StepAtIndexIsNull = "The pipeline step at index ['{0}'] is null or does not supply a step instance!";
            internal const string SingletonLifetimeNotSupported = "PipelineFlowInvoker<T> holds mutable per-invocation state and cannot be registered as a Singleton. Use ServiceLifetime.Scoped or ServiceLifetime.Transient.";
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A pipeline flow invoker messages.
        /// </summary>
        /// =================================================================================================
        internal static class PipelineFlowInvokerMessage
        {
            internal const string NoPipelineSteps = "Execution pipeline step list is empty!";
            internal const string ConcurrentInvocationNotSupported = "PipelineFlowInvoker<T> does not support concurrent invocation; resolve one invoker per invocation.";
            internal const string InitExecStepAndStrategy = "Initialize supplied steps and their execution strategy!";
            internal const string TotalRegisteredStepInPipeline = "In pipeline was registered ['{0}'] steps!";
            internal const string InitExecutionStepXFromY = "Start execution step with index ['{0}'](nr. ['{1}']) of ['{2}'] steps!";
            internal const string ExecutedStepXWithStatusIsSuccess = "Step with index ['{0}'] finished work with status 'IsSuccess' => ['{1}']";
            internal const string ExecStepFailedMoveToNext = "Step execution failed, according to execution policy ['{0}'], move to next step";
            internal const string ExecStepFailedStepRetry = "Step execution failed, according to execution policy ['{0}'], retry execution";
            internal const string ExecStepFailedStepRetried = "Step execution failed, according to execution policy ['{0}'], retried index ['{1}'] of ['{2}']";
            internal const string ExecStepFailedStepRetryUsed = "Step execution failed, according to execution policy ['{0}'], retry action was already used";
            internal const string ExecStepFailedPipelineStop = "Step execution failed, according to execution policy ['{0}'], pipeline stopped execution";
            internal const string ExecStepFailedPipelineStopWithError = "Step execution failed, according to execution policy ['{0}'], pipeline stopped execution. Error: ['{1}']";
            internal const string ExecStepXThrewException = "Step with index ['{0}'] threw an exception during execution; converted to a failed step result. Error: ['{1}']";
            internal const string ExecPipelineFinished = "Pipeline finished work";
            internal const string PreValidationExecutionStepHalt = "Pre-execution validation failed, break out from the pipeline";
            internal const string PreValidationExecutionStepSkipped = "Pre-execution validation failed, step ['{0}'] was skipped according to its pre-validation fail strategy ['{1}']";
            internal const string PreValidationExecutionStepThrewException = "Pre-execution validation threw an exception, break out from the pipeline. Error: ['{0}']";
            internal const string PreValidationExecutionStepResult = "Pre-execution validation was finished with status ['{0}']";
            internal const string ScheduledStepXDispatched = "Scheduled step ['{0}'] dispatched (fire-and-forget); result is not awaited.";
            internal const string ScheduledStepXDispatchFaulted = "Scheduled step ['{0}'] dispatched (fire-and-forget) faulted in the background.";
            internal const string ScheduledStepXStopFaulted = "Scheduled step ['{0}'] failed to stop after the pipeline cancellation was requested.";
            internal const string ScheduledStepXDispatchNotObserved = "Scheduled step ['{0}'] was dispatched (fire-and-forget); its execution outcome is not observed by the pipeline.";
            internal const string ScheduledStepXNotResolvableInScope = "Scheduled step ['{0}'] was dispatched (fire-and-forget) but could not be resolved from a new service scope; the originally supplied step instance and its dependencies are reused.";
            internal const string DispatchObserverForStepXThrew = "The registered dispatch completion observer threw while reporting the outcome of the dispatched step ['{0}']; the exception was swallowed.";
            internal const string ScheduledStepXDispatchedWithoutScope = "Scheduled step ['{0}'] was dispatched (fire-and-forget) without a service scope factory, so it keeps running against the service scope that resolved the invoker; that scope is usually disposed before the step ends.";
            internal const string ScheduledStepXNoObservedOutcome = "Scheduled step ['{0}'] completed without producing an observed execution outcome; the step never reported a result to the pipeline.";
        }
    }
}