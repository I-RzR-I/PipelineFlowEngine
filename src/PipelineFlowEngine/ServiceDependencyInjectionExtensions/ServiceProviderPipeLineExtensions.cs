// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-07-09 18:03
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-09 18:04
// ***********************************************************************
//  <copyright file="ServiceProviderPipeLineExtensions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using PipelineFlowEngine.Pipeline;
using System;

#endregion

namespace PipelineFlowEngine.ServiceDependencyInjectionExtensions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A service provider pipe line extensions.
    /// </summary>
    /// =================================================================================================
    public static class ServiceProviderPipeLineExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An IServiceProvider extension method that gets pipeline invoker.
        /// </summary>
        /// <typeparam name="TPipelineItem">Type of the pipeline item.</typeparam>
        /// <param name="serviceProvider">The serviceProvider to act on.</param>
        /// <returns>
        ///     The pipeline invoker.
        /// </returns>
        /// =================================================================================================
        public static PipelineFlowInvoker<TPipelineItem> GetPipelineFlowEngineInvoker<TPipelineItem>(
            this IServiceProvider serviceProvider)
            where TPipelineItem : class
            => serviceProvider.GetRequiredService<PipelineFlowInvoker<TPipelineItem>>();
    }
}