// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-25 12:41
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 14:35
// ***********************************************************************
//  <copyright file="InternalTypeExtensions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System;

#endregion

namespace RzR.PipelineFlowEngine.Extensions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     An internal type extensions.
    /// </summary>
    /// =================================================================================================
    internal static class InternalTypeExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A Type extension method that query if 'sourceType' is typeof <see cref="PipeLineResult{T}"/>.
        /// </summary>
        /// <param name="sourceType">The sourceType to act on.</param>
        /// <returns>
        ///     True if <see cref="PipeLineResult{T}"/>, false if not.
        /// </returns>
        /// =================================================================================================
        internal static bool IsPipeLineResult(this Type sourceType)
            => sourceType.Name == typeof(PipeLineResult<>).Name || sourceType.Name == typeof(PipelineFlowInvoker<>).Name;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A Type extension method that query if 'sourceType' is typeof <see cref="PipeLineStepResult{T}"/>
        ///     .
        /// </summary>
        /// <param name="sourceType">The sourceType to act on.</param>
        /// <returns>
        ///     True if <see cref="PipeLineStepResult{T}"/>, false if not.
        /// </returns>
        /// =================================================================================================
        internal static bool IsPipeLineStepResult(this Type sourceType)
            => sourceType.Name == typeof(PipeLineStepResult<>).Name;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A Type extension method that gets flow name.
        /// </summary>
        /// <param name="sourceType">The sourceType to act on.</param>
        /// <returns>
        ///     The flow name.
        /// </returns>
        /// =================================================================================================
        internal static string GetFlowName(this Type sourceType)
        {
            if (sourceType.IsPipeLineResult())
                return "PIPELINE";
            if (sourceType.IsPipeLineStepResult())
                return "PIPELINESTEP";

            return "FLOW";
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A Type extension method that query if 'sourceType' is pipeline flow step.
        /// </summary>
        /// <param name="sourceType">The sourceType to act on.</param>
        /// <returns>
        ///     True if pipeline flow step, false if not.
        /// </returns>
        /// =================================================================================================
        internal static bool IsPipelineFlowStep(this Type sourceType)
            => sourceType.IsGenericType
                && (sourceType.GetGenericTypeDefinition() == typeof(PipeLineFlowStep<>)
                    || sourceType.GetGenericTypeDefinition() == typeof(IPipelineFlowStep<>));

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A Type extension method that query if 'sourceType' is pipeline flow step.
        /// </summary>
        /// <param name="sourceType">The sourceType to act on.</param>
        /// <returns>
        ///     True if pipeline flow step, false if not.
        /// </returns>
        /// =================================================================================================
        internal static bool IsPipelineFlowStep<TImplementation>(this Type sourceType) where TImplementation : class
            => sourceType == typeof(PipeLineFlowStep<TImplementation>) || sourceType == typeof(IPipelineFlowStep<TImplementation>);
    }
}