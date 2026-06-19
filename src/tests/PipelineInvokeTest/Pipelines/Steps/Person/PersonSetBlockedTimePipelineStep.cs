// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-10 13:31
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-10 13:31
// ***********************************************************************
//  <copyright file="PersonSetBlockedTimePipelineStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Reflection.TypeParam;
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
    public class PersonSetBlockedTimePipelineStep : PipeLineFlowStep<PersonDto>
    {
        private static ILogger<PersonSetBlockedPipelineStep> _logger;

        /// <inheritdoc />
        public override int ExecutionOrderIndex => 3;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineExecutionCommandType ExecutionCommand => PipelineExecutionCommandType.Schedule;

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
        
        public PersonSetBlockedTimePipelineStep(IServiceProvider serviceProvider) 
            => _logger = serviceProvider.GetRequiredService<ILogger<PersonSetBlockedPipelineStep>>();

        /// <inheritdoc />
        public override Func<PersonDto, Task<bool>> PreExecutionValidationAsync { get; protected set; }
            = async currentObject =>
            {
                _logger.LogInformation($"Do pre-execution validation on step {nameof(PersonSetBlockedTimePipelineStep)}");

                return await Task.FromResult(currentObject.IsBlocked.IsTrue());
            };

        /// <inheritdoc />
        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            //var result = PipeLineStepResult<PersonDto>.Instance;
            var result = new PipeLineStepResult<PersonDto>();
            result.SetState(PipelineStateType.Initialize);
            try
            {
                result.SetState(PipelineStateType.Run);
                pipelineStep = pipelineStep.IfIsNull(new PersonDto());

                pipelineStep.BlockedOn = DateTime.Now.Date;
                result.SetResult(pipelineStep, PipelineStatusType.Success);
                result.SetState(PipelineStateType.Finish);
                result.SetMessage("Changed person blocked date in today.");
                result.SetStatus(PipelineStatusType.Success);

                return await Task.FromResult(result.AsPipelineStepResult());
            }
            catch (Exception e)
            {
                return PipeLineStepResult<PersonDto>.Failure()
                    .SetMessage("Failed set person blocked date in today")
                    .SetFlowEvent(LogLevel.Error, e.Message, e)
                    .SetState(PipelineStateType.Finish)
                    .SetStatus(PipelineStatusType.Fail)
                    .AsPipelineStepResult<PersonDto>();
            }
        }
    }
}

