// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:37
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-27 12:13
// ***********************************************************************
//  <copyright file="PipelineFlowExecutor.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RzR.Extensions.Domain.Collections;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Reflection.TypeParam;
using RzR.Extensions.Domain.Text;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
using RzR.PipelineFlowEngine.Helpers;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.Scheduling.RecurringJobs.Abstractions;
using RzR.Scheduling.RecurringJobs.Helpers;
using RzR.Scheduling.RecurringJobs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipeInvokeMessage = RzR.PipelineFlowEngine.Helpers.DefaultMessagesHelper.PipelineFlowInvokerMessage;

#endregion

namespace RzR.PipelineFlowEngine.Pipeline
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
        ///     (Immutable) the method scheduler used to run scheduled pipeline steps.
        /// </summary>
        /// =================================================================================================
        private readonly IMethodScheduler _methodScheduler;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the pipeline flow steps registered for queued execution.
        /// </summary>
        /// =================================================================================================
        private readonly ICollection<IPipelineFlowStep<T>> _stepsQueued;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the service scope factory used to run fire-and-forget scheduled steps in
        ///     their own scope. Optional; when null a dispatched step keeps running against the scope
        ///     that resolved it.
        /// </summary>
        /// =================================================================================================
        private readonly IServiceScopeFactory _scopeFactory;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the observer notified when a fire-and-forget dispatched step reaches a
        ///     terminal state. Optional; when null no dispatch outcome is reported.
        /// </summary>
        /// =================================================================================================
        private readonly IPipelineDispatchObserver<T> _dispatchObserver;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The reentrancy guard flag of the current invoker. A value of <c>1</c> means an
        ///     invocation is already running on this instance.
        /// </summary>
        /// =================================================================================================
        private int _invocationInProgress;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipelineFlowInvoker{T}"/> class.
        /// </summary>
        /// <param name="context">The pipeline flow context.</param>
        /// <param name="logger">The pipeline flow logger.</param>
        /// <param name="steps">A variable-length parameters list containing pipeline flow steps.</param>
        /// <param name="methodScheduler">
        ///     (Optional) The <see cref="IMethodScheduler"/> used for scheduled pipeline steps. When
        ///     null, falls back to <see cref="MethodSchedulerService.Default"/>.
        /// </param>
        /// <param name="scopeFactory">
        ///     (Optional) The <see cref="IServiceScopeFactory"/> used to run fire-and-forget scheduled
        ///     steps (<c>WaitSchedulerExecution</c> false) in their own service scope, so they no longer
        ///     outlive the scope that resolved the invoker. When null those steps keep running against
        ///     the resolving scope, which is the previous behaviour.
        /// </param>
        /// <param name="dispatchObserver">
        ///     (Optional) The <see cref="IPipelineDispatchObserver{T}"/> notified once a fire-and-forget
        ///     dispatched step (<c>WaitSchedulerExecution</c> false) reaches a terminal state. It must
        ///     be a singleton, see the contract documented on the interface. When null no dispatch
        ///     outcome is reported and the failure of a dispatched step stays visible through the logger
        ///     only.
        /// </param>
        /// =================================================================================================
        public PipelineFlowInvoker(
            IPipelineFlowContext<T> context,
            ILogger<PipelineFlowInvoker<T>> logger,
            IEnumerable<IPipelineFlowStep<T>> steps,
            IMethodScheduler methodScheduler = null,
            IServiceScopeFactory scopeFactory = null,
            IPipelineDispatchObserver<T> dispatchObserver = null)
        {
            _context = context;
            _logger = logger;
            _stepsQueued = steps.IfIsNull(new List<IPipelineFlowStep<T>>()).ToList();
            _methodScheduler = methodScheduler ?? MethodSchedulerService.Default;
            _scopeFactory = scopeFactory;
            _dispatchObserver = dispatchObserver;

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
        ///     (Optional) The step execution strategy. Default value is <seealso cref="PipelineStepExecutionStrategyType.AddInQueue"/>
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
                    _stepsQueued.Add(step);
                    break;
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Adds a pipeline steps to the pipeline flow context.
        /// </summary>
        /// <remarks>
        ///     This overload instantiates each step via <see cref="Activator.CreateInstance(Type)"/> and
        ///     therefore only supports steps with a parameterless constructor. Steps that require
        ///     constructor-injected dependencies must be registered through the DI extensions (<c>
        ///     AddPipelineFlowEngineStep</c> / <c>AddPipelineFlowEngineSteps</c>) instead.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///     Thrown when one of the supplied types is not an instantiable implementation of
        ///     <see cref="IPipelineFlowStep{T}"/>.
        /// </exception>
        /// <param name="stepExecutionStrategy">
        ///     (Optional) The step execution strategy. Default value is <seealso cref="PipelineStepExecutionStrategyType.AddInQueue"/>
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
                if (step.IsPipelineFlowStepImplementation<T>().IsFalse())
                {
                    throw new ArgumentException(
                        DefaultMessagesHelper.PipelineFlowStepRegistrationMessage.TypeIsNotPipelineStep.FormatWith(
                            step?.FullName, typeof(T).FullName),
                        nameof(stepsType));
                }

                AddPipelineStep((IPipelineFlowStep<T>)Activator.CreateInstance(step), stepExecutionStrategy);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Adds a pipeline steps to the pipeline flow context.
        /// </summary>
        /// <param name="steps">A variable-length parameters list containing pipeline flow steps.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when one of the supplied entries is null or does not supply a step instance. Such an
        ///     entry is rejected instead of being skipped, because a silently dropped step would let the
        ///     pipeline report success for work that never ran.
        /// </exception>
        /// =================================================================================================
        public void AddPipelineSteps(params AddPipelineStep<T>[] steps)
        {
            if (steps.IsNullOrEmptyEnumerable())
                return;

            for (var index = 0; index < steps.Length; index++)
            {
                var step = steps[index];
                if (step.IsNull() || step.Step.IsNull())
                {
                    throw new ArgumentException(
                        DefaultMessagesHelper.PipelineFlowStepRegistrationMessage.StepAtIndexIsNull.FormatWith(index),
                        nameof(steps));
                }

                AddPipelineStep(step.Step, step.StepExecutionStrategy);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Executes/Invoke the pipeline steps and return the execution result.
        /// </summary>
        /// <remarks>
        ///     The per-invocation state (collected flow events and step results) is reset on entry, so
        ///     every invocation observes only its own events.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the same invoker instance is already executing an invocation; concurrent
        ///     invocation is not supported, resolve one invoker per invocation.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     Thrown when <paramref name="cancellationToken"/> is cancelled; the cancellation
        ///     propagates to the caller instead of being converted into a failed pipeline result.
        /// </exception>
        /// <param name="pipelineItem">The pipeline flow item.</param>
        /// <param name="cancellationToken">
        ///     (Optional) A token that allows processing to be cancelled.
        /// </param>
        /// <returns>
        ///     The pipeline invoke result.
        /// </returns>
        /// =================================================================================================
        public async Task<PipeLineResult<T>> InvokeAsync(T pipelineItem,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _invocationInProgress, 1, 0) != 0)
                throw new InvalidOperationException(PipeInvokeMessage.ConcurrentInvocationNotSupported);

            try
            {
                _events.Clear();
                _stepResults.Clear();

                return await InvokePipelineAsync(pipelineItem, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Exchange(ref _invocationInProgress, 0);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Executes the pipeline steps once the reentrancy guard was acquired and the per-invocation
        ///     state was reset.
        /// </summary>
        /// <exception cref="OperationCanceledException">
        ///     Thrown when a thread cancels a running operation.
        /// </exception>
        /// <param name="pipelineItem">The pipeline flow item.</param>
        /// <param name="cancellationToken">A token that allows processing to be cancelled.</param>
        /// <returns>
        ///     The pipeline invoke result.
        /// </returns>
        /// =================================================================================================
        private async Task<PipeLineResult<T>> InvokePipelineAsync(T pipelineItem,
            CancellationToken cancellationToken)
        {
            //var result = PipeLineResult<T>.Instance;
            var result = new PipeLineResult<T>();
            result.SetState(PipelineStateType.Initialize);

            if (_stepsQueued.IsNullOrEmptyEnumerable()
                && _stepsForceExecute.IsNullOrEmptyEnumerable()
                && _stepsPriorityExecute.IsNullOrEmptyEnumerable())
                return LogEmptyStepData(result);

            result.SetState(PipelineStateType.Run);
            SetLogAndEvent(LogLevel.Information, PipeInvokeMessage.InitExecStepAndStrategy);

            var executionSteps = BuildExecutionSteps();

            if (executionSteps.IsNullOrEmptyEnumerable())
                return LogEmptyStepData(result);

            try
            {
                var totalSteps = executionSteps.Count;
                SetLogAndEvent(LogLevel.Information,
                    PipeInvokeMessage.TotalRegisteredStepInPipeline.FormatWith(totalSteps));

                foreach (var step in executionSteps.WithIndex())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    SetLogAndEvent(LogLevel.Information,
                        PipeInvokeMessage.InitExecutionStepXFromY.FormatWith(step.item.ExecutionOrderIndex, step.index + 1, totalSteps));

                    var precondition = step.item.PreExecutionValidationAsync;
                    if (precondition.IsNotNull())
                    {
                        var preValidation = await DoPreExecutionValidationAsync(precondition, pipelineItem, cancellationToken).ConfigureAwait(false);

                        // Log event information
                        SetLogAndEvent(LogLevel.Information,
                            PipeInvokeMessage.PreValidationExecutionStepResult.FormatWith(preValidation.IsValid));

                        if (preValidation.IsValid.IsFalse())
                        {
                            var canSkipStep = preValidation.CanSkipStep
                                && step.item.PreValidationFailStrategy.AreEquals(PipelineStepPreValidationFailStrategyType.StepSkip);

                            if (canSkipStep)
                            {
                                var skippedStepName = step.item.GetType().Name;
                                var skipMessage = PipeInvokeMessage.PreValidationExecutionStepSkipped
                                    .FormatWith(skippedStepName, step.item.PreValidationFailStrategy);

                                SetLogAndEvent(LogLevel.Warning, skipMessage);

                                if (_context.IsEnabledStepResultCollector.IsTrue())
                                {
                                    var skippedStepResult = new PipeLineStepResult<T>();
                                    skippedStepResult.SetState(PipelineStateType.Skip);
                                    skippedStepResult.SetMessage(skipMessage);

                                    _stepResults.Add(new PipelineFlowStepResult<T>
                                    {
                                        StepIteration = PipelineFlowStepIterationType.SkippedByPreValidation,
                                        StepName = skippedStepName,
                                        StepResult = skippedStepResult
                                    });
                                }

                                continue;
                            }

                            if (preValidation.CanSkipStep.IsTrue())
                                SetLogAndEvent(LogLevel.Error, preValidation.StopMessage);

                            return FailPipeline(result, preValidation.StopMessage);
                        }
                    }

                    bool isExecuted;
                    var scheduleRetryPolicy = step.item.RetrySchedulePolicy.IfIsNull(new PipelineFlowRetryPolicy());
                    var stepRetryCount = 0;

                    do
                    {
                        isExecuted = true;

                        cancellationToken.ThrowIfCancellationRequested();

                        var pipelineStepResult = new PipeLineStepResult<T>();
                        string stepExceptionMessage = null;

                        if (step.item.ExecutionCommand.AreEquals(PipelineExecutionCommandType.Schedule))
                        {
                            var options = BuildScheduledJobOptions(scheduleRetryPolicy);

                            var executionState = new ScheduledExecutionState();
                            var waitSchedulerExecution = scheduleRetryPolicy.WaitSchedulerExecution.IsTrue();

                            Func<CancellationToken, Task> scheduledWork;
                            if (waitSchedulerExecution)
                            {
                                scheduledWork = async token =>
                                {
                                    executionState.BeginIteration();

                                    pipelineStepResult = await ExecuteScheduledBodyAsync(
                                            executionState, step.item, pipelineItem, _context, token)
                                        .ConfigureAwait(false);
                                };
                            }
                            else
                            {
                                scheduledWork = async token =>
                                {
                                    executionState.BeginIteration();

                                    if (_scopeFactory.IsNull())
                                    {
                                        await ExecuteScheduledBodyAsync(
                                                executionState, step.item, pipelineItem, _context, token)
                                            .ConfigureAwait(false);

                                        return;
                                    }

                                    var dispatchScope = ResolveDispatchScope(executionState, step.item);

                                    using (dispatchScope.Scope)
                                    {
                                        await ExecuteScheduledBodyAsync(
                                                executionState, dispatchScope.Step, pipelineItem,
                                                dispatchScope.Context, token)
                                            .ConfigureAwait(false);
                                    }
                                };
                            }

                            var job = _methodScheduler.Schedule(options, scheduledWork);

                            if (waitSchedulerExecution)
                            {
                                using (RegisterStopOnCancellation(job, step.item.GetType().Name, cancellationToken))
                                {
                                    try
                                    {
                                        await job.Completion.ConfigureAwait(false);
                                    }
                                    catch (OperationCanceledException e) when (e.IsPipelineCancellation(cancellationToken))
                                    {
                                        throw;
                                    }
                                    catch (Exception stepException)
                                    {
                                        stepExceptionMessage = stepException.Message;
                                        pipelineStepResult = BuildFailedStepResult(step.item, stepException);
                                    }
                                }

                                cancellationToken.ThrowIfCancellationRequested();

                                if (stepExceptionMessage.IsMissing())
                                {
                                    if (executionState.LastError.IsNotNull())
                                    {
                                        stepExceptionMessage = executionState.LastError.Message;
                                        pipelineStepResult = BuildFailedStepResult(step.item, executionState.LastError);
                                    }
                                    else if (executionState.Completed.IsFalse())
                                    {
                                        var noOutcomeMessage = PipeInvokeMessage.ScheduledStepXNoObservedOutcome
                                            .FormatWith(step.item.GetType().Name);

                                        SetLogAndEvent(LogLevel.Warning, noOutcomeMessage);

                                        stepExceptionMessage = noOutcomeMessage;

                                        pipelineStepResult = new PipeLineStepResult<T>();
                                        pipelineStepResult.SetFailure(noOutcomeMessage);
                                    }
                                }
                            }
                            else
                            {
                                DispatchScheduledStep(step.item, job, cancellationToken, executionState);

                                continue;
                            }
                        }
                        else
                        {
                            try
                            {
                                pipelineStepResult = await step.item
                                    .ExecuteStepAsync(pipelineItem, _context, _logger, cancellationToken)
                                    .ConfigureAwait(false);
                            }
                            catch (OperationCanceledException e) when (e.IsPipelineCancellation(cancellationToken))
                            {
                                throw;
                            }
                            catch (Exception stepException)
                            {
                                stepExceptionMessage = stepException.Message;
                                pipelineStepResult = BuildFailedStepResult(step.item, stepException);
                            }
                        }

                        if (_context.IsEnabledStepResultCollector.IsTrue())
                        {
                            _stepResults.Add(new PipelineFlowStepResult<T>()
                            {
                                StepIteration = (stepRetryCount > 0).IsTrue()
                                    ? PipelineFlowStepIterationType.RetryExecution
                                    : PipelineFlowStepIterationType.FirstExecution,
                                StepName = step.item.GetType().Name,
                                StepResult = pipelineStepResult
                            });
                        }

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
                                        // Log event information
                                        SetLogAndEvent(LogLevel.Warning,
                                            PipeInvokeMessage.ExecStepFailedMoveToNext.FormatWith(_context.FailExecutionStrategy));

                                        continue;
                                    }
                                case PipelineStepFailExecutionStrategyType.StepRetry:
                                    {
                                        if (step.item.ExecutionCommand.AreEquals(PipelineExecutionCommandType.Schedule))
                                        {
                                            var scheduledMessage = string.IsNullOrEmpty(stepExceptionMessage)
                                                ? PipeInvokeMessage.ExecStepFailedPipelineStop.FormatWith(_context.FailExecutionStrategy)
                                                : PipeInvokeMessage.ExecStepFailedPipelineStopWithError.FormatWith(_context.FailExecutionStrategy, stepExceptionMessage);

                                            SetLogAndEvent(LogLevel.Error, scheduledMessage);

                                            return FailPipeline(result, scheduledMessage);
                                        }

                                        SetLogAndEvent(LogLevel.Information,
                                            PipeInvokeMessage.ExecStepFailedStepRetried.FormatWith(_context.FailExecutionStrategy,
                                                stepRetryCount, scheduleRetryPolicy.RetryIterations));

                                        if (stepRetryCount >= scheduleRetryPolicy.RetryIterations)
                                        {
                                            var message = PipeInvokeMessage.ExecStepFailedStepRetryUsed.FormatWith(_context.FailExecutionStrategy);

                                            // Log event information
                                            SetLogAndEvent(LogLevel.Error, message);

                                            return FailPipeline(result, message);
                                        }

                                        isExecuted = false;
                                        stepRetryCount++;

                                        // Log event information
                                        SetLogAndEvent(LogLevel.Warning,
                                                PipeInvokeMessage.ExecStepFailedStepRetry.FormatWith(_context.FailExecutionStrategy));

                                        break;
                                    }
                                case PipelineStepFailExecutionStrategyType.Undefined:
                                case PipelineStepFailExecutionStrategyType.PipelineStop:
                                default:
                                    {
                                        var message = string.IsNullOrEmpty(stepExceptionMessage)
                                            ? PipeInvokeMessage.ExecStepFailedPipelineStop.FormatWith(_context.FailExecutionStrategy)
                                            : PipeInvokeMessage.ExecStepFailedPipelineStopWithError.FormatWith(_context.FailExecutionStrategy, stepExceptionMessage);

                                        // Log event information
                                        SetLogAndEvent(LogLevel.Error, message);

                                        return FailPipeline(result, message);
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
            catch (OperationCanceledException e) when (e.IsPipelineCancellation(cancellationToken))
            {
                throw;
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
        ///     Builds the ordered collection of steps to execute, applying the registered execution
        ///     strategies.
        /// </summary>
        /// <remarks>
        ///     A non-empty force-execute bucket REPLACES the whole execution set. A non-empty 
        ///     priority-execute bucket is PREPENDED to the queued bucket, each bucket ordered on its own and
        ///     never globally, so a queued step never interleaves ahead of a priority one.
        /// </remarks>
        /// <returns>
        ///     The ordered pipeline flow steps to execute.
        /// </returns>
        /// =================================================================================================
        private ICollection<IPipelineFlowStep<T>> BuildExecutionSteps()
        {
            if (_stepsForceExecute.IsNullOrEmptyEnumerable().IsFalse())
                return _stepsForceExecute.FilterEnabledOrdered();

            if (_stepsPriorityExecute.IsNullOrEmptyEnumerable().IsFalse())
            {
                var list = new List<IPipelineFlowStep<T>>();
                list.AddRange(_stepsPriorityExecute.FilterEnabledOrdered());

                list.AddRange(_stepsQueued.FilterEnabledOrdered());

                return list;
            }

            return _stepsQueued.FilterEnabledOrdered();
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Maps a pipeline retry policy onto the scheduler options used to schedule a step.
        /// </summary>
        /// <param name="scheduleRetryPolicy">The retry/schedule policy of the current step.</param>
        /// <returns>
        ///     The scheduled job options describing how the step must be scheduled.
        /// </returns>
        /// =================================================================================================
        private static ScheduledJobOptions BuildScheduledJobOptions(PipelineFlowRetryPolicy scheduleRetryPolicy)
        {
            var settings = scheduleRetryPolicy.ExecutionSchedulerSettings.IfIsNull(new ScheduledJobOptions());

            var options = new ScheduledJobOptions
            {
                SuccessInterval = settings.SuccessInterval,
                FailInterval = settings.FailInterval,
                StopOnFailure = settings.StopOnFailure,
                ThrowOnFailure = settings.ThrowOnFailure,
                InitialDelay = settings.InitialDelay,
                MaxIterations = scheduleRetryPolicy.RetryIterations,
                StopOnFirstSuccess = scheduleRetryPolicy.StopExecutionIfSuccessful
            };

            if (scheduleRetryPolicy.ThreadSleepBeforeExecution.IsTrue()
                && options.InitialDelay.IsNull()
                && options.SuccessInterval > TimeSpan.Zero)
                options.InitialDelay = options.SuccessInterval;

            return options;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Completes the invocation result as a terminal pipeline failure.
        /// </summary>
        /// <remarks>
        ///     The fluent order is load-bearing: <c>SetMessage</c> must stay ahead of <c>SetFailure</c>,
        ///     because both recompute the status of the result.
        /// </remarks>
        /// <param name="result">The result of the current invocation.</param>
        /// <param name="message">
        ///     (Optional) The failure message. When null or empty no message is applied, leaving the
        ///     message already carried by <paramref name="result"/> untouched.
        /// </param>
        /// <returns>
        ///     The failed pipeline invoke result.
        /// </returns>
        /// =================================================================================================
        private PipeLineResult<T> FailPipeline(PipeLineResult<T> result, string message = null)
        {
            result.SetStepResults(_stepResults);

            if (message.IsPresent())
                result.SetMessage(message);

            return result
                .SetFlowEvent(_events)
                .SetFailure()
                .AsPipelineResult();
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Resolves the equivalent of an already registered step from a freshly created service scope.
        /// </summary>
        /// <remarks>
        ///     Steps are registered with <see cref="IPipelineFlowStep{T}"/> as the service type and the
        ///     concrete class as the implementation, so they cannot be resolved by their concrete type; the
        ///     registrations of the scope are enumerated and matched on the runtime type instead. A step
        ///     supplied as a constructed instance was never in the container and therefore never matches; it
        ///     is returned unchanged, keeping the dependencies it was constructed with.
        /// </remarks>
        /// <param name="scope">The service scope created for the dispatched execution.</param>
        /// <param name="registeredStep">The step instance held by this invoker.</param>
        /// <returns>
        ///     The step resolved from <paramref name="scope"/>, or <paramref name="registeredStep"/> when the
        ///     scope has no registration for it.
        /// </returns>
        /// =================================================================================================
        private IPipelineFlowStep<T> ResolveScopedStep(IServiceScope scope, IPipelineFlowStep<T> registeredStep)
        {
            var stepType = registeredStep.GetType();

            foreach (var scopedStep in scope.ServiceProvider.GetServices<IPipelineFlowStep<T>>())
            {
                if (scopedStep.GetType() == stepType)
                    return scopedStep;
            }

            _logger.IfEnabledWrite(LogLevel.Warning,
                PipelineFlowEventIds.ScheduledStepNotResolvable,
                PipeInvokeMessage.ScheduledStepXNotResolvableInScope.FormatWith(stepType.Name));

            return registeredStep;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Handles a scheduled step that was dispatched without waiting for its execution: keeps the
        ///     cancellation registration alive for the job lifetime only, logs a faulted dispatch, and
        ///     records the dispatch event and step result.
        /// </summary>
        /// <param name="step">The dispatched pipeline step.</param>
        /// <param name="job">The scheduled job running the step.</param>
        /// <param name="cancellationToken">A token that allows processing to be cancelled.</param>
        /// <param name="executionState">
        ///     The state written by the work delegate of the dispatched execution. Read only once the job
        ///     completed, to tell a job that finished without ever running the step apart from one that
        ///     ran it, and from one whose body threw while the scheduler absorbed the failure.
        /// </param>
        /// =================================================================================================
        private void DispatchScheduledStep(IPipelineFlowStep<T> step, IScheduledJob job,
            CancellationToken cancellationToken, ScheduledExecutionState executionState)
        {
            var dispatchedStepName = step.GetType().Name;

            var stopRegistration = RegisterStopOnCancellation(job, dispatchedStepName, cancellationToken);

            _ = job.Completion.ContinueWith(
                t =>
                {
                    stopRegistration.Dispose();

                    var dispatch = ClassifyDispatch(t, executionState, cancellationToken);

                    if (dispatch.Status.AreEquals(PipelineDispatchStatusType.Faulted))
                    {
                        try
                        {
                            _logger.IfEnabledWrite(LogLevel.Error,
                                PipelineFlowEventIds.ScheduledStepDispatchFaulted,
                                PipeInvokeMessage.ScheduledStepXDispatchFaulted.FormatWith(dispatchedStepName),
                                dispatch.Error);
                        }
                        catch (Exception loggingException)
                        {
                            _ = loggingException;
                        }
                    }

                    RaiseDispatchCompleted(dispatchedStepName, dispatch);
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            SetLogAndEvent(LogLevel.Information, PipelineFlowEventIds.ScheduledStepDispatched,
                PipeInvokeMessage.ScheduledStepXDispatched.FormatWith(dispatchedStepName));

            if (_scopeFactory.IsNull())
            {
                SetLogAndEvent(LogLevel.Warning, PipelineFlowEventIds.ScheduledStepDispatchedWithoutScope,
                    PipeInvokeMessage.ScheduledStepXDispatchedWithoutScope.FormatWith(dispatchedStepName));
            }

            if (_context.IsEnabledStepResultCollector.IsTrue())
            {
                var dispatchedStepResult = new PipeLineStepResult<T>();
                dispatchedStepResult.SetState(PipelineStateType.Run);
                dispatchedStepResult.SetMessage(
                    PipeInvokeMessage.ScheduledStepXDispatchNotObserved.FormatWith(dispatchedStepName));

                _stepResults.Add(new PipelineFlowStepResult<T>
                {
                    StepIteration = PipelineFlowStepIterationType.Dispatched,
                    StepName = dispatchedStepName,
                    StepResult = dispatchedStepResult
                });
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Runs one iteration of a scheduled step body and records on the execution state what that
        ///     iteration did.
        /// </summary>
        /// <exception cref="Exception">Rethrows whatever the step body raised.</exception>
        /// <param name="executionState">The state of the current scheduled execution.</param>
        /// <param name="step">The step whose body is executed.</param>
        /// <param name="pipelineItem">The pipeline flow item.</param>
        /// <param name="context">The flow context handed to the step body.</param>
        /// <param name="cancellationToken">The token supplied by the scheduler for this iteration.</param>
        /// <returns>
        ///     The step result produced by the body.
        /// </returns>
        /// =================================================================================================
        private async Task<PipeLineStepResult<T>> ExecuteScheduledBodyAsync(
            ScheduledExecutionState executionState,
            IPipelineFlowStep<T> step,
            T pipelineItem,
            IPipelineFlowContext<T> context,
            CancellationToken cancellationToken)
        {
            try
            {
                var stepResult = await step
                    .ExecuteStepAsync(pipelineItem, context, _logger, cancellationToken)
                    .ConfigureAwait(false);

                executionState.Completed = true;

                return stepResult;
            }
            catch (Exception stepBodyException)
            {
                executionState.LastError = stepBodyException;

                throw;
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Creates the service scope of a dispatched execution and resolves the step and flow context
        ///     from it, recording a failure of that setup on the execution state.
        /// </summary>
        /// <exception cref="Exception">Rethrows whatever the resolution raised.</exception>
        /// <param name="executionState">The state of the current scheduled execution.</param>
        /// <param name="registeredStep">The step instance held by this invoker.</param>
        /// <returns>
        ///     The created scope together with the step and flow context resolved from it.
        /// </returns>
        /// =================================================================================================
        private (IServiceScope Scope, IPipelineFlowStep<T> Step, IPipelineFlowContext<T> Context) ResolveDispatchScope(
            ScheduledExecutionState executionState, IPipelineFlowStep<T> registeredStep)
        {
            IServiceScope scope = null;

            try
            {
                scope = _scopeFactory.CreateScope();

                return (scope, ResolveScopedStep(scope, registeredStep),
                    scope.ServiceProvider.GetService<IPipelineFlowContext<T>>() ?? _context);
            }
            catch (Exception setupException)
            {
                executionState.SetupError = setupException;

                scope?.Dispose();

                throw;
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Registers a stop of the scheduled job against the pipeline cancellation, reporting a stop
        ///     that fails either way it can fail.
        /// </summary>
        /// <param name="job">The scheduled job to stop.</param>
        /// <param name="stepName">The runtime type name of the scheduled step.</param>
        /// <param name="cancellationToken">A token that allows processing to be cancelled.</param>
        /// <returns>
        ///     The registration, which the caller must dispose once the job no longer needs stopping.
        /// </returns>
        /// =================================================================================================
        private CancellationTokenRegistration RegisterStopOnCancellation(IScheduledJob job, string stepName,
            CancellationToken cancellationToken)
            => cancellationToken.Register(() =>
            {
                try
                {
                    _ = job.StopAsync().ContinueWith(
                        t => _logger.IfEnabledWrite(LogLevel.Error,
                            PipelineFlowEventIds.ScheduledStepStopFaulted,
                            PipeInvokeMessage.ScheduledStepXStopFaulted.FormatWith(stepName), t.Exception),
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
                catch (Exception stopException)
                {
                    _logger.IfEnabledWrite(LogLevel.Error,
                        PipelineFlowEventIds.ScheduledStepStopFaulted,
                        PipeInvokeMessage.ScheduledStepXStopFaulted.FormatWith(stepName),
                        stopException);
                }
            });

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Classifies the terminal outcome of a dispatched execution from the completed job task and the
        ///     state its work delegate left behind.
        /// </summary>
        /// <param name="completion">The completed job task of the dispatched execution.</param>
        /// <param name="executionState">The state written by the work delegate of the dispatched execution.</param>
        /// <param name="cancellationToken">The token supplied to the pipeline invocation.</param>
        /// <returns>
        ///     The terminal dispatch status and the exception that goes with it, or null when there is none.
        /// </returns>
        /// =================================================================================================
        private static (PipelineDispatchStatusType Status, Exception Error) ClassifyDispatch(Task completion,
            ScheduledExecutionState executionState, CancellationToken cancellationToken)
        {
            if (completion.IsFaulted.IsTrue())
                return (PipelineDispatchStatusType.Faulted, completion.Exception);

            if (completion.IsCanceled.IsTrue())
                return (PipelineDispatchStatusType.Canceled, null);

            var lastError = executionState.LastError;

            if (lastError is OperationCanceledException canceledException
                && canceledException.IsPipelineCancellation(cancellationToken).IsTrue())
                return (PipelineDispatchStatusType.Canceled, null);

            if (executionState.SetupError.IsNotNull())
                return (PipelineDispatchStatusType.Faulted, new AggregateException(executionState.SetupError));

            if (lastError.IsNotNull())
                return (PipelineDispatchStatusType.Faulted, new AggregateException(lastError));

            if (executionState.Completed.IsTrue())
                return (PipelineDispatchStatusType.Completed, null);

            return (PipelineDispatchStatusType.NotObserved, null);
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Reports the terminal outcome of a dispatched (fire-and-forget) step to the registered
        ///     dispatch observer, if any.
        /// </summary>
        /// <param name="stepName">The runtime type name of the dispatched step.</param>
        /// <param name="dispatch">
        ///     The already classified terminal status of the dispatch and the exception that goes with it.
        ///     The status is never re-derived here, so the observer and the logger always agree.
        /// </param>
        /// =================================================================================================
        private void RaiseDispatchCompleted(string stepName,
            (PipelineDispatchStatusType Status, Exception Error) dispatch)
        {
            var observer = _dispatchObserver;
            if (observer.IsNull())
                return;

            var outcome = new PipelineDispatchOutcome(stepName, dispatch.Status, dispatch.Error);

            try
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        observer.OnDispatchCompleted(outcome);
                    }
                    catch (Exception observerException)
                    {
                        _logger.IfEnabledWrite(LogLevel.Error,
                            PipelineFlowEventIds.DispatchObserverFaulted,
                            PipeInvokeMessage.DispatchObserverForStepXThrew.FormatWith(stepName),
                            observerException);
                    }
                });
            }
            catch (Exception scheduleException)
            {
                _logger.IfEnabledWrite(LogLevel.Error,
                    PipelineFlowEventIds.DispatchObserverFaulted,
                    PipeInvokeMessage.DispatchObserverForStepXThrew.FormatWith(stepName),
                    scheduleException);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Logs empty step data on the invocation result.
        /// </summary>
        /// <remarks>
        ///     The caller's <paramref name="result"/> instance is completed in place instead of a new
        ///     one being returned, otherwise the state-transition events already recorded on it are lost. 
        /// </remarks>
        /// <param name="result">The result of the current invocation.</param>
        /// <returns>
        ///     A PipeLineResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        private PipeLineResult<T> LogEmptyStepData(PipeLineResult<T> result)
        {
            SetLogAndEvent(LogLevel.Error, PipeInvokeMessage.NoPipelineSteps);

            return result
                .SetFailure()
                .SetMessage(PipeInvokeMessage.NoPipelineSteps)
                .SetFlowEvent(_events)
                .SetState(PipelineStateType.Finish)
                .SetStatus(PipelineStatusType.Fail)
                .AsPipelineResult();
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Executes the pre execution validation asynchronous operation.
        /// </summary>
        /// <exception cref="OperationCanceledException">
        ///     Thrown when a thread cancels a running operation.
        /// </exception>
        /// <param name="executePrecondition">The execute precondition.</param>
        /// <param name="currentObject">The current object.</param>
        /// <param name="cancellationToken">A token that allows processing to be cancelled.</param>
        /// <returns>
        ///     The validation outcome, the message describing why the pipeline must stop, and whether
        ///     the failure is eligible for <see cref="PipelineStepPreValidationFailStrategyType.StepSkip"/>.
        ///     A precondition that threw is reported with a different message than one that returned
        ///     false, so the two remain distinguishable on the result and in the event stream, and is
        ///     never skippable: a precondition that cannot be evaluated always halts the pipeline. 
        /// </returns>
        /// =================================================================================================
        private async Task<(bool IsValid, string StopMessage, bool CanSkipStep)> DoPreExecutionValidationAsync(
            Func<T, CancellationToken, Task<bool>> executePrecondition, T currentObject, CancellationToken cancellationToken)
        {
            try
            {
                var preValidation = await executePrecondition.Invoke(currentObject, cancellationToken).ConfigureAwait(false);
                if (preValidation.IsFalse())
                    return (false, PipeInvokeMessage.PreValidationExecutionStepHalt, true);

                return (true, null, false);
            }
            catch (OperationCanceledException e) when (e.IsPipelineCancellation(cancellationToken))
            {
                throw;
            }
            catch (Exception e)
            {
                var message = PipeInvokeMessage.PreValidationExecutionStepThrewException.FormatWith(e.Message);

                // Log event information
                SetLogAndEvent(LogLevel.Critical, message, e);

                return (false, message, false);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Converts an exception thrown by a pipeline step into a failed step result, so the
        ///     configured
        ///     <see cref="PipelineStepFailExecutionStrategyType"/> stays in control of the flow.
        /// </summary>
        /// <param name="step">The pipeline step that threw.</param>
        /// <param name="stepException">The exception thrown by the pipeline step.</param>
        /// <returns>
        ///     A failed PipeLineStepResult&lt;T&gt; carrying the originating exception message.
        /// </returns>
        /// =================================================================================================
        private PipeLineStepResult<T> BuildFailedStepResult(IPipelineFlowStep<T> step, Exception stepException)
        {
            SetLogAndEvent(LogLevel.Warning,
                PipeInvokeMessage.ExecStepXThrewException.FormatWith(step.ExecutionOrderIndex, stepException.Message),
                stepException);

            var failedStepResult = new PipeLineStepResult<T>();
            failedStepResult.SetFailure(stepException.Message);

            return failedStepResult;
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
        ///     Sets log and event, tagging the log entry with a stable event identifier.
        /// </summary>
        /// <remarks>
        ///     The flow event carries no identifier: only the logger entry does, because the identifier
        ///     exists for sinks and alert rules that match on a number instead of on message text.
        /// </remarks>
        /// <param name="level">The level.</param>
        /// <param name="eventId">The stable event identifier of the log entry.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">(Optional) The exception.</param>
        /// =================================================================================================
        private void SetLogAndEvent(LogLevel level, EventId eventId, string message, Exception exception = null)
        {
            _logger.IfEnabledWrite(level, eventId, message, exception);
            _events.Add(new PipelineFlowEvent(level, this.GetType().Name,
                DefaultMessagesHelper.FormatEventLog.FormatWith(this.GetType().GetFlowName(), message), exception));
        }
    }
}