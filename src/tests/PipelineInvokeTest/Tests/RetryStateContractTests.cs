#region U S A G E S

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

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class RetryStateContractTests
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
        public async Task StepRetry_OnFailingStepThatMutatesTheItem_DoesNotRollBackTheItemBetweenAttempts()
        {

            const string originalName = "CallerSuppliedName";
            var person = new PersonDto { Id = Guid.Empty, Name = originalName, IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var step = new MutatingThenFailingPipelineStep();
            invoker.AddPipelineStep(step, PipelineStepExecutionStrategyType.AddInQueue);

            var result = await invoker.InvokeAsync(person);

            Assert.IsFalse(result.IsSuccess,
                "The step always fails, so the pipeline must fail once the retry budget is exhausted.");

            Assert.AreEqual(3, step.ObservedNamesOnEntry.Count,
                "With RetryIterations = 2 the step must be entered 3 times: the first execution plus 2 retries.");

            Assert.AreEqual(originalName, step.ObservedNamesOnEntry[0],
                "Attempt 1 must observe the value supplied by the caller.");

            Assert.AreEqual(MutatingThenFailingPipelineStep.MutatedNameFor(1), step.ObservedNamesOnEntry[1],
                "Attempt 2 must observe the MUTATION left behind by the failed attempt 1: retry does NOT roll " +
                "back the pipeline item and pipeline steps must therefore be idempotent.");

            Assert.AreEqual(MutatingThenFailingPipelineStep.MutatedNameFor(2), step.ObservedNamesOnEntry[2],
                "Attempt 3 must observe the MUTATION left behind by the failed attempt 2: retry does NOT roll " +
                "back the pipeline item and pipeline steps must therefore be idempotent.");
        }
    }
}
