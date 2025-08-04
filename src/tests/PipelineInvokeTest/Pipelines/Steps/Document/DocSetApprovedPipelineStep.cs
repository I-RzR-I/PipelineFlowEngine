// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-14 14:21
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-14 14:21
// ***********************************************************************
//  <copyright file="DocSetApprovedPipelineStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using DomainCommonExtensions.CommonExtensions;
using DomainCommonExtensions.CommonExtensions.TypeParam;
using DomainCommonExtensions.DataTypeExtensions;
using MethodScheduler.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PipelineFlowEngine.Abstractions;
using PipelineFlowEngine.Enums;
using PipelineFlowEngine.Extensions;
using PipelineFlowEngine.Models;
using PipelineFlowEngine.Models.Result;
using PipelineFlowEngine.Pipeline;
using PipelineInvokeTest.Enums;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineInvokeTest.Pipelines.Steps.Document
{
    public class DocSetApprovedPipelineStep : PipeLineFlowStep<DocumentItemDto>
    {
        private static IServiceProvider _serviceProvider;
        private static ILogger<DocSetApprovedPipelineStep> _logger;

        public DocSetApprovedPipelineStep(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<DocSetApprovedPipelineStep>>();
        }

        /// <inheritdoc />
        public override int ExecutionOrderIndex => 4;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineExecutionCommandType ExecutionCommand => PipelineExecutionCommandType.Schedule;
        
        /// <inheritdoc />
        public override PipelineFlowRetryPolicy RetrySchedulePolicy { get; protected set; }
            = new PipelineFlowRetryPolicy()
            {
                StopExecutionIfSuccessful = true,
                WaitSchedulerExecution = true,
                RetryIterations = 2,
                ExecutionSchedulerSettings = new SchedulerSettings()
                {
                    DisableOnFailure = true,
                    ThrowException = false,
                    FailInterval = 2,
                    SuccessInterval = 0.5
                }
            };

        /// <inheritdoc />
        public override Func<DocumentItemDto, Task<bool>> PreExecutionValidationAsync
            => async currentObject =>
            {
                _logger.LogInformation($"Do pre-execution validation on step {nameof(DocSetApprovedPipelineStep)}");

                var service = _serviceProvider.GetRequiredService<DocumentService>();

                var obj = await service.GetAsync(currentObject.Id);
                if (obj.IsNotNull() && obj.IsActive.IsTrue() && obj.Id.IsEmpty().IsFalse()
                    && obj.Status == DocStatusType.OnApprove && obj.State == DocStateType.OnProcessing)
                    return await Task.FromResult(true);
                else
                    return await Task.FromResult(false);
            };

        /// <inheritdoc />
        public override async Task<PipeLineStepResult<DocumentItemDto>> ExecuteStepAsync(
            DocumentItemDto pipelineStep,
            IPipelineFlowContext<DocumentItemDto> context,
            ILogger<PipelineFlowInvoker<DocumentItemDto>> logger,
            CancellationToken cancellationToken = default)
        {
            var result = new PipeLineStepResult<DocumentItemDto>();
            result.SetState(PipelineStateType.Initialize);
            try
            {
                result.SetState(PipelineStateType.Run);
                pipelineStep = pipelineStep.IfIsNull(new DocumentItemDto());

                Thread.Sleep(TimeSpan.FromMinutes(1));

                pipelineStep.ModifiedAt = DateTime.Now;
                pipelineStep.ModifiedById = Guid.NewGuid();
                pipelineStep.ApprovedAt = DateTime.Now;
                pipelineStep.ApprovedById = Guid.NewGuid();
                pipelineStep.State = DocStateType.OnProcessing;
                pipelineStep.Status = DocStatusType.Approved;

                result.SetResult(pipelineStep, PipelineStatusType.Success);
                result.SetState(PipelineStateType.Finish);
                result.SetMessage("Set on approve.");
                result.SetStatus(PipelineStatusType.Success);

                return await Task.FromResult(result.AsPipelineStepResult());
            }
            catch (Exception e)
            {
                return PipeLineStepResult<DocumentItemDto>.Failure()
                    .SetMessage("Failed set on approve")
                    .SetFlowEvent(LogLevel.Error, e.Message, e)
                    .SetState(PipelineStateType.Finish)
                    .SetStatus(PipelineStatusType.Fail)
                    .AsPipelineStepResult();
            }
        }
    }
}

