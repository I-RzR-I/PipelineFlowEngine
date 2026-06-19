// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineInvokeTest
//  Author            : RzR
//  Created           : 19-06-2026 20:06
// 
//  Last Modified By : RzR
//  Last Modified On : 19-06-2026 21:48
//  ***********************************************************************
//  <copyright file="ThrowingFireAndForgetPipelineStep.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using RzR.Scheduling.RecurringJobs.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{
    public class ThrowingFireAndForgetPipelineStep : PipeLineFlowStep<PersonDto>
    {
        /// <inheritdoc />
        public override int ExecutionOrderIndex => 7;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineExecutionCommandType ExecutionCommand
            => PipelineExecutionCommandType.Schedule;

        /// <inheritdoc />
        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new()
        {
            WaitSchedulerExecution = false,
            RetryIterations = 1,
            StopExecutionIfSuccessful = false,
            ExecutionSchedulerSettings = new ScheduledJobOptions
            {
                StopOnFailure = false,
                ThrowOnFailure = false, // scheduler itself does not rethrow
                FailInterval = TimeSpan.FromMilliseconds(20),
                SuccessInterval = TimeSpan.FromMilliseconds(20)
            }
        };

        /// <inheritdoc />
        public override Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
                PersonDto pipelineStep,
                IPipelineFlowContext<PersonDto> context,
                ILogger<PipelineFlowInvoker<PersonDto>> logger,
                CancellationToken cancellationToken = default)
            => throw new InvalidOperationException(
                "ThrowingFireAndForgetPipelineStep — intentional throw to verify Fix #2 fault isolation.");
    }
}