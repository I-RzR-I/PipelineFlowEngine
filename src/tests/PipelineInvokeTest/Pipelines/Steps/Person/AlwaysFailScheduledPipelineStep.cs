// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineInvokeTest
//  Author            : RzR
//  Created           : 19-06-2026 20:06
// 
//  Last Modified By : RzR
//  Last Modified On : 19-06-2026 21:45
//  ***********************************************************************
//  <copyright file="AlwaysFailScheduledPipelineStep.cs" company="RzR SOFT & TECH">
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
using RzR.PipelineFlowEngine.Extensions;
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
    public class AlwaysFailScheduledPipelineStep : PipeLineFlowStep<PersonDto>
    {
        private static int _executionCount;

        public static int ExecutionCount => Volatile.Read(ref _executionCount);

        /// <inheritdoc />
        public override int ExecutionOrderIndex => 6;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineExecutionCommandType ExecutionCommand
            => PipelineExecutionCommandType.Schedule;

        /// <inheritdoc />
        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new()
        {
            RetryIterations = 3,
            WaitSchedulerExecution = true,
            StopExecutionIfSuccessful = false,
            ExecutionSchedulerSettings = new ScheduledJobOptions
            {
                StopOnFailure = false,
                ThrowOnFailure = false,
                SuccessInterval = TimeSpan.FromMilliseconds(20),
                FailInterval = TimeSpan.FromMilliseconds(20)
            }
        };

        public static void ResetExecutionCount() => Interlocked.Exchange(ref _executionCount, 0);

        /// <inheritdoc />
        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _executionCount);

            var result = new PipeLineStepResult<PersonDto>();
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Fail);
            result.SetMessage("AlwaysFailScheduledPipelineStep — intentionally returns Fail every time.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}