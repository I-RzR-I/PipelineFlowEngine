#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Pipelines;
using PipelineInvokeTest.Pipelines.Steps.Person;
using PipelineInvokeTest.TestDoubles;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{

    [TestClass]
    public class DispatchScopeTests
    {

        private const int ExecutionSignalTimeoutMilliseconds = 5000;

        private const string NotResolvableInScopeText = "could not be resolved from a new service scope";

        private IServiceCollection _serviceCollection;
        private DispatchProbeRecorder _recorder;
        private RecordingLoggerProvider _recordingLoggerProvider;

        [TestInitialize]
        public void Init()
        {
            _recorder = new DispatchProbeRecorder();
            _recordingLoggerProvider = new RecordingLoggerProvider();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory, LoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder
                .AddConsole()
                .AddProvider(_recordingLoggerProvider)
                .SetMinimumLevel(LogLevel.Debug));

            serviceCollection.AddSingleton(_recorder);
            serviceCollection.AddScoped<ScopedDisposalProbe>();

            _serviceCollection = serviceCollection;
        }

        [TestMethod]
        public async Task FireAndForgetStep_RunsInItsOwnScope_NotTheDisposedCallerScope()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, ScopeProbingFireAndForgetPipelineStep>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var callerScope = localServiceProvider.CreateScope();
            var callerScopeProbe = callerScope.ServiceProvider.GetRequiredService<ScopedDisposalProbe>();
            var invoker = callerScope.ServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var result = await invoker.InvokeAsync(person);

            callerScope.Dispose();

            await AwaitRecordedExecutionAsync(
                "The dispatched step never reported an execution within "
                + $"{ExecutionSignalTimeoutMilliseconds}ms; it either never ran or failed before reaching its body.");

            Assert.IsTrue(result.IsSuccess,
                "A dispatched step must not fail the pipeline; its outcome is never observed by the invocation.");
            Assert.IsTrue(callerScopeProbe.IsDisposed,
                "The scope-bound dependency of the caller scope must be disposed once that scope ended; without "
                + "that this test cannot tell a live scope from a dead one.");

            var executions = _recorder.Snapshot();

            Assert.IsTrue(executions.Length >= 1,
                "The dispatched step must actually run its body after the invocation returned.");
            Assert.IsFalse(executions[0].ProbeWasDisposed,
                "The dispatched step must run against a live scope of its own: the scope-bound dependency it "
                + "received was already disposed, so a real step would be working with a dead database context "
                + "while the caller was told the pipeline succeeded.");

            var executedStep = (ScopeProbingFireAndForgetPipelineStep)executions[0].StepInstance;

            Assert.AreNotSame(callerScopeProbe, executedStep.ScopedDependency,
                "The dependency used by the dispatched execution must come from its own scope, not from the "
                + "caller scope that has already ended.");
        }

        [TestMethod]
        public async Task FireAndForgetStep_ReceivesADifferentInstanceThanTheCallerScopeResolved()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, ScopeProbingFireAndForgetPipelineStep>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var callerScope = localServiceProvider.CreateScope();
            var invoker = callerScope.ServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var callerScopeStep = callerScope.ServiceProvider
                .GetServices<IPipelineFlowStep<PersonDto>>()
                .OfType<ScopeProbingFireAndForgetPipelineStep>()
                .Single();

            await invoker.InvokeAsync(person);
            callerScope.Dispose();

            await AwaitRecordedExecutionAsync(
                "The dispatched step never reported an execution within "
                + $"{ExecutionSignalTimeoutMilliseconds}ms; it either never ran or failed before reaching its body.");

            var executions = _recorder.Snapshot();

            Assert.IsTrue(executions.Length >= 1,
                "The dispatched step must actually run its body after the invocation returned.");
            Assert.AreNotSame(callerScopeStep, executions[0].StepInstance,
                "The dispatched execution must be served by a step re-resolved from its own scope. Running the "
                + "instance the caller scope resolved means it keeps every dependency of that ended scope.");
            Assert.AreNotEqual(
                RuntimeHelpers.GetHashCode(callerScopeStep),
                executions[0].StepInstanceIdentity,
                "The identity recorded by the dispatched execution must differ from the identity of the step "
                + "instance held by the caller scope.");
        }

        [TestMethod]
        public async Task FireAndForgetStep_AddedAsAConstructedInstance_StillRuns()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            var suppliedStep = new ScopeProbingFireAndForgetPipelineStep(_recorder, new ScopedDisposalProbe());

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var callerScope = localServiceProvider.CreateScope();
            var invoker = callerScope.ServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();
            invoker.AddPipelineStep(suppliedStep);

            var result = await invoker.InvokeAsync(person);
            callerScope.Dispose();

            await AwaitRecordedExecutionAsync(
                "A step supplied as a constructed instance never ran within "
                + $"{ExecutionSignalTimeoutMilliseconds}ms; a step that cannot be re-resolved must still be "
                + "executed as supplied instead of being silently dropped.");

            Assert.IsTrue(result.IsSuccess,
                "A dispatched step supplied as an instance must not fail the pipeline.");

            var executions = _recorder.Snapshot();

            Assert.IsTrue(executions.Length >= 1,
                "The supplied step instance must still run its body even though it is unknown to the container.");
            Assert.AreSame(suppliedStep, executions[0].StepInstance,
                "A step that was never registered cannot be re-resolved, so the instance supplied by the caller "
                + "is the one that must run, keeping the dependencies it was constructed with.");

            var notResolvableEntry = _recordingLoggerProvider.Snapshot()
                .FirstOrDefault(x => x.Level == LogLevel.Warning
                    && x.Message != null
                    && x.Message.Contains(NotResolvableInScopeText)
                    && x.Message.Contains(nameof(ScopeProbingFireAndForgetPipelineStep)));

            Assert.IsNotNull(notResolvableEntry,
                "Reusing a supplied instance and its dependencies must be visible to whoever operates the "
                + "application: a Warning naming the step is the only signal that this step was NOT given a "
                + "fresh scope.");
        }

        [TestMethod]
        public async Task WaitSchedulerExecutionStep_StillUsesTheCallerScope()
        {

            var person = new PersonDto { Id = Guid.Empty, Name = "TestName", IsActive = true };

            _serviceCollection.RegisterPipelineFlowEngine<PersonDto, PersonPipelineContext>();
            _serviceCollection.AddPipelineFlowEngineStep<PersonDto, ScopeProbingWaitSchedulerPipelineStep>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();

            var callerScope = localServiceProvider.CreateScope();
            var invoker = callerScope.ServiceProvider.GetPipelineFlowEngineInvoker<PersonDto>();

            var callerScopeStep = callerScope.ServiceProvider
                .GetServices<IPipelineFlowStep<PersonDto>>()
                .OfType<ScopeProbingWaitSchedulerPipelineStep>()
                .Single();

            var result = await invoker.InvokeAsync(person);

            await AwaitRecordedExecutionAsync(
                "A scheduled step the pipeline waits for must have executed by the time the invocation "
                + $"returned; nothing was recorded within {ExecutionSignalTimeoutMilliseconds}ms.");

            Assert.IsTrue(result.IsSuccess,
                "A waited scheduled step that succeeds must leave the pipeline successful.");
            Assert.AreEqual(ScopeProbingWaitSchedulerPipelineStep.AppliedName, result.FlowResponse.Name,
                "The waited step body must have been applied to the pipeline item the caller receives.");

            var executions = _recorder.Snapshot();

            Assert.AreEqual(1, executions.Length,
                "MaxIterations is bound to RetryIterations(1) and the job stops on first success, so a waited "
                + "scheduled step must run exactly once.");
            Assert.AreSame(callerScopeStep, executions[0].StepInstance,
                "A step the pipeline waits for runs while the invocation is still in progress, so it must keep "
                + "using the instance and dependencies the caller resolved; re-resolving it into another scope "
                + "would silently split the unit of work of a single invocation across two scopes.");
            Assert.IsFalse(executions[0].ProbeWasDisposed,
                "The caller scope is alive for the whole invocation, so a waited step must never see its "
                + "scope-bound dependency disposed.");

            callerScope.Dispose();
        }

        private async Task AwaitRecordedExecutionAsync(string failureMessage)
        {
            var signaled = await Task.WhenAny(
                _recorder.Completed,
                Task.Delay(ExecutionSignalTimeoutMilliseconds));

            Assert.AreSame(_recorder.Completed, signaled, failureMessage);
        }
    }
}
