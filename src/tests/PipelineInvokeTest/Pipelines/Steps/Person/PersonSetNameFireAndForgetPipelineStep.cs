// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineInvokeTest
//  Author            : RzR
//  Created           : 19-06-2026 19:06
// 
//  Last Modified By : RzR
//  Last Modified On : 19-06-2026 21:48
//  ***********************************************************************
//  <copyright file="PersonSetNameFireAndForgetPipelineStep.cs" company="RzR SOFT & TECH">
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
    public class PersonSetNameFireAndForgetPipelineStep : PipeLineFlowStep<PersonDto>
    {
        /// <inheritdoc />
        public override int ExecutionOrderIndex => 5;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineExecutionCommandType ExecutionCommand
            => PipelineExecutionCommandType.Schedule;

        /// <inheritdoc />
        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new()
        {
            WaitSchedulerExecution = false, // ← fire-and-forget path under test
            RetryIterations = 1,
            StopExecutionIfSuccessful = false,
            ExecutionSchedulerSettings = new ScheduledJobOptions
            {
                StopOnFailure = false,
                ThrowOnFailure = false,
                FailInterval = TimeSpan.FromMilliseconds(50), 
                SuccessInterval = TimeSpan.FromMilliseconds(50)
            }
        };

        /// <inheritdoc />
        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            var result = new PipeLineStepResult<PersonDto>();
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Fail);
            result.SetMessage("Fire-and-forget step body — result must not be evaluated by the invoker.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}