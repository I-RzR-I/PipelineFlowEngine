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
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Pipeline;
using RzR.Scheduling.RecurringJobs;
using RzR.Scheduling.RecurringJobs.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions
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
            if (serviceCollection.All(d => d.ServiceType != typeof(IMethodScheduler)))
                serviceCollection.AddMethodScheduler();

            switch (stepLifetime)
            {
                case ServiceLifetime.Singleton:
                    throw new NotSupportedException("PipelineFlowInvoker<T> holds mutable per-invocation state and cannot be registered as a Singleton. Use ServiceLifetime.Scoped or ServiceLifetime.Transient.");
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
            if (serviceCollection.All(d => d.ServiceType != typeof(IMethodScheduler)))
                serviceCollection.AddMethodScheduler();

            switch (stepLifetime)
            {
                case ServiceLifetime.Singleton:
                    throw new NotSupportedException("PipelineFlowInvoker<T> holds mutable per-invocation state and cannot be registered as a Singleton. Use ServiceLifetime.Scoped or ServiceLifetime.Transient.");
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