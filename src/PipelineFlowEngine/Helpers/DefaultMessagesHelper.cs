// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-25 14:44
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 14:44
// ***********************************************************************
//  <copyright file="DefaultMessagesHelper.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

namespace PipelineFlowEngine.Helpers
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
        ///     A pipeline flow invoker messages.
        /// </summary>
        /// =================================================================================================
        internal static class PipelineFlowInvokerMessage
        {
            internal const string NoPipelineSteps = "Execution pipeline step list is empty!";
            internal const string InitExecStepAndStrategy = "Initialize supplied steps and their execution strategy!";
            internal const string TotalRegisteredStepInPipeline = "In pipeline was registered ['{0}'] steps!";
            internal const string InitExecutionStepXFromY = "Start execution step with index ['{0}'](nr. ['{1}']) of ['{2}'] steps!";
            internal const string ExecutedStepXWithStatusIsSuccess = "Step with index ['{0}'] finished work with status 'IsSuccess' => ['{1}']";
            internal const string ExecStepFailedMoveToNext = "Step execution failed, according to execution policy ['{0}'], move to next step";
            internal const string ExecStepFailedStepRetry = "Step execution failed, according to execution policy ['{0}'], retry execution";
            internal const string ExecStepFailedStepRetried = "Step execution failed, according to execution policy ['{0}'], retried index ['{1}'] of ['{2}']";
            internal const string ExecStepFailedStepRetryUsed = "Step execution failed, according to execution policy ['{0}'], retry action was already used";
            internal const string ExecStepFailedPipelineStop = "Step execution failed, according to execution policy ['{0}'], pipeline stopped execution";
            internal const string ExecPipelineFinished = "Pipeline finished work";
            internal const string PreValidationExecutionStep = "Pre-execution validation failed, break out from the pipeline step";
            internal const string PreValidationExecutionStepResult = "Pre-execution validation was finished with status ['{0}']";
        }
    }
}