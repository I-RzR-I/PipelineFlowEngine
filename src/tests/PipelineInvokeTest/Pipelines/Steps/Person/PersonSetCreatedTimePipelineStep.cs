// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-11 15:26
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-11 15:26
// ***********************************************************************
//  <copyright file="PersonSetCreatedTimePipelineStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using Microsoft.Extensions.DependencyInjection;
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

namespace PipelineInvokeTest.Pipelines.Steps.Person
{
    public class PersonSetCreatedTimePipelineStep : PipeLineFlowStep<PersonDto>
    {
        private static ILogger<PersonSetCreatedTimePipelineStep> _logger;

        /// <inheritdoc />
        public override int ExecutionOrderIndex => 4;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineExecutionCommandType ExecutionCommand => PipelineExecutionCommandType.Simple;

        /// <inheritdoc />
        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new PipelineFlowRetryPolicy()
        {
            WaitSchedulerExecution = true,
            RetryIterations = 2,
            ExecutionSchedulerSettings = new ScheduledJobOptions()
            {
                StopOnFailure = true,
                ThrowOnFailure = false,
                FailInterval = TimeSpan.FromMinutes(1),
                SuccessInterval = TimeSpan.FromMinutes(1)
            }
        };

        public PersonSetCreatedTimePipelineStep(IServiceProvider serviceProvider)
            => _logger = serviceProvider.GetRequiredService<ILogger<PersonSetCreatedTimePipelineStep>>();

        /// <inheritdoc />
        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            var result = new PipeLineStepResult<PersonDto>();
            result.SetState(PipelineStateType.Initialize);
            try
            {
                result.SetStatus(PipelineStatusType.Fail);
                result.SetState(PipelineStateType.Undefined);

                return await Task.FromResult(result.AsPipelineStepResult());
            }
            catch (Exception e)
            {
                return PipeLineStepResult<PersonDto>.Failure()
                    .SetMessage("Failed set person created date")
                    .SetFlowEvent(LogLevel.Error, e.Message, e)
                    .SetState(PipelineStateType.Finish)
                    .SetStatus(PipelineStatusType.Fail)
                    .AsPipelineStepResult<PersonDto>();
            }
        }
    }
}

