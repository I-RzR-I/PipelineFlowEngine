// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineInvokeTest
//  Author            : RzR
//  Created           : 19-06-2026 20:06
// 
//  Last Modified By : RzR
//  Last Modified On : 19-06-2026 21:52
//  ***********************************************************************
//  <copyright file="SchedulingRobustnessTests.cs" company="RzR SOFT & TECH">
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
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class SchedulingRobustnessTests
    {
        private IServiceCollection _serviceCollection;

        [TestInitialize]
        public void Init()
        {
            AlwaysFailScheduledPipelineStep.ResetExecutionCount();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory, LoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug));

            _serviceCollection = serviceCollection;
        }

        [TestMethod]
        public async Task ScheduledStep_WithStepRetryContext_TotalExecutionsAreBoundedByRetryIterations_NotAmplified()
        {
            const int retryIterations = 3;

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, AlwaysFailScheduledPipelineStep>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result,
                "InvokeAsync must return a non-null result.");
            Assert.IsFalse(result.IsSuccess,
                "Pipeline must fail: the scheduled step always returns Fail and the invoker " +
                "must treat the scheduler's exhausted result as terminal.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "Pipeline Status must be Fail after the scheduler exhausts all iterations.");

            var actualCount = AlwaysFailScheduledPipelineStep.ExecutionCount;

            Assert.IsTrue(actualCount >= 1,
                $"ExecuteStepAsync must have been called at least once; got {actualCount}.");
            Assert.IsTrue(actualCount <= retryIterations,
                $"Fix #5 bound violated: ExecutionCount ({actualCount}) exceeds RetryIterations ({retryIterations}). " +
                $"The outer StepRetry loop must NOT re-schedule a scheduled step — total executions must be " +
                $"<= {retryIterations} (the scheduler's own MaxIterations), not {retryIterations * (retryIterations + 1)} " +
                $"(the pre-fix amplified value).");
        }

        [TestMethod]
        public async Task FireAndForget_ScheduledStep_ThatThrows_DoesNotCrashOrFailThePipeline()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, ThrowingFireAndForgetPipelineStep>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result,
                "InvokeAsync must return a non-null result when a fire-and-forget step throws.");
            Assert.IsTrue(result.IsSuccess,
                "A fire-and-forget scheduled step that throws must NOT cause the pipeline to fail: " +
                "the fault is observed/logged in the background; the pipeline skips the dispatched step " +
                "and reports Success.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                "Pipeline Status must be Success when the only step is a throwing dispatched scheduled step.");
        }

        [TestMethod]
        public async Task FireAndForget_ThrowingStep_DoesNotBlockSubsequentSteps_NormalStepMutationIsReflected()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, PersonSetNamePipelineStep>();

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, ThrowingFireAndForgetPipelineStep>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess,
                "Pipeline must succeed: the normal step ran; the throwing dispatched step must not stop the pipeline.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status);
            Assert.AreEqual(PipelineStateType.Finish, result.State);

            // Normal step mutation must be present
            Assert.IsNotNull(result.FlowResponse,
                "FlowResponse must be populated by the normal step that ran before the dispatched step.");
            Assert.AreEqual("Person Name", result.FlowResponse.Name,
                "PersonSetNamePipelineStep must have executed and set Name = 'Person Name'.");
        }
    }
}