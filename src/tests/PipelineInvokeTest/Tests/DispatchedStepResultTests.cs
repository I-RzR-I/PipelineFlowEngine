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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class DispatchedStepResultTests
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
        public async Task StepResultCollector_OnFireAndForgetScheduledStep_AddsSingleDispatchedPlaceholder_WithoutFailingPipeline()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type>
                {
                    typeof(PersonSetNamePipelineStep),
                    typeof(PersonSetNameFireAndForgetPipelineStep)
                });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            Assert.IsNotNull(result,
                "InvokeAsync must return a non-null result.");
            Assert.IsNotNull(result.StepResults,
                "IsEnabledStepResultCollector is true so StepResults must be populated.");

            var dispatchedEntry = result.StepResults.Single(x =>
                x.StepName == nameof(PersonSetNameFireAndForgetPipelineStep));

            Assert.AreEqual(PipelineFlowStepIterationType.Dispatched, dispatchedEntry.StepIteration,
                "A fire-and-forget scheduled step must contribute exactly one placeholder entry labelled Dispatched.");
            Assert.IsNotNull(dispatchedEntry.StepResult,
                "The Dispatched placeholder must carry a step result object describing the un-observed execution.");
            Assert.AreEqual(PipelineStateType.Run, dispatchedEntry.StepResult.State,
                "The Dispatched placeholder is recorded at dispatch time, so its State must remain Run.");
            Assert.AreEqual(PipelineStatusType.Undefined, dispatchedEntry.StepResult.Status,
                "The pipeline never observes the outcome of a fire-and-forget step, so its Status must stay Undefined.");
            Assert.IsFalse(dispatchedEntry.StepResult.IsSuccess,
                "The Dispatched placeholder must not claim success: its outcome is unknown to the pipeline.");

            Assert.IsTrue(result.IsSuccess,
                "The Dispatched placeholder must NOT poison the pipeline result: the pipeline must still be successful.");
            Assert.AreEqual(PipelineStatusType.Success, result.Status,
                "Pipeline Status must be Success even though the Dispatched placeholder is not successful.");
            Assert.AreEqual(PipelineStateType.Finish, result.State,
                "Pipeline State must be Finish even though the Dispatched placeholder is not successful.");
        }

        [TestMethod]
        public async Task StepResults_OnReturnedPipelineResult_AreACallerOwnedCopy_UnaffectedByALaterInvocation()
        {

            var firstPerson = new PersonDto { Id = Guid.Empty, Name = "FirstName", IsActive = true };
            var secondPerson = new PersonDto { Id = Guid.Empty, Name = "SecondName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>(
                new List<Type>
                {
                    typeof(PersonSetNamePipelineStep),
                    typeof(PersonSetNameFireAndForgetPipelineStep)
                });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var firstResult = await invoker.InvokeAsync(firstPerson);
            var countAtReturn = firstResult.StepResults.Count();
            var dispatchedCountAtReturn = firstResult.StepResults
                .Count(x => x.StepIteration == PipelineFlowStepIterationType.Dispatched);

            var secondResult = await invoker.InvokeAsync(secondPerson);

            Assert.AreEqual(2, countAtReturn,
                "Exactly two entries are expected at return: one for the executed step and one Dispatched placeholder.");
            Assert.AreEqual(1, dispatchedCountAtReturn,
                "A fire-and-forget step must contribute exactly one Dispatched placeholder and must never "
                + "append further entries for its background iterations.");

            Assert.AreEqual(countAtReturn, firstResult.StepResults.Count(),
                "The returned PipeLineResult owns a copy of the step results; the invoker resetting its own "
                + "per-invocation collection must never mutate a collection already handed to a caller.");
            Assert.AreEqual(dispatchedCountAtReturn,
                firstResult.StepResults.Count(x => x.StepIteration == PipelineFlowStepIterationType.Dispatched),
                "The Dispatched placeholder recorded by the first invocation must still be present and "
                + "unduplicated after a later invocation repopulated the invoker's collection.");
            Assert.AreEqual(2, secondResult.StepResults.Count(),
                "The second invocation must report only its own entries: shared state between the two results "
                + "would show up here as accumulated entries.");
        }
    }
}
