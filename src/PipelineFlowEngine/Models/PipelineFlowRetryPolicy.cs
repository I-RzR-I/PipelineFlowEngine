// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-07-10 13:45
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-10 13:51
// ***********************************************************************
//  <copyright file="PipelineFlowRetryPolicy.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using DomainCommonExtensions.CommonExtensions.TypeParam;
using MethodScheduler.Models;
using PipelineFlowEngine.Abstractions;
using PipelineFlowEngine.Enums;

// ReSharper disable UnusedMember.Global

#endregion

namespace PipelineFlowEngine.Models
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A pipeline flow retry policy.
    /// </summary>
    /// =================================================================================================
    public class PipelineFlowRetryPolicy
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the execution scheduler settings.
        /// </summary>
        /// <value>
        ///     The execution scheduler settings.
        /// </value>
        /// <remarks>
        ///     Used when the pipeline flow step on <seealso cref="IPipelineFlowStep{T}.ExecutionCommand"/>
        ///     is set to the <seealso cref="PipelineExecutionCommandType.Schedule"/> flag.
        /// </remarks>
        /// =================================================================================================
        public SchedulerSettings ExecutionSchedulerSettings { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the retry iterations.
        /// </summary>
        /// <value>
        ///     The retry iterations.
        ///     Default value is 1.
        /// </value>
        /// <remarks>
        ///     Used when the pipeline flow context on <seealso cref="IPipelineFlowContext{T}.FailExecutionStrategy"/>
        ///     is set to the <seealso cref="PipelineStepFailExecutionStrategyType.StepRetry"/> flag.
        ///     Also is used when the pipeline flow step on <seealso cref="IPipelineFlowStep{T}.ExecutionCommand"/>
        ///     is set to the <seealso cref="PipelineExecutionCommandType.Schedule"/> flag to iterate schedule X times.
        /// </remarks>
        /// =================================================================================================
        public int RetryIterations { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets a value indicating whether the wait scheduler execution.
        /// </summary>
        /// <value>
        ///     True if wait scheduler execution, false if not.
        /// </value>
        /// =================================================================================================
        public bool WaitSchedulerExecution { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets a value indicating whether the stop execution if successful.
        /// </summary>
        /// <value>
        ///     True if stop execution if successful, false if not.
        /// </value>
        /// =================================================================================================
        public bool StopExecutionIfSuccessful { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets a value indicating whether the thread sleep before execution.
        /// </summary>
        /// <remarks>
        ///     Waiting time consists of:
        ///     <seealso cref="RetryIterations"/> * 
        ///     <seealso cref="SchedulerSettings.SuccessInterval"/> * 
        ///     <seealso cref="SchedulerSettings.FailInterval"/>
        /// </remarks>
        /// <value>
        ///     True if thread sleep before execution, false if not.
        /// </value>
        /// =================================================================================================
        public bool ThreadSleepBeforeExecution { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipelineFlowRetryPolicy"/> class.
        /// </summary>
        /// =================================================================================================
        public PipelineFlowRetryPolicy()
        {
            ExecutionSchedulerSettings = new SchedulerSettings();
            RetryIterations = 1;
            WaitSchedulerExecution = true;
            StopExecutionIfSuccessful = true;
            ThreadSleepBeforeExecution = false;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipelineFlowRetryPolicy"/> class.
        /// </summary>
        /// <param name="scheduleSetting">
        ///     The schedule setting.
        ///     If the parameter value is null, then object will be initialized with default values.
        /// </param>
        /// <param name="retryIterations">
        ///     The retry iterations. 
        ///     If the parameter value is null, default value will be 1.
        /// </param>
        /// <param name="waitSchedulerExecution">
        ///     (Optional)
        ///     True if wait scheduler execution, false if not.
        ///     Default value is true.
        /// </param>
        /// <param name="stopExecutionIfSuccessful">
        ///     (Optional)
        ///     True if stop scheduler execution when it was successful, false if not.
        ///     Default value is true.
        /// </param>
        /// <param name="threadSleepBeforeExecution">
        ///     (Optional)
        ///     True if before scheduler execution wait time, false if not.
        ///     Default value is false.
        /// </param>
        /// =================================================================================================
        public PipelineFlowRetryPolicy(
            SchedulerSettings scheduleSetting,
            int retryIterations,
            bool waitSchedulerExecution = true,
            bool stopExecutionIfSuccessful = true,
            bool threadSleepBeforeExecution = true)
        {
            ExecutionSchedulerSettings = scheduleSetting.IfIsNull(new SchedulerSettings());
            RetryIterations = retryIterations.IfIsNull(1);
            WaitSchedulerExecution = waitSchedulerExecution;
            StopExecutionIfSuccessful = stopExecutionIfSuccessful;
            ThreadSleepBeforeExecution = threadSleepBeforeExecution;
        }
    }
}