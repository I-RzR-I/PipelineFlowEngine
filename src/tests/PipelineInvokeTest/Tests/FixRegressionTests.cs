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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class FixRegressionTests
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
        public async Task InvokeAsync_WithAlreadyCancelledToken_ThrowsOperationCanceledException()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => invoker.InvokeAsync(person, cts.Token));
        }

        [TestMethod]
        public async Task InvokeAsync_WithNonCancelledToken_CompletesSuccessfully()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            using var cts = new CancellationTokenSource();

            var result = await invoker.InvokeAsync(person, cts.Token);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess,
                "A non-cancelled token must not cause the pipeline to report failure.");
        }

        [TestMethod]
        public async Task InvokeAsync_MultiStepPipeline_WithAlreadyCancelledToken_ThrowsOperationCanceledException()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type>
                {
                    typeof(PersonSetNamePipelineStep),
                    typeof(PersonSetIdPipelineStep),
                    typeof(PeronSetInactivePipelineStep)
                });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => invoker.InvokeAsync(person, cts.Token));
        }

        [TestMethod]
        public void RegisterPipelineFlowEngine_WithStepList_SingletonLifetime_ThrowsNotSupportedException() =>

            Assert.ThrowsException<NotSupportedException>(() =>
                _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                    new List<Type> { typeof(PersonSetNamePipelineStep) },
                    ServiceLifetime.Singleton));

        [TestMethod]
        public void RegisterPipelineFlowEngine_WithoutStepList_SingletonLifetime_ThrowsNotSupportedException() =>

            Assert.ThrowsException<NotSupportedException>(() =>
                _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                    ServiceLifetime.Singleton));

        [TestMethod]
        public void RegisterPipelineFlowEngine_ScopedLifetime_DoesNotThrow_AndInvokerResolves()
        {
            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            Assert.IsNotNull(invoker,
                "Scoped registration must produce a resolvable PipelineFlowInvoker<T>.");
        }

        [TestMethod]
        public void RegisterPipelineFlowEngine_TransientLifetime_DoesNotThrow_AndInvokerResolves()
        {
            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep) },
                ServiceLifetime.Transient);

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            Assert.IsNotNull(invoker,
                "Transient registration must produce a resolvable PipelineFlowInvoker<T>.");
        }

        [TestMethod]
        public async Task StepResultCollector_OnRetryStep_FirstAttemptIsFirstExecution_SubsequentAttemptsAreRetryExecution()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, PersonSetCreatedTimePipelineStep>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var retryIterations = new PersonSetCreatedTimePipelineStep(localServiceProvider)
                .RetrySchedulePolicy.RetryIterations;
            var expectedAttempts = retryIterations + 1;

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsSuccess,
                "The pipeline must fail because PersonSetCreatedTimePipelineStep always returns Fail.");
            Assert.IsNotNull(result.StepResults,
                "IsEnabledStepResultCollector is true so StepResults must be populated.");

            var stepResults = result.StepResults.ToList();

            Assert.AreEqual(expectedAttempts, stepResults.Count,
                $"A step that fails every attempt must be executed {expectedAttempts} times (the first "
                + $"execution plus {retryIterations} retries) and record one step-result entry per attempt.");

            Assert.AreEqual(
                PipelineFlowStepIterationType.FirstExecution,
                stepResults[0].StepIteration,
                "Attempt index 0 (first try, stepRetryCount == 0) must be labelled FirstExecution.");

            for (var i = 1; i < stepResults.Count; i++)
            {
                Assert.AreEqual(
                    PipelineFlowStepIterationType.RetryExecution,
                    stepResults[i].StepIteration,
                    $"Attempt index {i} (stepRetryCount > 0) must be labelled RetryExecution.");
            }
        }

        [TestMethod]
        public async Task StepResultCollector_OnSuccessfulSteps_AllIterationsAreFirstExecution()
        {
            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type> { typeof(PersonSetNamePipelineStep), typeof(PersonSetIdPipelineStep) });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.StepResults);

            var stepResults = result.StepResults.ToList();
            Assert.AreEqual(2, stepResults.Count,
                "Two steps were registered and both succeed; exactly 2 step-result entries expected.");

            foreach (var sr in stepResults)
            {
                Assert.AreEqual(
                    PipelineFlowStepIterationType.FirstExecution,
                    sr.StepIteration,
                    $"Step '{sr.StepName}' succeeded on first try — StepIteration must be FirstExecution.");
            }
        }
    }
}
