// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineFlowEngine
//  Author            : RzR
//  Created           : 20-07-2026 22:07
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-27 12:13
//  ***********************************************************************
//  <copyright file="ServicePipelineDispatchObserverExtensions.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using Microsoft.Extensions.DependencyInjection;
using RzR.PipelineFlowEngine.Abstractions;

#endregion

namespace RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A service pipeline dispatch observer extensions.
    /// </summary>
    /// =================================================================================================
    public static class ServicePipelineDispatchObserverExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An IServiceCollection extension method that registers the observer notified when a fire-
        ///     and-forget dispatched pipeline step reaches a terminal state.
        /// </summary>
        /// <typeparam name="TPipelineItem">Type of the pipeline item.</typeparam>
        /// <typeparam name="TObserver">Type of the dispatch observer implementation.</typeparam>
        /// <param name="serviceCollection">The serviceCollection to act on.</param>
        /// <returns>
        ///     An IServiceCollection.
        /// </returns>
        /// =================================================================================================
        public static IServiceCollection AddPipelineFlowDispatchObserver<TPipelineItem, TObserver>(
            this IServiceCollection serviceCollection)
            where TPipelineItem : class
            where TObserver : class, IPipelineDispatchObserver<TPipelineItem>
        {
            serviceCollection.AddSingleton<IPipelineDispatchObserver<TPipelineItem>, TObserver>();

            return serviceCollection;
        }
    }
}