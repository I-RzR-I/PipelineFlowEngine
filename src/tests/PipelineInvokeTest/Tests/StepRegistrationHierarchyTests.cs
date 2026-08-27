#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Pipelines;
using PipelineInvokeTest.Pipelines.Steps.Document;
using PipelineInvokeTest.Pipelines.Steps.Person;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class StepRegistrationHierarchyTests
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
        public async Task RegisterPipelineFlowEngine_WithTwoLevelInheritanceStepType_RegistersAndExecutesTheStep()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(TwoLevelInheritancePipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsTrue(result.IsSuccess,
                "A step reaching PipeLineFlowStep<PersonDto> through an intermediate base class must be registered and must succeed.");
            Assert.AreEqual(TwoLevelInheritancePipelineStep.AppliedName, result.FlowResponse.Name,
                "The step must actually have RUN: its entity mutation must be visible in FlowResponse. " +
                "A silently skipped step still yields a successful pipeline, so the mutation is the real proof.");
            Assert.AreEqual(1, result.StepResults.Count(x => x.StepName == nameof(TwoLevelInheritancePipelineStep)),
                "The step-result collector must contain exactly one entry for the two-level inheritance step.");
        }

        [TestMethod]
        public async Task AddPipelineFlowEngineStep_SingleTypeOverload_WithTwoLevelInheritanceStepType_RegistersAndExecutesTheStep()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowEngineStep<PersonDto>(typeof(TwoLevelInheritancePipelineStep));

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsTrue(result.IsSuccess,
                "The single-Type AddPipelineFlowEngineStep overload must accept a step declared through an intermediate base class.");
            Assert.AreEqual(TwoLevelInheritancePipelineStep.AppliedName, result.FlowResponse.Name,
                "The step must actually have RUN via the single-Type registration call site.");
            Assert.AreEqual(1, result.StepResults.Count(x => x.StepName == nameof(TwoLevelInheritancePipelineStep)),
                "The step-result collector must contain exactly one entry for the two-level inheritance step.");
        }

        [TestMethod]
        public async Task AddPipelineSteps_OnInvoker_WithTwoLevelInheritanceStepType_RegistersAndExecutesAllSteps()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            invoker.AddPipelineSteps(
                PipelineStepExecutionStrategyType.AddInQueue,
                typeof(TwoLevelInheritancePipelineStep),
                typeof(PersonSetIdPipelineStep));

            var result = await invoker.InvokeAsync(person);

            Assert.IsTrue(result.IsSuccess,
                "The invoker-level AddPipelineSteps call site must register both steps and the pipeline must succeed.");
            Assert.AreEqual(2, result.StepResults.Count(),
                "BOTH steps must have executed. A silently skipped step produces a pipeline reporting SUCCESS " +
                "while one step never ran - the most dangerous failure mode of this call site.");
            Assert.AreEqual(TwoLevelInheritancePipelineStep.AppliedName, result.FlowResponse.Name,
                "The two-level inheritance step must actually have RUN via invoker.AddPipelineSteps(Type).");
            Assert.AreNotEqual(Guid.Empty, result.FlowResponse.Id,
                "The second, directly-derived step must also have RUN.");
        }

        [TestMethod]
        public void AddPipelineSteps_OnInvoker_WithStepForAnotherPipelineItemType_ThrowsArgumentExceptionNamingTheType()
        {

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var exception = Assert.ThrowsException<ArgumentException>(() =>
                invoker.AddPipelineSteps(
                    PipelineStepExecutionStrategyType.AddInQueue,
                    typeof(MismatchedGenericPipelineStep)));

            Assert.IsTrue(exception.Message.Contains(typeof(MismatchedGenericPipelineStep).FullName),
                $"The exception message must name the offending step type; actual message: '{exception.Message}'.");
            Assert.IsTrue(exception.Message.Contains(typeof(PersonDto).FullName),
                $"The exception message must name the expected pipeline item type; actual message: '{exception.Message}'.");
        }

        [TestMethod]
        public void AddPipelineFlowEngineStep_WithTypeThatIsNotAPipelineStep_ThrowsArgumentExceptionNamingTheType()
        {

            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _serviceCollection.AddPipelineFlowEngineStep<PersonDto>(typeof(string)));

            Assert.IsTrue(exception.Message.Contains(typeof(string).FullName),
                $"The exception message must name the offending type; actual message: '{exception.Message}'.");
            Assert.IsTrue(exception.Message.Contains(typeof(PersonDto).FullName),
                $"The exception message must name the expected pipeline item type; actual message: '{exception.Message}'.");
        }
    }
}
