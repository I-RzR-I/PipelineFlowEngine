// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-25 16:28
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-30 21:22
// ***********************************************************************
//  <copyright file="ServicePipelineStepExtensions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using PipelineFlowEngine.Abstractions;
using PipelineFlowEngine.Extensions;
using PipelineFlowEngine.Pipeline;
using System;
using System.Collections.Generic;

#endregion

namespace PipelineFlowEngine.ServiceDependencyInjectionExtensions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A service pipeline step extensions.
    /// </summary>
    /// =================================================================================================
    public static class ServicePipelineStepExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An IServiceCollection extension method that adds a step to 'stepLifetime'.
        /// </summary>
        /// <typeparam name="TPipelineItem">Type of the pipeline item.</typeparam>
        /// <typeparam name="TPipelineStepImplementation">
        ///     Type of the pipeline step implementation.
        /// </typeparam>
        /// <param name="serviceCollection">The serviceCollection to act on.</param>
        /// <param name="stepLifetime">(Optional) The step lifetime.</param>
        /// <returns>
        ///     An IServiceCollection.
        /// </returns>
        /// =================================================================================================
        public static IServiceCollection AddPipelineFlowEngineStep<TPipelineItem, TPipelineStepImplementation>(
            this IServiceCollection serviceCollection,
            ServiceLifetime stepLifetime = ServiceLifetime.Scoped)
            where TPipelineItem : class
            where TPipelineStepImplementation : PipeLineFlowStep<TPipelineItem>
        {
            serviceCollection.Add(
                ServiceDescriptor.Describe(typeof(IPipelineFlowStep<TPipelineItem>),
                    typeof(TPipelineStepImplementation),
                    stepLifetime));

            return serviceCollection;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An IServiceCollection extension method that adds the steps.
        /// </summary>
        /// <typeparam name="TPipelineItem">Type of the pipeline item.</typeparam>
        /// <param name="serviceCollection">The serviceCollection to act on.</param>
        /// <param name="step">The pipeline step.</param>
        /// <param name="stepLifetime">(Optional) The step lifetime.</param>
        /// <returns>
        ///     An IServiceCollection.
        /// </returns>
        /// =================================================================================================
        public static IServiceCollection AddPipelineFlowEngineStep<TPipelineItem>(
            this IServiceCollection serviceCollection,
            Type step,
            ServiceLifetime stepLifetime = ServiceLifetime.Scoped)
            where TPipelineItem : class
        {
            if (step.BaseType.IsPipelineFlowStep<TPipelineItem>())
            {
                serviceCollection.Add(ServiceDescriptor.Describe(
                    typeof(IPipelineFlowStep<TPipelineItem>),
                    step,
                    stepLifetime));
            }

            return serviceCollection;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An IServiceCollection extension method that adds the steps.
        /// </summary>
        /// <typeparam name="TPipelineItem">Type of the pipeline item.</typeparam>
        /// <param name="serviceCollection">The serviceCollection to act on.</param>
        /// <param name="steps">The pipeline steps.</param>
        /// <param name="stepLifetime">(Optional) The step lifetime.</param>
        /// <returns>
        ///     An IServiceCollection.
        /// </returns>
        /// =================================================================================================
        public static IServiceCollection AddPipelineFlowEngineSteps<TPipelineItem>(
            this IServiceCollection serviceCollection,
            IEnumerable<Type> steps,
            ServiceLifetime stepLifetime = ServiceLifetime.Scoped)
            where TPipelineItem : class
        {
            foreach (var step in steps)
            {
                if (step.BaseType.IsPipelineFlowStep<TPipelineItem>())
                {
                    serviceCollection.Add(ServiceDescriptor.Describe(
                        typeof(IPipelineFlowStep<TPipelineItem>),
                        step,
                        stepLifetime));
                }
            }

            return serviceCollection;
        }
    }
}