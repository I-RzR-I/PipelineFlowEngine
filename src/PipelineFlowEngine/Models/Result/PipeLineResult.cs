// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:34
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 16:04
// ***********************************************************************
//  <copyright file="PipeLineResult.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using DomainCommonExtensions.ArraysExtensions;
using PipelineFlowEngine.Extensions;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace PipelineFlowEngine.Models.Result
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Encapsulates the result of a pipe line. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <seealso cref="T:PipelineFlowEngine.Models.Result.FlowResult{T}"/>
    /// =================================================================================================
    public sealed class PipeLineResult<T> : FlowResult<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the step results.
        /// </summary>
        /// <value>
        ///     The step results.
        /// </value>
        /// =================================================================================================
        public IEnumerable<PipelineFlowStepResult<T>> StepResults { get; private set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipeLineResult{T}"/> class.
        /// </summary>
        /// =================================================================================================
        public PipeLineResult() { }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipeLineResult{T}"/> class.
        /// </summary>
        /// <param name="stepResults">The step results.</param>
        /// =================================================================================================
        public PipeLineResult(IEnumerable<PipelineFlowStepResult<T>> stepResults)
            => StepResults = stepResults;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the success.
        /// </summary>
        /// <returns>
        ///     A PipeLineResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public static PipeLineResult<T> Success()
            => new PipeLineResult<T>().SetSuccess().AsPipelineResult();

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the failure.
        /// </summary>
        /// <returns>
        ///     A PipeLineResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public static PipeLineResult<T> Failure()
            => new PipeLineResult<T>().SetFailure().AsPipelineResult();

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Appends a step result.
        /// </summary>
        /// <param name="stepResult">The step result.</param>
        /// <returns>
        ///     A PipeLineResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public PipeLineResult<T> AppendStepResult(PipelineFlowStepResult<T> stepResult)
        {
            StepResults = (StepResults ?? new List<PipelineFlowStepResult<T>>()).ToArray().AppendItem(stepResult);

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets step results.
        /// </summary>
        /// <param name="stepResults">The step results.</param>
        /// <returns>
        ///     A PipeLineResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public PipeLineResult<T> SetStepResults(IEnumerable<PipelineFlowStepResult<T>> stepResults)
        {
            StepResults = stepResults;

            return this;
        }
    }
}