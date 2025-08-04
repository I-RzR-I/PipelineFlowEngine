// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-25 18:28
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 21:13
// ***********************************************************************
//  <copyright file="ServicePipelineExtensions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using PipelineFlowEngine.Abstractions;
using PipelineFlowEngine.Pipeline;
using System;
using System.Collections.Generic;

#endregion

namespace PipelineFlowEngine.ServiceDependencyInjectionExtensions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A service pipeline flow engine extensions.
    /// </summary>
    /// =================================================================================================
    public static class ServicePipelineExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An IServiceCollection extension method that registers the pipeline.
        /// </summary>
        /// <typeparam name="TPipelineItem">Type of the pipeline item.</typeparam>
        /// <typeparam name="TPersonPipelineContext">Type of the person pipeline context.</typeparam>
        /// <param name="serviceCollection">The serviceCollection to act on.</param>
        /// <param name="steps">The steps.</param>
        /// <param name="stepLifetime">(Optional) The step lifetime.</param>
        /// <returns>
        ///     An IServiceCollection.
        /// </returns>
        /// =================================================================================================
        public static IServiceCollection RegisterPipelineFlowEngine<TPipelineItem, TPersonPipelineContext>(
            this IServiceCollection serviceCollection,
            IEnumerable<Type> steps,
            ServiceLifetime stepLifetime = ServiceLifetime.Scoped)
            where TPipelineItem : class
            where TPersonPipelineContext : class, IPipelineFlowContext<TPipelineItem>
        {
            switch (stepLifetime)
            {
                case ServiceLifetime.Singleton:
                    serviceCollection.AddSingleton<PipelineFlowInvoker<TPipelineItem>>();
                    serviceCollection.AddSingleton<IPipelineFlowContext<TPipelineItem>, TPersonPipelineContext>();
                    break;
                case ServiceLifetime.Transient:
                    serviceCollection.AddTransient<PipelineFlowInvoker<TPipelineItem>>();
                    serviceCollection.AddTransient<IPipelineFlowContext<TPipelineItem>, TPersonPipelineContext>();
                    break;
                case ServiceLifetime.Scoped:
                default:
                    serviceCollection.AddScoped<PipelineFlowInvoker<TPipelineItem>>();
                    serviceCollection.AddScoped<IPipelineFlowContext<TPipelineItem>, TPersonPipelineContext>();
                    break;
            }

            serviceCollection.AddPipelineFlowEngineSteps<TPipelineItem>(steps, stepLifetime);

            return serviceCollection;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An IServiceCollection extension method that registers the pipeline.
        /// </summary>
        /// <typeparam name="TPipelineItem">Type of the pipeline item.</typeparam>
        /// <typeparam name="TPersonPipelineContext">Type of the person pipeline context.</typeparam>
        /// <param name="serviceCollection">The serviceCollection to act on.</param>
        /// <param name="stepLifetime">(Optional) The step lifetime.</param>
        /// <returns>
        ///     An IServiceCollection.
        /// </returns>
        /// =================================================================================================
        public static IServiceCollection RegisterPipelineFlowEngine<TPipelineItem, TPersonPipelineContext>(
            this IServiceCollection serviceCollection,
            ServiceLifetime stepLifetime = ServiceLifetime.Scoped)
            where TPipelineItem : class
            where TPersonPipelineContext : class, IPipelineFlowContext<TPipelineItem>
        {
            switch (stepLifetime)
            {
                case ServiceLifetime.Singleton:
                    serviceCollection.AddSingleton<PipelineFlowInvoker<TPipelineItem>>();
                    serviceCollection.AddSingleton<IPipelineFlowContext<TPipelineItem>, TPersonPipelineContext>();
                    break;
                case ServiceLifetime.Transient:
                    serviceCollection.AddTransient<PipelineFlowInvoker<TPipelineItem>>();
                    serviceCollection.AddTransient<IPipelineFlowContext<TPipelineItem>, TPersonPipelineContext>();
                    break;
                case ServiceLifetime.Scoped:
                default:
                    serviceCollection.AddScoped<PipelineFlowInvoker<TPipelineItem>>();
                    serviceCollection.AddScoped<IPipelineFlowContext<TPipelineItem>, TPersonPipelineContext>();
                    break;
            }

            return serviceCollection;
        }
    }
}