// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-06-24 17:22
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-30 21:35
// ***********************************************************************
//  <copyright file="PersonPipelineContext.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using PipelineFlowEngine.Abstractions;
using PipelineFlowEngine.Enums;
using PipelineInvokeTest.Models;

#endregion

namespace PipelineInvokeTest.Pipelines
{
    public class PersonPipelineContext : IPipelineFlowContext<PersonDto>
    {
        /// <inheritdoc />
        public PipelineStepFailExecutionStrategyType FailExecutionStrategy { get; set; }
            = PipelineStepFailExecutionStrategyType.Undefined;

        /// <inheritdoc />
        public bool IsEnabledStepResultCollector => true;
    }
}