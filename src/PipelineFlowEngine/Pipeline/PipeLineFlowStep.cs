// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:37
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-23 23:40
// ***********************************************************************
//  <copyright file="PipeLineFlowStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineFlowEngine.Abstractions;
using PipelineFlowEngine.Enums;
using PipelineFlowEngine.Models;
using PipelineFlowEngine.Models.Result;
using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

#endregion

namespace PipelineFlowEngine.Pipeline
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A pipe line flow step.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <seealso cref="T:PipelineFlowEngine.Abstractions.IPipelineFlowStep{T}"/>
    /// =================================================================================================
    public abstract class PipeLineFlowStep<T> : IPipelineFlowStep<T> where T : class
    {
        /// <inheritdoc/>
        public virtual bool IsEnabled { get; protected set; } = false;

        /// <inheritdoc/>
        public virtual int ExecutionOrderIndex { get; protected set; } = 0;

        /// <inheritdoc/>
        public virtual PipelineStatusType Status { get; protected set; } 
            = PipelineStatusType.Undefined;

        /// <inheritdoc/>
        public virtual PipelineStateType State { get; protected set; } 
            = PipelineStateType.Undefined;

        /// <inheritdoc />
        public virtual Func<T, Task<bool>> PreExecutionValidationAsync { get; protected set; }

        /// <inheritdoc />
        public virtual PipelineExecutionCommandType ExecutionCommand { get; protected set; }
            = PipelineExecutionCommandType.Simple;

        /// <inheritdoc />
        public virtual PipelineFlowRetryPolicy RetrySchedulePolicy { get; protected set; } 
            = new PipelineFlowRetryPolicy();

        /// <inheritdoc/>
        public abstract Task<PipeLineStepResult<T>> ExecuteStepAsync(
            T pipelineStep,
            IPipelineFlowContext<T> context,
            ILogger<PipelineFlowInvoker<T>> logger,
            CancellationToken cancellationToken = default);
    }
}