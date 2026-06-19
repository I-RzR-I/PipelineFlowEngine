// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-06-24 16:57
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-30 21:16
// ***********************************************************************
//  <copyright file="PersonPipelineStepTests.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Pipelines;
using PipelineInvokeTest.Pipelines.Steps.Person;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Pipeline;
using RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class PersonPipelineStepTests
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
        public async Task Change_Person_Name_Test()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.AddScoped<PipelineFlowInvoker<PersonDto>>();
            _serviceCollection.AddScoped<IPipelineFlowContext<PersonDto>, PersonPipelineContext>();
            _serviceCollection.AddScoped<IPipelineFlowStep<PersonDto>, PersonSetNamePipelineStep>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetRequiredService<PipelineFlowInvoker<PersonDto>>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.FlowResponse);
            Assert.AreEqual(PipelineStateType.Finish, result.State);
            Assert.AreEqual(PipelineStatusType.Success, result.Status);
            Assert.AreEqual("Person Name", result.FlowResponse.Name);
        }

        [TestMethod]
        public async Task Change_Person_Name_2_Test()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.FlowResponse);
            Assert.AreEqual(PipelineStateType.Finish, result.State);
            Assert.AreEqual(PipelineStatusType.Success, result.Status);
            Assert.AreEqual("Person Name", result.FlowResponse.Name);
        }

        [TestMethod]
        public async Task Change_Person_Id_Name_1_Test()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            invoker.AddPipelineStep(new PersonSetIdPipelineStep());

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.FlowResponse);
            Assert.AreEqual(PipelineStateType.Finish, result.State);
            Assert.AreEqual(PipelineStatusType.Success, result.Status);
            Assert.AreEqual("Person Name", result.FlowResponse.Name);
            Assert.AreNotEqual(Guid.Empty, result.FlowResponse.Id);
        }

        [TestMethod]
        public async Task Change_Person_Id_Name_IsActive_1_Test()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            var steps = new List<AddPipelineStep<PersonDto>>
            {
                new AddPipelineStep<PersonDto>(new PersonSetIdPipelineStep(), PipelineStepExecutionStrategyType.AddInQueue),
                new AddPipelineStep<PersonDto>(new PeronSetInactivePipelineStep(), PipelineStepExecutionStrategyType.AddInQueue)
            }.ToArray();

            invoker.AddPipelineSteps(steps);

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.FlowResponse);
            Assert.AreEqual(PipelineStateType.Finish, result.State);
            Assert.AreEqual(PipelineStatusType.Success, result.Status);
            Assert.AreEqual("Person Name", result.FlowResponse.Name);
            Assert.AreNotEqual(Guid.Empty, result.FlowResponse.Id);
            Assert.AreEqual(false, result.FlowResponse.IsActive);
        }

        [TestMethod]
        public async Task Change_Person_Id_Name_IsActive_2_Test()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            invoker.AddPipelineSteps(PipelineStepExecutionStrategyType.AddInQueue,
                typeof(PersonSetIdPipelineStep), typeof(PeronSetInactivePipelineStep));

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.FlowResponse);
            Assert.AreEqual(PipelineStateType.Finish, result.State);
            Assert.AreEqual(PipelineStatusType.Success, result.Status);
            Assert.AreEqual("Person Name", result.FlowResponse.Name);
            Assert.AreNotEqual(Guid.Empty, result.FlowResponse.Id);
            Assert.AreEqual(false, result.FlowResponse.IsActive);
        }
    }
}