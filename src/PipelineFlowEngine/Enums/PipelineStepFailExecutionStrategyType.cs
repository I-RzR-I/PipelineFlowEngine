// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:32
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-23 23:39
// ***********************************************************************
//  <copyright file="PipelineStepFailExecutionStrategyType.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

namespace PipelineFlowEngine.Enums
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Values that represent pipeline step fail execution strategy types.
    /// </summary>
    /// =================================================================================================
    public enum PipelineStepFailExecutionStrategyType
    {
        /// <summary>
        ///     An enum constant representing the undefined option.
        /// </summary>
        Undefined,

        /// <summary>
        ///     An enum constant representing the pipeline stop option.
        /// </summary>
        PipelineStop,

        /// <summary>
        ///     An enum constant representing the step move to next option.
        /// </summary>
        StepMoveToNext,

        /// <summary>
        ///     An enum constant representing the step retry option.
        /// </summary>
        StepRetry
    }
}