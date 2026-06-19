// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-14 13:53
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-14 13:53
// ***********************************************************************
//  <copyright file="DocSetInProcessPipelineStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Enums;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Services;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Reflection.TypeParam;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineInvokeTest.Pipelines.Steps.Document
{
    public class DocSetInProcessPipelineStep : PipeLineFlowStep<DocumentItemDto>
    {
        private static IServiceProvider _serviceProvider;
        private static ILogger<DocSetInProcessPipelineStep> _logger;

        public DocSetInProcessPipelineStep(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<DocSetInProcessPipelineStep>>();
        }

        /// <inheritdoc />
        public override int ExecutionOrderIndex => 2;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineExecutionCommandType ExecutionCommand => PipelineExecutionCommandType.Simple;

        /// <inheritdoc />
        public override Func<DocumentItemDto, Task<bool>> PreExecutionValidationAsync
            => async currentObject =>
            {
                _logger.LogInformation($"Do pre-execution validation on step {nameof(DocSetInProcessPipelineStep)}");

                var service = _serviceProvider.GetRequiredService<DocumentService>();

                var obj = await service.GetAsync(currentObject.Id);
                if (obj.IsNotNull() && obj.IsActive.IsTrue() && obj.Id.IsEmpty().IsFalse()
                    && obj.Status == DocStatusType.New && obj.State == DocStateType.Created)
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
                pipelineStep.Status = DocStatusType.InProcess;

                result.SetResult(pipelineStep, PipelineStatusType.Success);
                result.SetState(PipelineStateType.Finish);
                result.SetMessage("Set in process.");
                result.SetStatus(PipelineStatusType.Success);

                return await Task.FromResult(result.AsPipelineStepResult());
            }
            catch (Exception e)
            {
                return PipeLineStepResult<DocumentItemDto>.Failure()
                    .SetMessage("Failed set in process")
                    .SetFlowEvent(LogLevel.Error, e.Message, e)
                    .SetState(PipelineStateType.Finish)
                    .SetStatus(PipelineStatusType.Fail)
                    .AsPipelineStepResult<DocumentItemDto>();
            }
        }
    }
}

