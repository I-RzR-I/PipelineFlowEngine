// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:37
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-23 23:40
// ***********************************************************************
//  <copyright file="PipelineFlowExecutor.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using DomainCommonExtensions.ArraysExtensions;
using DomainCommonExtensions.CommonExtensions;
using DomainCommonExtensions.CommonExtensions.TypeParam;
using DomainCommonExtensions.DataTypeExtensions;
using MethodScheduler.Helpers;
using MethodScheduler.Models;
using Microsoft.Extensions.Logging;
using PipelineFlowEngine.Abstractions;
using PipelineFlowEngine.Enums;
using PipelineFlowEngine.Extensions;
using PipelineFlowEngine.Helpers;
using PipelineFlowEngine.Models;
using PipelineFlowEngine.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using PipeInvokeMessage = PipelineFlowEngine.Helpers.DefaultMessagesHelper.PipelineFlowInvokerMessage;

#pragma warning disable CS0162 
// ReSharper disable HeuristicUnreachableCode

#endregion

namespace PipelineFlowEngine.Pipeline
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A pipeline flow invoker. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// =================================================================================================
    public sealed class PipelineFlowInvoker<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the pipeline flow context.
        /// </summary>
        /// =================================================================================================
        private readonly IPipelineFlowContext<T> _context;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the logger.
        /// </summary>
        /// =================================================================================================
        private readonly ILogger<PipelineFlowInvoker<T>> _logger;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the pipeline flow events.
        /// </summary>
        /// =================================================================================================
        private readonly ICollection<PipelineFlowEvent> _events;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the pipeline flow step results.
        /// </summary>
        /// =================================================================================================
        private readonly ICollection<PipelineFlowStepResult<T>> _stepResults;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the pipeline flow steps defined for force execute.
        /// </summary>
        /// =================================================================================================
        private readonly ICollection<IPipelineFlowStep<T>> _stepsForceExecute;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the pipeline flow steps defined for priority execute.
        /// </summary>
        /// =================================================================================================
        private readonly ICollection<IPipelineFlowStep<T>> _stepsPriorityExecute;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The pipeline flow steps.
        /// </summary>
        /// <value>
        ///     The steps.
        /// </value>
        /// =================================================================================================
        private ICollection<IPipelineFlowStep<T>> Steps { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipelineFlowInvoker{T}"/> class.
        /// </summary>
        /// <param name="context">The pipeline flow context.</param>
        /// <param name="logger">The pipeline flow logger.</param>
        /// <param name="steps">A variable-length parameters list containing pipeline flow steps.</param>
        /// =================================================================================================
        public PipelineFlowInvoker(
            IPipelineFlowContext<T> context,
            ILogger<PipelineFlowInvoker<T>> logger,
            IEnumerable<IPipelineFlowStep<T>> steps)
        {
            _context = context;
            _logger = logger;
            Steps = steps.IfIsNull(new List<IPipelineFlowStep<T>>()).ToList();

            _events = new List<PipelineFlowEvent>();
            _stepResults = new List<PipelineFlowStepResult<T>>();
            _stepsForceExecute = new List<IPipelineFlowStep<T>>();
            _stepsPriorityExecute = new List<IPipelineFlowStep<T>>();
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Adds a pipeline step to the pipeline flow context.
        /// </summary>
        /// <param name="step">The pipeline flow step.</param>
        /// <param name="stepExecutionStrategy">
        ///     (Optional) The step execution strategy.
        ///     Default value is <seealso cref="PipelineStepExecutionStrategyType.AddInQueue"/>
        /// </param>
        /// =================================================================================================
        public void AddPipelineStep(
            IPipelineFlowStep<T> step,
            PipelineStepExecutionStrategyType stepExecutionStrategy = PipelineStepExecutionStrategyType.AddInQueue)
        {
            switch (stepExecutionStrategy)
            {
                case PipelineStepExecutionStrategyType.ForceExecute:
                    _stepsForceExecute.Add(step);
                    break;
                case PipelineStepExecutionStrategyType.PriorityExecute:
                    _stepsPriorityExecute.Add(step);
                    break;
                case PipelineStepExecutionStrategyType.AddInQueue:
                default:
                    Steps.Add(step);
                    break;
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Adds a pipeline steps to the pipeline flow context.
        /// </summary>
        /// <param name="stepExecutionStrategy">
        ///     (Optional) The step execution strategy.     
        ///     Default value is <seealso cref="PipelineStepExecutionStrategyType.AddInQueue"/>
        /// </param>
        /// <param name="stepsType">
        ///     A variable-length parameters list containing pipeline flow steps type.
        /// </param>
        /// =================================================================================================
        public void AddPipelineSteps(
            PipelineStepExecutionStrategyType stepExecutionStrategy = PipelineStepExecutionStrategyType.AddInQueue,
            params Type[] stepsType)
        {
            if (stepsType.IsNullOrEmptyEnumerable())
                return;

            foreach (var step in stepsType)
            {
                if (step.BaseType.IsPipelineFlowStep())
                    AddPipelineStep((IPipelineFlowStep<T>)Activator.CreateInstance(step), stepExecutionStrategy);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Adds a pipeline steps to the pipeline flow context.
        /// </summary>
        /// <param name="steps">A variable-length parameters list containing pipeline flow steps.</param>
        /// =================================================================================================
        public void AddPipelineSteps(params AddPipelineStep<T>[] steps)
        {
            if (steps.IsNullOrEmptyEnumerable())
                return;

            foreach (var step in steps)
            {
                if (step.Step.GetType().BaseType.IsPipelineFlowStep())
                    AddPipelineStep(step.Step, step.StepExecutionStrategy);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Executes/Invoke the the pipeline steps and return the execution result.
        /// </summary>
        /// <param name="pipelineItem">The pipeline flow item.</param>
        /// <param name="cancellationToken">
        ///     (Optional) A token that allows processing to be cancelled.
        /// </param>
        /// <returns>
        ///     The pipeline invoke result.
        /// </returns>
        /// =================================================================================================
        public async Task<PipeLineResult<T>> InvokeAsync(
            T pipelineItem,
            CancellationToken cancellationToken = default)
        {
            //var result = PipeLineResult<T>.Instance;
            var result = new PipeLineResult<T>();
            result.SetState(PipelineStateType.Initialize);

            if (Steps.IsNullOrEmptyEnumerable()
                && _stepsForceExecute.IsNullOrEmptyEnumerable()
                && _stepsPriorityExecute.IsNullOrEmptyEnumerable())
                return LogEmptyStepData();

            result.SetState(PipelineStateType.Run);
            SetLogAndEvent(LogLevel.Information, PipeInvokeMessage.InitExecStepAndStrategy);

            if (_stepsForceExecute.IsNullOrEmptyEnumerable().IsFalse())
                Steps = _stepsForceExecute.FilterEnabledOrdered();
            else if (_stepsPriorityExecute.IsNullOrEmptyEnumerable().IsFalse())
            {
                var list = new List<IPipelineFlowStep<T>>();
                list.AddRange(_stepsPriorityExecute.FilterEnabledOrdered());

                list.AddRange(Steps.FilterEnabledOrdered());

                Steps = list;
            }
            else
                Steps = Steps.FilterEnabledOrdered();

            if (Steps.IsNullOrEmptyEnumerable())
                return LogEmptyStepData();

            try
            {
                var totalSteps = Steps.Count;
                SetLogAndEvent(LogLevel.Information,
                    PipeInvokeMessage.TotalRegisteredStepInPipeline.FormatWith(totalSteps));

                foreach (var step in Steps.WithIndex())
                {
                    SetLogAndEvent(LogLevel.Information,
                        PipeInvokeMessage.InitExecutionStepXFromY.FormatWith(step.item.ExecutionOrderIndex, step.index + 1, totalSteps));

                    if (step.item.PreExecutionValidationAsync.IsNotNull())
                    {
                        var preValidation = await DoPreExecutionValidationAsync(step.item.PreExecutionValidationAsync, pipelineItem);

                        // Log event information 
                        SetLogAndEvent(LogLevel.Information,
                            PipeInvokeMessage.PreValidationExecutionStepResult.FormatWith(preValidation));
                        if (preValidation.IsFalse())
                        {
                            return result
                                .SetStepResults(_stepResults)
                                .SetFlowEvent(_events)
                                .SetFailure()
                                .AsPipelineResult();
                        }
                    }

                    var isExecuted = true;
                    var copyTempData = pipelineItem;
                    var scheduleRetryPolicy = step.item.RetrySchedulePolicy.IfIsNull(new PipelineFlowRetryPolicy());
                    var stepRetryCount = 0;

                    do
                    {
                        var pipelineStepResult = new PipeLineStepResult<T>();
                        if (step.item.ExecutionCommand.AreEquals(PipelineExecutionCommandType.Schedule))
                        {
                            scheduleRetryPolicy.ExecutionSchedulerSettings = scheduleRetryPolicy.ExecutionSchedulerSettings.
                                IfIsNull(
                                    new SchedulerSettings()
                                    {
                                        DisableOnFailure = true,
                                        ThrowException = false,
                                        FailInterval = 0.5,
                                        SuccessInterval = 1.0
                                    });

                            //Always wait X time before execution
                            if (scheduleRetryPolicy.ThreadSleepBeforeExecution.IsTrue())
                                ForceWaitBeforeExecution(scheduleRetryPolicy);

                            MultipleScheduler.Instance.Start(
                                scheduleMethod: async () =>
                                {
                                    pipelineStepResult = await step.item
                                        .ExecuteStepAsync(pipelineItem, _context, _logger, cancellationToken)
                                        .ConfigureAwait(false);
                                },
                                settings: scheduleRetryPolicy.ExecutionSchedulerSettings,
                                stopAfterXIteration: scheduleRetryPolicy.RetryIterations.IfIsNull(1),
                                forceStopAfterFirstSuccessExecution: scheduleRetryPolicy.StopExecutionIfSuccessful);

                            if (scheduleRetryPolicy.WaitSchedulerExecution.IsTrue())
                                ForceWaitBeforeExecution(scheduleRetryPolicy);
                        }
                        else
                        {
                            pipelineStepResult = await step.item
                                .ExecuteStepAsync(pipelineItem, _context, _logger, cancellationToken)
                                .ConfigureAwait(false);
                        }

                        if (_context.IsEnabledStepResultCollector.IsTrue())
                            _stepResults.Add(new PipelineFlowStepResult<T>()
                            {
                                StepIteration = (stepRetryCount >= scheduleRetryPolicy.RetryIterations).IsTrue()
                                    ? PipelineFlowStepIterationType.RetryExecution
                                    : PipelineFlowStepIterationType.FirstExecution,
                                StepName = step.item.GetType().Name,
                                StepResult = pipelineStepResult
                            });

                        SetLogAndEvent(pipelineStepResult.IsSuccess
                                ? LogLevel.Information
                                : LogLevel.Warning,
                            PipeInvokeMessage.ExecutedStepXWithStatusIsSuccess.FormatWith(step.item.ExecutionOrderIndex, pipelineStepResult.IsSuccess));

                        if (pipelineStepResult.IsSuccess.IsFalse())
                        {
                            switch (_context.FailExecutionStrategy)
                            {
                                case PipelineStepFailExecutionStrategyType.StepMoveToNext:
                                    {
                                        isExecuted = true;

                                        // Log event information 
                                        SetLogAndEvent(LogLevel.Warning,
                                            PipeInvokeMessage.ExecStepFailedMoveToNext.FormatWith(_context.FailExecutionStrategy));

                                        continue;
                                    }
                                case PipelineStepFailExecutionStrategyType.StepRetry:
                                    {
                                        SetLogAndEvent(LogLevel.Information,
                                            PipeInvokeMessage.ExecStepFailedStepRetried.FormatWith(_context.FailExecutionStrategy,
                                                stepRetryCount, scheduleRetryPolicy.RetryIterations));

                                        if (stepRetryCount >= scheduleRetryPolicy.RetryIterations)
                                        {
                                            isExecuted = true;
                                            var message = PipeInvokeMessage.ExecStepFailedStepRetryUsed.FormatWith(_context.FailExecutionStrategy);

                                            // Log event information 
                                            SetLogAndEvent(LogLevel.Error, message);

                                            return result
                                                .SetStepResults(_stepResults)
                                                .SetMessage(message)
                                                .SetFlowEvent(_events)
                                                .SetFailure()
                                                .AsPipelineResult();
                                        }

                                        isExecuted = false;
                                        stepRetryCount++;
                                        pipelineItem = copyTempData;

                                        // Log event information 
                                        SetLogAndEvent(LogLevel.Warning,
                                                PipeInvokeMessage.ExecStepFailedStepRetry.FormatWith(_context.FailExecutionStrategy));

                                        break;
                                    }
                                case PipelineStepFailExecutionStrategyType.Undefined:
                                case PipelineStepFailExecutionStrategyType.PipelineStop:
                                default:
                                    {
                                        isExecuted = true;
                                        var message = PipeInvokeMessage.ExecStepFailedPipelineStop.FormatWith(_context.FailExecutionStrategy);

                                        // Log event information 
                                        SetLogAndEvent(LogLevel.Error, message);

                                        return result
                                            .SetStepResults(_stepResults)
                                            .SetMessage(message)
                                            .SetFlowEvent(_events)
                                            .SetFailure()
                                            .AsPipelineResult();
                                    }
                            }
                        }
                    } while (isExecuted.IsFalse());
                }

                SetLogAndEvent(LogLevel.Information, PipeInvokeMessage.ExecPipelineFinished);
                result
                    .SetStepResults(_stepResults)
                    .SetFlowEvent(_events)
                    .SetResult(pipelineItem)
                    .SetSuccess();
            }
            catch (Exception e)
            {
                SetLogAndEvent(LogLevel.Critical, e.Message, e);

                result
                    .SetStepResults(_stepResults)
                    .SetMessage(e.Message)
                    .SetFlowEvent(_events)
                    .SetFailure();
            }

            return result.AsPipelineResult();
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Logs empty step data.
        /// </summary>
        /// <returns>
        ///     A PipeLineResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        private PipeLineResult<T> LogEmptyStepData()
        {
            SetLogAndEvent(LogLevel.Error, PipeInvokeMessage.NoPipelineSteps);

            return PipeLineResult<T>
                .Failure()
                .SetFlowEvent(_events)
                .SetState(PipelineStateType.Finish)
                .SetStatus(PipelineStatusType.Fail)
                .AsPipelineResult();
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Executes the pre execution validation asynchronous operation.
        /// </summary>
        /// <param name="executePrecondition">The execute precondition.</param>
        /// <param name="currentObject">The current object.</param>
        /// <returns>
        ///     The do pre execution validation.
        /// </returns>
        /// =================================================================================================
        private async Task<bool> DoPreExecutionValidationAsync(Func<T, Task<bool>> executePrecondition, T currentObject)
        {
            try
            {
                var preValidation = await executePrecondition.Invoke(currentObject);
                if (preValidation.IsFalse())
                {
                    // Log event information 
                    SetLogAndEvent(LogLevel.Error, PipeInvokeMessage.PreValidationExecutionStep);
                }

                return preValidation;
            }
            catch (Exception e)
            {
                // Log event information 
                SetLogAndEvent(LogLevel.Critical, e.Message, e);

                return false;
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets log and event.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">(Optional) The exception.</param>
        /// =================================================================================================
        private void SetLogAndEvent(LogLevel level, string message, Exception exception = null)
        {
            _logger.IfEnabledWrite(level, message, exception);
            _events.Add(new PipelineFlowEvent(level, this.GetType().Name,
                DefaultMessagesHelper.FormatEventLog.FormatWith(this.GetType().GetFlowName(), message), exception));
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Force thread sleep execution.
        /// </summary>
        /// <param name="scheduleRetryPolicy">The schedule retry policy.</param>
        /// =================================================================================================
        private static void ForceWaitBeforeExecution(PipelineFlowRetryPolicy scheduleRetryPolicy)
            => Thread.Sleep(
                (scheduleRetryPolicy.RetryIterations
                 * scheduleRetryPolicy.ExecutionSchedulerSettings.SuccessInterval
                 * scheduleRetryPolicy.ExecutionSchedulerSettings.FailInterval).MinutesToMs()
                );
    }
}