// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:37
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-30 21:36
// ***********************************************************************
//  <copyright file="IPipelineFlowContext.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using PipelineFlowEngine.Enums;

#endregion

namespace PipelineFlowEngine.Abstractions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Interface for pipeline flow context.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// =================================================================================================
    public interface IPipelineFlowContext<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the pipeline flow fail execution strategy.
        /// </summary>
        /// <value>
        ///     The pipeline flow fail execution strategy.
        /// </value>
        /// =================================================================================================
        PipelineStepFailExecutionStrategyType FailExecutionStrategy { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets a value indicating whether this pipeline is enabled to collect pipeline step result.
        /// </summary>
        /// <value>
        ///     True if this pipeline is enabled step result collector, false if not.
        /// </value>
        /// =================================================================================================
        bool IsEnabledStepResultCollector { get; }
    }
}