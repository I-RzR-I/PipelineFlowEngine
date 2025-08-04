// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-14 14:15
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-14 14:15
// ***********************************************************************
//  <copyright file="DocSetOnApprovePipelineStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using DomainCommonExtensions.CommonExtensions;
using DomainCommonExtensions.CommonExtensions.TypeParam;
using DomainCommonExtensions.DataTypeExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PipelineFlowEngine.Abstractions;
using PipelineFlowEngine.Enums;
using PipelineFlowEngine.Extensions;
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
    public class DocSetOnApprovePipelineStep : PipeLineFlowStep<DocumentItemDto>
    {
        private static IServiceProvider _serviceProvider;
        private static ILogger<DocSetOnApprovePipelineStep> _logger;

        public DocSetOnApprovePipelineStep(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<DocSetOnApprovePipelineStep>>();
        }

        /// <inheritdoc />
        public override int ExecutionOrderIndex => 3;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineExecutionCommandType ExecutionCommand => PipelineExecutionCommandType.Simple;

        /// <inheritdoc />
        public override Func<DocumentItemDto, Task<bool>> PreExecutionValidationAsync
            => async currentObject =>
            {
                _logger.LogInformation($"Do pre-execution validation on step {nameof(DocSetOnApprovePipelineStep)}");

                var service = _serviceProvider.GetRequiredService<DocumentService>();

                var obj = await service.GetAsync(currentObject.Id);
                if (obj.IsNotNull() && obj.IsActive.IsTrue() && obj.Id.IsEmpty().IsFalse()
                    && obj.Status == DocStatusType.InProcess && obj.State == DocStateType.OnProcessing)
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

                pipelineStep.ModifiedAt = DateTime.Now;
                pipelineStep.ModifiedById = Guid.NewGuid();
                pipelineStep.State = DocStateType.OnProcessing;
                pipelineStep.Status = DocStatusType.OnApprove;

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

