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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{

    [TestClass]
    public class StepSelectionTests
    {

        private const string NoPipelineStepsText = "Execution pipeline step list is empty!";

        private IServiceCollection _serviceCollection;

        [TestInitialize]
        public void Init() => _serviceCollection = BuildServiceCollection();

        [TestMethod]
        public async Task DisabledStep_IsFilteredOut_AndNeverExecutes()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var disabledStep = new DisabledExecutionCountingPipelineStep();
            var enabledStep = new ExecutionCountingPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(disabledStep);
            invoker.AddPipelineStep(enabledStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, disabledStep.ExecutionCount,
                "A step reporting IsEnabled == false must be filtered out of the execution set and must never "
                + "be executed.");
            Assert.AreEqual("TestName", person.Name,
                "The disabled step mutates the pipeline item, so an unchanged item proves it never ran.");
            Assert.AreEqual(1, enabledStep.ExecutionCount,
                "The enabled step registered alongside a disabled one must still execute exactly once.");
            Assert.IsTrue(result.IsSuccess,
                "Filtering out a disabled step is not a failure; the remaining enabled steps decide the "
                + "outcome.");

            Assert.IsFalse(
                result.StepResults.Any(x => x.StepName == nameof(DisabledExecutionCountingPipelineStep)),
                "A disabled step must produce no step result at all.");
            Assert.IsTrue(
                result.StepResults.Any(x => x.StepName == nameof(ExecutionCountingPipelineStep)),
                "The enabled step must be represented in StepResults.");
        }

        [TestMethod]
        public async Task PipelineWhereEveryStepIsDisabled_IsTreatedAsAnEmptyPipeline()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var disabledStep = new DisabledExecutionCountingPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextPipelineStop>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(disabledStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, disabledStep.ExecutionCount,
                "The only registered step is disabled, so nothing may execute.");
            Assert.IsFalse(result.IsSuccess,
                "A pipeline left with no executable step is reported as a FAILURE, not as a successful no-op.");
            Assert.AreEqual(PipelineStatusType.Fail, result.Status,
                "The empty-pipeline path ends with the Fail status.");
            Assert.AreEqual(PipelineStateType.Finish, result.State,
                "The empty-pipeline path ends in the Finish state.");
            Assert.AreEqual(0, result.StepResults.Count(),
                "The empty-pipeline path collects no step results.");
            Assert.IsNull(result.FlowResponse,
                "The empty-pipeline path never assigns the pipeline item to the flow response.");

            Assert.AreEqual(NoPipelineStepsText, result.Message,
                "The empty-pipeline failure must state its reason on the result Message itself. A caller that "
                + "only inspects Message — the common case — would otherwise see a failure with no explanation "
                + "and would have to walk the event stream to learn that nothing was executable.");

            var errorEvents = result.Events
                .Where(x => x.EventLevel == LogLevel.Error)
                .ToList();

            Assert.AreEqual(1, errorEvents.Count,
                $"The empty-pipeline path must raise exactly one Error event; got {errorEvents.Count}.");
            StringAssert.Contains(errorEvents[0].Message, NoPipelineStepsText,
                "The Error event of the empty-pipeline path must state that the step list is empty.");
        }

        [TestMethod]
        public async Task StepResultCollector_WhenDisabled_ProducesNoStepResults_ButStillRunsEverySteps()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };
            var mutatingStep = new PersonSetNamePipelineStep();
            var countingStep = new ExecutionCountingPipelineStep();

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContextNoCollector>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(mutatingStep);
            invoker.AddPipelineStep(countingStep);

            var result = await invoker.InvokeAsync(person);

            Assert.AreEqual(0, result.StepResults.Count(),
                "With the step result collector disabled, no step result is collected at all.");
            Assert.IsTrue(result.IsSuccess,
                "Disabling the step result collector must not change the pipeline outcome.");
            Assert.AreEqual(1, countingStep.ExecutionCount,
                "Disabling the step result collector must not change WHICH steps run.");
            Assert.AreEqual("Person Name", person.Name,
                "The mutation applied by the steps must still be present — the collector is an observability "
                + "switch only, never a control-flow one.");
            Assert.IsNotNull(result.FlowResponse,
                "A successful pipeline must still expose the processed item with the collector disabled.");
            Assert.AreEqual("Person Name", result.FlowResponse.Name,
                "The flow response must carry the mutated item even with the collector disabled.");
        }

        [TestMethod]
        public async Task StepThatIgnoresCancellationAndReturnsFailure_UnderStepRetry_IsNotRetriedAfterCancellation()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            using var cts = new CancellationTokenSource();
            var tokenIgnoringStep = new TokenIgnoringFailingPipelineStep(cts, 5);

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext2>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(tokenIgnoringStep);

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                () => invoker.InvokeAsync(person, cts.Token));

            Assert.AreEqual(1, tokenIgnoringStep.ExecutionCount,
                "The invoker itself must observe the cancellation between retry attempts. A step that ignores "
                + "its token and REPORTS failure would otherwise burn its whole remaining retry budget "
                + "(5 more attempts here) after the caller already gave up, holding the scope and its "
                + "connections open for that entire window.");
        }

        private static IServiceCollection BuildServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory, LoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug));

            return serviceCollection;
        }
    }
}
