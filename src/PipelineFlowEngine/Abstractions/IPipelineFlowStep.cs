// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:37
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-30 21:37
// ***********************************************************************
//  <copyright file="IPipelineFlowStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.Logging;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RzR.PipelineFlowEngine.Abstractions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Interface for pipeline flow step.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// =================================================================================================
    public interface IPipelineFlowStep<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the zero-based index of the pipeline step execution order.
        /// </summary>
        /// <value>
        ///     The pipeline step execution order index.
        /// </value>
        /// =================================================================================================
        int ExecutionOrderIndex { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets a value indicating whether this pipeline step is enabled.
        /// </summary>
        /// <value>
        ///     True if this pipeline step is enabled, false if not.
        /// </value>
        /// =================================================================================================
        bool IsEnabled { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the pipeline step state.
        /// </summary>
        /// <value>
        ///     The pipeline step state.
        /// </value>
        /// =================================================================================================
        PipelineStateType State { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the pipeline step status.
        /// </summary>
        /// <value>
        ///     The pipeline step status.
        /// </value>
        /// =================================================================================================
        PipelineStatusType Status { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the pipeline step execute precondition asynchronous.
        /// </summary>
        /// <value>
        ///     The pipeline step execute precondition.
        /// </value>
        /// =================================================================================================
        Func<T, Task<bool>> PreExecutionValidationAsync { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the pipeline step 'execution' command type.
        ///     See the <seealso cref="PipelineExecutionCommandType"/> definition.
        /// </summary>
        /// <value>
        ///     The pipeline step 'execution' command.
        /// </value>
        /// =================================================================================================
        PipelineExecutionCommandType ExecutionCommand { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the pipeline step retry schedule policy.
        /// </summary>
        /// <value>
        ///     The pipeline step retry schedule policy.
        /// </value>
        /// =================================================================================================
        PipelineFlowRetryPolicy RetrySchedulePolicy { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Executes the pipeline step asynchronous operation.
        /// </summary>
        /// <param name="pipelineStep">The pipeline step.</param>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cancellationToken">
        ///     (Optional) A token that allows processing to be cancelled.
        /// </param>
        /// <returns>
        ///     The pipeline execute step.
        /// </returns>
        /// =================================================================================================
        Task<PipeLineStepResult<T>> ExecuteStepAsync(
            T pipelineStep,
            IPipelineFlowContext<T> context,
            ILogger<PipelineFlowInvoker<T>> logger,
            CancellationToken cancellationToken = default);
    }
}