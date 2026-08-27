#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Pipelines;
using PipelineInvokeTest.Pipelines.Steps.Person;
using PipelineInvokeTest.Services;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class PersonPipelineFlowWithStepsDITests
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
        public async Task Change_Person_Id_Name_Test()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) }, ServiceLifetime.Scoped);

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            invoker.AddPipelineStep(new PersonSetIdPipelineStep(), PipelineStepExecutionStrategyType.AddInQueue);

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
        public async Task Change_Person_Id_Name_Test_2()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) }, ServiceLifetime.Scoped);

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, PersonSetIdPipelineStep>(ServiceLifetime.Scoped);

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

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
        public async Task Change_Person_Id_Name_Test_2_1()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) }, ServiceLifetime.Scoped);

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto>(typeof(PersonSetIdPipelineStep), ServiceLifetime.Scoped);

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

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
        public async Task Change_Person_Id_Name_Test_3()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) }, ServiceLifetime.Scoped);

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, PersonSetIdPipelineStep>(ServiceLifetime.Scoped);
            _serviceCollection.AddPipelineFlowEngineSteps<PersonDto>(
                new List<Type>
                {
                    typeof(PersonSetIdPipelineStep),
                    typeof(PeronSetInactivePipelineStep)
                }, ServiceLifetime.Scoped);

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

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
        public async Task Change_Person_Id_Name_Test_4()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            _serviceCollection.AddScoped<Service>();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(ServiceLifetime.Scoped);

            _serviceCollection.AddPipelineFlowEngineSteps<PersonDto>(
                new List<Type>
                {
                    typeof(PersonSetNamePipelineStep),
                    typeof(PersonSetIdPipelineStep),
                    typeof(PeronSetInactivePipelineStep),
                    typeof(PersonSetBlockedPipelineStep)
                },
                ServiceLifetime.Scoped);

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.FlowResponse);
            Assert.AreEqual(PipelineStateType.Finish, result.State);
            Assert.AreEqual(PipelineStatusType.Success, result.Status);
            Assert.AreEqual("Person Name", result.FlowResponse.Name);
            Assert.AreNotEqual(Guid.Empty, result.FlowResponse.Id);
            Assert.AreEqual(false, result.FlowResponse.IsActive);
            Assert.AreEqual(true, result.FlowResponse.IsBlocked);
        }

        [TestMethod]
        public async Task CheckScheduledPipeline()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            _serviceCollection.AddScoped<Service>();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(ServiceLifetime.Scoped);

            _serviceCollection.AddPipelineFlowEngineSteps<PersonDto>(
                new List<Type>
                {
                    typeof(PersonSetNamePipelineStep),
                    typeof(PersonSetIdPipelineStep),
                    typeof(PeronSetInactivePipelineStep),
                    typeof(PersonSetBlockedPipelineStep),
                    typeof(PersonSetBlockedTimePipelineStep)
                },
                ServiceLifetime.Scoped);

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var dateBeforeInvocation = DateTime.Now.Date;
            var result = await invoker.InvokeAsync(person);
            var dateAfterInvocation = DateTime.Now.Date;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.FlowResponse);
            Assert.AreEqual(PipelineStateType.Finish, result.State);
            Assert.AreEqual(PipelineStatusType.Success, result.Status);
            Assert.AreEqual("Person Name", result.FlowResponse.Name);
            Assert.AreNotEqual(Guid.Empty, result.FlowResponse.Id);
            Assert.AreEqual(false, result.FlowResponse.IsActive);
            Assert.AreEqual(true, result.FlowResponse.IsBlocked);

            Assert.AreNotEqual(default(DateTime), result.FlowResponse.BlockedOn,
                "The blocked step must stamp a blocked date; a default value means it never ran.");
            Assert.AreEqual(TimeSpan.Zero, result.FlowResponse.BlockedOn.TimeOfDay,
                "The blocked date must be stamped as a date without a time component.");
            Assert.IsTrue(
                result.FlowResponse.BlockedOn >= dateBeforeInvocation
                && result.FlowResponse.BlockedOn <= dateAfterInvocation,
                $"The blocked date ({result.FlowResponse.BlockedOn:O}) must be the date the pipeline ran on, "
                + $"between {dateBeforeInvocation:O} and {dateAfterInvocation:O}.");
        }

        [TestMethod]
        public async Task Person_Set_CreatedDate_WithRetry_Test()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            _serviceCollection.AddScoped<Service>();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>(ServiceLifetime.Scoped);

            _serviceCollection.AddPipelineFlowEngineSteps<PersonDto>(
                new List<Type>
                {
                    typeof(PersonSetNamePipelineStep),
                    typeof(PersonSetIdPipelineStep),
                    typeof(PeronSetInactivePipelineStep),
                    typeof(PersonSetBlockedPipelineStep),
                    typeof(PersonSetCreatedTimePipelineStep)
                },
                ServiceLifetime.Scoped);

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(result.FlowResponse);
            Assert.AreEqual(PipelineStateType.Finish, result.State);
            Assert.AreEqual(PipelineStatusType.Fail, result.Status);
        }
    }
}
