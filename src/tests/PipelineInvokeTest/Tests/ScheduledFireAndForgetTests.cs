// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineInvokeTest
//  Author            : RzR
//  Created           : 19-06-2026 19:06
// 
//  Last Modified By : RzR
//  Last Modified On : 19-06-2026 21:51
//  ***********************************************************************
//  <copyright file="ScheduledFireAndForgetTests.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Pipelines;
using PipelineInvokeTest.Pipelines.Steps.Person;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class ScheduledFireAndForgetTests
    {
        private IServiceCollection _serviceCollection;

        [TestInitialize]
        public void Init()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory, LoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug));

            _serviceCollection = serviceCollection;
        }

        [TestMethod]
        public async Task FireAndForget_ScheduledStep_WithUndefinedStrategy_DoesNotFailPipeline()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNameFireAndForgetPipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result,
                "InvokeAsync must return a non-null result for a dispatched scheduled step.");
            Assert.IsTrue(result.IsSuccess,
                "A fire-and-forget scheduled step must NOT cause the pipeline to fail: " +
                "the invoker must skip result evaluation and advance to the next step.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                "Pipeline Status must be Success when the only step is dispatched fire-and-forget.");
        }

        [TestMethod]
        public async Task FireAndForget_ScheduledStep_WithStepRetryStrategy_DoesNotTriggerRetryOrFailPipeline()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, PersonSetNamePipelineStep>();

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, PersonSetNameFireAndForgetPipelineStep>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess,
                "A fire-and-forget scheduled step must NOT trigger StepRetry or fail the pipeline.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                "Pipeline Status must be Success: dispatched step result must not be evaluated.");
        }

        [TestMethod]
        public async Task FireAndForget_ScheduledStep_DoesNotBlockSubsequentSteps_AndPipelineSucceeds()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, PersonSetIdPipelineStep>();

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, PersonSetNameFireAndForgetPipelineStep>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess,
                "Pipeline must succeed: the dispatched step must not block or fail the pipeline.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status);
            Assert.AreEqual(PipelineStateType.Finish, result.State);

            Assert.IsNotNull(result.FlowResponse,
                "FlowResponse must be populated by the normally-executing step that ran before the dispatched step.");
            Assert.AreNotEqual(Guid.Empty, result.FlowResponse.Id,
                "PersonSetIdPipelineStep (Simple/normal) must have set a non-empty Id.");
        }
    }
}