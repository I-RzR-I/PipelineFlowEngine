// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-09 17:45
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-09 17:45
// ***********************************************************************
//  <copyright file="DocumentProcessPipelineFlowContext.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using PipelineFlowEngine.Abstractions;
using PipelineFlowEngine.Enums;
using PipelineInvokeTest.Models;
// ReSharper disable ClassNeverInstantiated.Global

namespace PipelineInvokeTest.Pipelines
{
    public class DocumentProcessPipelineFlowContext : IPipelineFlowContext<DocumentItemDto>
    {
        /// <inheritdoc />
        public PipelineStepFailExecutionStrategyType FailExecutionStrategy
            => PipelineStepFailExecutionStrategyType.StepRetry;

        /// <inheritdoc />
        public bool IsEnabledStepResultCollector => true;
    }
}

