// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-25 12:41
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-27 12:13
// ***********************************************************************
//  <copyright file="InternalTypeExtensions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using RzR.Extensions.Domain.Primitives;
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
        ///     A Type extension method that query if 'sourceType' is an instantiable implementation of
        ///     <see cref="IPipelineFlowStep{TPipelineItem}"/>, regardless of how deep in the inheritance
        ///     chain the interface is introduced.
        /// </summary>
        /// <typeparam name="TPipelineItem">Type of the pipeline item.</typeparam>
        /// <param name="sourceType">The sourceType to act on.</param>
        /// <returns>
        ///     True if the type can be instantiated and assigned to
        ///     <see cref="IPipelineFlowStep{TPipelineItem}"/>, false if not.
        /// </returns>
        /// =================================================================================================
        internal static bool IsPipelineFlowStepImplementation<TPipelineItem>(this Type sourceType)
            where TPipelineItem : class
            => sourceType.IsNotNull()
                && sourceType.IsClass
                && sourceType.IsAbstract.IsFalse()
                && sourceType.IsGenericTypeDefinition.IsFalse()
                && typeof(IPipelineFlowStep<TPipelineItem>).IsAssignableFrom(sourceType);
    }
}