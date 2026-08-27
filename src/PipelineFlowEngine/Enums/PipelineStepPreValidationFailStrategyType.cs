// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineFlowEngine
//  Author            : RzR
//  Created           : 20-07-2026 20:07
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-27 12:13
//  ***********************************************************************
//  <copyright file="PipelineStepPreValidationFailStrategyType.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

namespace RzR.PipelineFlowEngine.Enums
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Values that represent pipeline step pre-validation fail strategy types.
    ///     The strategy is honored ONLY when the pre-execution validation RETURNS FALSE. A pre-execution
    ///     validation that THROWS always halts the pipeline, whatever the configured strategy.
    /// </summary>
    /// =================================================================================================
    public enum PipelineStepPreValidationFailStrategyType
    {
        /// <summary>
        ///     An enum constant representing the undefined option. Treated as
        ///     <see cref="PipelineStop" />.
        /// </summary>
        Undefined,

        /// <summary>
        ///     An enum constant representing the pipeline stop option. The default strategy: a precondition
        ///     that returns false stops the whole pipeline with a failed result.
        /// </summary>
        PipelineStop,

        /// <summary>
        ///     An enum constant representing the step skip option. A precondition that returns false skips
        ///     only the owning step and the pipeline continues with the next one. Applies ONLY when the
        ///     precondition returns false; a precondition that throws always halts the pipeline.
        /// </summary>
        StepSkip
    }
}