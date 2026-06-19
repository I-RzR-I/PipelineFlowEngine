// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-26 16:15
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-26 16:17
// ***********************************************************************
//  <copyright file="AddPipelineStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

#endregion

namespace RzR.PipelineFlowEngine.Models
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     An add pipeline step.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// =================================================================================================
    public class AddPipelineStep<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the amount to increment by.
        /// </summary>
        /// <value>
        ///     The amount to increment by.
        /// </value>
        /// =================================================================================================
        public IPipelineFlowStep<T> Step { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the step execution strategy.
        /// </summary>
        /// <value>
        ///     The step execution strategy.
        /// </value>
        /// =================================================================================================
        public PipelineStepExecutionStrategyType StepExecutionStrategy { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="AddPipelineStep{T}"/> class.
        /// </summary>
        /// =================================================================================================
        public AddPipelineStep() { }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="AddPipelineStep{T}"/> class.
        /// </summary>
        /// <param name="step">The amount to increment by.</param>
        /// <param name="stepExecutionStrategy">The step execution strategy.</param>
        /// =================================================================================================
        public AddPipelineStep(IPipelineFlowStep<T> step, PipelineStepExecutionStrategyType stepExecutionStrategy)
        {
            Step = step;
            StepExecutionStrategy = stepExecutionStrategy;
        }
    }
}