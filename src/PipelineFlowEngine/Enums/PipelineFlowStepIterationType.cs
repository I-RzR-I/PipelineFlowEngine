// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-24 18:16
// 
//  Last Modified By : RzR
//  Last Modified On : 27-08-2026 12:13
// ***********************************************************************
//  <copyright file="PipelineFlowStepIterationType.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

namespace RzR.PipelineFlowEngine.Enums
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Values that represent pipeline flow step iteration types.
    /// </summary>
    /// =================================================================================================
    public enum PipelineFlowStepIterationType
    {
        /// <summary>
        ///     An enum constant representing the first execution option.
        /// </summary>
        FirstExecution,

        /// <summary>
        ///     An enum constant representing the retry execution option.
        /// </summary>
        RetryExecution,

        /// <summary>
        ///     An enum constant representing a step that was dispatched (fire-and-forget) to the scheduler.
        ///     The pipeline does not observe the execution outcome of such a step.
        /// </summary>
        Dispatched,

        /// <summary>
        ///     An enum constant representing a step that was not executed because its pre-execution
        ///     validation returned false and its pre-validation fail strategy is
        ///     <see cref="PipelineStepPreValidationFailStrategyType.StepSkip"/>.
        /// </summary>
        SkippedByPreValidation
    }
}