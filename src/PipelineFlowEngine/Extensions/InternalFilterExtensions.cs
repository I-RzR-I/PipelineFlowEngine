// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-07-09 15:50
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-09 15:56
// ***********************************************************************
//  <copyright file="InternalFilterExtensions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using RzR.Extensions.Domain.Primitives;
using RzR.PipelineFlowEngine.Abstractions;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace RzR.PipelineFlowEngine.Extensions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     An internal filter extensions.
    /// </summary>
    /// =================================================================================================
    internal static class InternalFilterExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An IEnumerable&lt;IPipelineFlowStep&lt;T&gt;&gt; extension method that filter collection by `IsEnabled`
        ///     and order by `ExecutionOrderIndex`.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="steps">The steps to act on.</param>
        /// <returns>
        ///     A list of.
        /// </returns>
        /// =================================================================================================
        internal static ICollection<IPipelineFlowStep<T>> FilterEnabledOrdered<T>(
            this IEnumerable<IPipelineFlowStep<T>> steps)
            where T : class
            => steps.Where(x => x.IsEnabled.IsTrue())
                .OrderBy(x => x.ExecutionOrderIndex)
                .ToList();
    }
}