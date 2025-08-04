// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:34
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 16:04
// ***********************************************************************
//  <copyright file="PipeLineStepResult.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using PipelineFlowEngine.Extensions;

#endregion

namespace PipelineFlowEngine.Models.Result
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Encapsulates the result of a pipe line step. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <seealso cref="T:PipelineFlowEngine.Models.Result.FlowResult{T}"/>
    /// =================================================================================================
    public sealed class PipeLineStepResult<T> : FlowResult<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipeLineStepResult{T}"/> class.
        /// </summary>
        /// =================================================================================================
        public PipeLineStepResult() { }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the success.
        /// </summary>
        /// <returns>
        ///     A PipeLineStepResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public static PipeLineStepResult<T> Success()
            => new PipeLineStepResult<T>().SetSuccess().AsPipelineStepResult();

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the failure.
        /// </summary>
        /// <returns>
        ///     A PipeLineStepResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public static PipeLineStepResult<T> Failure()
            => new PipeLineStepResult<T>().SetFailure().AsPipelineStepResult();
    }
}