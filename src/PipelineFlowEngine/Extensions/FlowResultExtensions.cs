// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-24 20:23
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 14:39
// ***********************************************************************
//  <copyright file="FlowResultExtensions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using RzR.PipelineFlowEngine.Models.Result;

#endregion

namespace RzR.PipelineFlowEngine.Extensions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A flow result extensions.
    /// </summary>
    /// =================================================================================================
    public static class FlowResultExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A FlowResult&lt;T&gt; extension method that converts a flowResult to a pipeline step
        ///     result.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="flowResult">The flowResult to act on.</param>
        /// <returns>
        ///     A PipeLineStepResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public static PipeLineStepResult<T> AsPipelineStepResult<T>(this FlowResult<T> flowResult) where T : class
            => (PipeLineStepResult<T>)flowResult;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A FlowResult&lt;T&gt; extension method that converts a flowResult to a pipeline
        ///     result.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="flowResult">The flowResult to act on.</param>
        /// <returns>
        ///     A PipeLineResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public static PipeLineResult<T> AsPipelineResult<T>(this FlowResult<T> flowResult) where T : class
            => (PipeLineResult<T>)flowResult;
    }
}