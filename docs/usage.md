# USING

This package pulls in the following dependencies:

| Package | Version | Purpose |
| ------- | ------- | ------- |
| `RzR.Extensions.Domain` | 6.x | Domain and extension helpers used internally |
| `RzR.Scheduling.RecurringJobs` | 3.x | Scheduler behind `PipelineExecutionCommandType.Schedule` steps |
| `Microsoft.Extensions.Logging.Abstractions` | 6.0.1 | Logging abstractions |

Your consuming project also needs `Microsoft.Extensions.DependencyInjection` (for `IServiceCollection`) and a logging implementation.

To be able to use functionalities, extension methods were implemented for `IServiceCollection` and `IServiceProvider`.

Once the package is installed, you must define the pipeline context and its steps.

#### Implementation order:
1. Define the pipeline and its steps location folder;
2. Define the pipeline context class;
3. Define the pipeline step/s class;
4. Register the pipeline to DI;
5. Register the pipeline steps to DI;
6. Invoke pipeline execution.

> Define the pipeline context class

Pipeline context implementation requires inheritance of the `IPipelineFlowContext<T>`.

The context have o list of properties/options like:
`FailExecutionStrategy` and `IsEnabledStepResultCollector`.

- `FailExecutionStrategy` -> define the pipeline behavior in case of failure. The default strategy is `PipelineStop`.

`PipelineStepFailExecutionStrategyType` definition

| Type      | Description              |
|-----------|--------------------------|
| **Undefined** | Undefined = PipelineStop |
| **PipelineStop** | Represents the pipeline stop execution strategy |
| **StepMoveToNext** | Represents the pipeline move to the next step strategy |
| **StepRetry** | Represents the pipeline retry step execution strategy |

> **WARNING — `StepRetry` requires idempotent steps.**
> A retry re-executes the step against the **same pipeline item instance**. The engine never snapshots, clones, or rolls back that instance. Any mutation or external side effect performed before the failure (HTTP POST, INSERT, payment call) persists into the retry. A step that **throws** is retried too, so one side effect can become `RetryIterations + 1` side effects. Only run idempotent steps under `StepRetry`.

Under `StepRetry` a failed step is retried whether it returned a failed result or threw. Under `StepMoveToNext` the pipeline continues past a step that threw, running the remaining steps against a partially mutated item.

**WARNING — under `StepMoveToNext` a step that throws no longer fails the pipeline.** The exception is converted into a failed step result and recorded as a **Warning** event (an `Error`/`Critical` event would keep a successfully retried pipeline unsuccessful forever). `StepMoveToNext` then continues to the next step, and because no `Error`/`Critical` event was recorded, the run ends with `result.IsSuccess = true`. In `2.0.0.8471` and earlier the same step produced a failure result.

This matters when a step signals denial or rejection **by throwing** — an authorization check or an input validator, for example — while the context uses `StepMoveToNext` for unrelated best-effort steps such as enrichment or notification. After upgrading, that denial is recorded as a Warning and the pipeline reports success; nothing in the return value distinguishes it from a clean run, so an `if (result.IsSuccess)` branch proceeds with an item that was never validated or authorized.

If a step must abort the pipeline, run it under `PipelineStop` semantics. Otherwise inspect `StepResults` for failed entries instead of relying on `result.IsSuccess`.

> **WARNING — `StepResults` is only populated when the collector is on.**
> Every entry is written behind the context's `IsEnabledStepResultCollector`. With it `false` — a supported configuration — `StepResults` comes back **empty**, and a caller following the advice above finds no failed entries and concludes that nothing failed. Under `StepMoveToNext` that leaves a run in which a step threw with **no programmatic evidence at all** that anything went wrong, which is exactly the fail-open this guidance exists to close.
> `Events` is collected unconditionally, so the detection path that always works is:
>
> ```csharp
> if (result.Events.Any(x => x.Exception != null))
> {
>     // a step threw during this run, whatever the collector setting
> }
> ```
>
> Use that, or set `IsEnabledStepResultCollector = true` on any context where a thrown step failure must remain detectable. A guard whose own detection path is conditional is not a guard.

> **NOTE — retry recovery path.** A step that fails and then succeeds on a later attempt now correctly terminates the retry loop.

 - `IsEnabledStepResultCollector` -> controls whether per-step results are collected into `StepResults`. Pipeline `Events` are always collected, regardless of this setting.
 
 ```csharp
public class DocumentProcessPipelineFlowContext : IPipelineFlowContext<DocumentItemDto>
{
    /// <inheritdoc />
    public PipelineStepFailExecutionStrategyType FailExecutionStrategy
        => PipelineStepFailExecutionStrategyType.StepRetry;

    /// <inheritdoc />
    public bool IsEnabledStepResultCollector => true;
}
```


> Define the pipeline step/s class

Pipeline step implementation requires inheritance of the `PipeLineFlowStep<T>`.

The step have o list of properties/options like:
`IsEnabled`, `ExecutionOrderIndex`, `Status`, `State`, `ExecutionCommand`, `RetrySchedulePolicy`, `PreValidationFailStrategy`.
Also is two functions: `PreExecutionValidationAsync` and `ExecuteStepAsync`.
 
 - `IsEnabled` -> True if this pipeline step is enabled, false if not.
 - `ExecutionOrderIndex` -> The pipeline step execution order index.
 - `Status` -> The pipeline step status.

`PipelineStatusType` definition
 
| Type      | Description              |
|-----------|--------------------------|
| **Undefined** | Represents the undefined step status |
| **Success** | Represents the step status when the execution succeeds |
| **Fail** | Represents the step status when the execution failed |

 - `State` -> The pipeline step state.

`PipelineStateType` definition
 
| Type      | Description              |
|-----------|--------------------------|
| **Undefined** | Represents the undefined step state |
| **Initialize** | Represents the initialized step state |
| **Run** | Represents the step state when it is running |
| **Skip** | Represents the step state when it is skipped |
| **Finish** | Represents the step state when the run is finished |

A step skipped by its pre-execution validation under `PreValidationFailStrategy = StepSkip` contributes a `StepResults` entry whose `StepResult.State` is `Skip`.

 - `ExecutionCommand` -> The pipeline step 'execution' command.

`PipelineExecutionCommandType` definition
 
| Type      | Description              |
|-----------|--------------------------|
| **Simple** | Represents the simple/default step execution |
| **Schedule** | Represents the scheduled/cycled step execution |

 - `RetrySchedulePolicy` -> The pipeline step retry schedule policy.
 - `PreExecutionValidationAsync` -> The pipeline step execute precondition. It receives the pipeline item and the `CancellationToken` passed to `InvokeAsync`, so a precondition that performs I/O can observe pipeline cancellation.
 - `PreValidationFailStrategy` -> Defines what happens when `PreExecutionValidationAsync` returns `false`. The default strategy is `PipelineStop`. It is honored **only** on a `false` result; a precondition that throws always halts the pipeline. `FailExecutionStrategy` is never consulted on this branch, so a rejected precondition is never retried.
 - `ExecuteStepAsync` -> Executes the pipeline step asynchronous operation.

`PipelineStepPreValidationFailStrategyType` definition

| Type      | Description              |
|-----------|--------------------------|
| **Undefined** | Undefined = PipelineStop |
| **PipelineStop** | Default. A `false` precondition halts the pipeline and returns a failure result |
| **StepSkip** | A `false` precondition skips only this step; the pipeline continues and records a `SkippedByPreValidation` step result |

> **WARNING — `StepSkip` is fail-open by design.**
> A skipped step does not fail the pipeline, and the run can return `IsSuccess = true`. Do not use `StepSkip` for authorization, entitlement or eligibility guards — keep those on the default `PipelineStop`, where a rejected precondition halts the run. `StepSkip` is for applicability filters ("this document is not in a state I handle"). To detect skips programmatically, inspect `result.StepResults` for entries with `StepIteration == PipelineFlowStepIterationType.SkippedByPreValidation`; every skip is also recorded as a `Warning` event.
> A precondition must also never catch `OperationCanceledException` and return `false`: that converts a cancellation into a validation failure, or under `StepSkip` into a skipped step on an otherwise successful pipeline. Let it propagate.

```csharp
public class DocSetCreatedPipelineStep : PipeLineFlowStep<DocumentItemDto>
{
    private static IServiceProvider _serviceProvider;
    private static ILogger<DocSetCreatedPipelineStep> _logger;

    public DocSetCreatedPipelineStep(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = _serviceProvider.GetRequiredService<ILogger<DocSetCreatedPipelineStep>>();
    }

    /// <inheritdoc />
    public override int ExecutionOrderIndex => 1;

    /// <inheritdoc />
    public override bool IsEnabled => true;

    /// <inheritdoc />
    public override PipelineExecutionCommandType ExecutionCommand => PipelineExecutionCommandType.Simple;

    /// <inheritdoc />
    public override Func<DocumentItemDto, CancellationToken, Task<bool>> PreExecutionValidationAsync
    => async (currentObject, cancellationToken) =>
    {
        _logger.LogInformation($"Do pre-execution validation on step {nameof(DocSetCreatedPipelineStep)}");

        var service = _serviceProvider.GetRequiredService<DocumentService>();

        // Forward the pipeline token, so a cancelled pipeline stops this lookup too.
        var obj = await service.GetAsync(currentObject.Id, cancellationToken);

        return obj.IsNotNull() && obj.Id.IsEmpty().IsFalse();
    };

    /// <inheritdoc />
    public override PipelineStepPreValidationFailStrategyType PreValidationFailStrategy
        => PipelineStepPreValidationFailStrategyType.PipelineStop;

    /// <inheritdoc />
    public override async Task<PipeLineStepResult<DocumentItemDto>> ExecuteStepAsync(
        DocumentItemDto pipelineStep,
        IPipelineFlowContext<DocumentItemDto> context,
        ILogger<PipelineFlowInvoker<DocumentItemDto>> logger,
        CancellationToken cancellationToken = default)
    {
        ...
    }
}
```

> Scheduled steps and `RetrySchedulePolicy`

A step whose `ExecutionCommand` is `PipelineExecutionCommandType.Schedule` is executed through a recurring-job scheduler (`RzR.Scheduling.RecurringJobs`) instead of being run once inline. Its behaviour is configured through the step's `RetrySchedulePolicy` (`PipelineFlowRetryPolicy`):

- `ExecutionSchedulerSettings` (`ScheduledJobOptions`, from the `RzR.Scheduling.RecurringJobs.Models` namespace) — the scheduler configuration. Intervals are `TimeSpan` values, not raw numbers:
    - `SuccessInterval` (`TimeSpan`, default 1 minute) — wait after a successful iteration.
    - `FailInterval` (`TimeSpan`, default 30 seconds) — wait before retrying after a failed iteration.
    - `InitialDelay` (`TimeSpan?`) — delay before the first iteration.
    - `MaxIterations` (`int?`) — maximum number of iterations.
    - `StopOnFirstSuccess` / `StopOnFailure` / `ThrowOnFailure` (`bool`) — stop and throw conditions.
- `RetryIterations` (`int`) — for a scheduled step this maps to the scheduler's `MaxIterations`; for a simple step under a `StepRetry` context it is the retry budget (the step can run up to `RetryIterations + 1` times in total).

    > **WARNING — the retry budget is a side-effect multiplier.**
    > Retries re-run the step against the **same pipeline item instance**; nothing is snapshotted, cloned, or rolled back. Every mutation and every external call made before the failure is repeated on each retry, and a step that throws is retried as well. A step that writes to a database or calls a remote API must be idempotent before you give it a non-zero `RetryIterations`.
    >
    > **WARNING — `RetryIterations = 0` is not "use the default", and it does two different things.**
    > For a **simple** step under a `StepRetry` context it makes the step **non-retryable**. The budget is checked as `stepRetryCount >= RetryIterations` and the counter starts at `0`, so the very first failure exhausts it: the step runs exactly once and the pipeline stops with the retry-exhausted message.
    > For a **scheduled** step (`ExecutionCommand = Schedule`) the value maps onto the scheduler's `MaxIterations`, and the scheduler **rejects it**. `Schedule(...)` throws `ArgumentException: MaxIterations must be >= 1 when set.` before any iteration is attempted. That throw is a scheduling *plumbing* failure, not a step failure: it is raised outside the per-step handling, so no `StepResults` entry is recorded for the step, no fail strategy can absorb it, it is logged and recorded as a `Critical` event, its message becomes `result.Message`, and the whole pipeline fails with the step body never having run.

- `WaitSchedulerExecution` (`bool`):
    - `true` — the pipeline awaits the scheduled job to finish and evaluates the result of its **last** iteration (a "run/poll until ready, then continue" pattern). See the note below once `RetryIterations` is greater than 1.
    - `false` — **fire-and-forget**: the step is dispatched to the scheduler and the pipeline advances immediately, without waiting for or evaluating its result. Background faults are logged but are never surfaced on the pipeline result. See the warning below before using it.

> **NOTE — a waited scheduled step is judged on its LAST iteration.**
> With `RetryIterations > 1` the scheduler may run the step many times before the job ends, and *which* of those runs the caller sees is the whole question. The pipeline evaluates the last one: if it succeeded, the step is successful and a failure the scheduler absorbed on an earlier attempt is **not** carried forward — that is exactly the transient blip a scheduled retry budget exists to absorb. If it failed, the step fails even when an earlier attempt succeeded, and the cause of that final failure reaches both `result.Message` and the step's `StepResults` entry. Intermediate absorbed failures are invisible on the result; they appear in the log only.
>
> A waited scheduled step contributes exactly **one** `StepResults` entry, tagged `FirstExecution`, however many scheduler iterations it ran. The iterations belong to the scheduler, not to the pipeline's own `StepRetry` loop, so they never produce `RetryExecution` entries and the entry count never reveals how many times the step actually ran.
- `StopExecutionIfSuccessful` (`bool`) — maps to the scheduler's `StopOnFirstSuccess` (stop after the first successful iteration).
- `ThreadSleepBeforeExecution` (`bool`) — when `true` and `SuccessInterval` is greater than zero, defers the first iteration by `SuccessInterval`.

> **WARNING — fire-and-forget runs outside the DI scope that started the pipeline.**
> When `WaitSchedulerExecution` is `false`, the step is dispatched and `InvokeAsync` returns without waiting. The scheduler callback closes over the step instance, the pipeline context, the logger and the pipeline item — all resolved from the DI scope that owns the invoker, and `RegisterPipelineFlowEngine` registers all of them as `Scoped` by default. In a scoped host (an ASP.NET Core request, a worker `IServiceScope`, a message-handler scope) that scope is disposed as soon as the pipeline returns, so the background callback runs against disposed objects. Anything scope-bound it captured — `DbContext`, unit of work, scoped cache, ambient transaction — will typically fail with `ObjectDisposedException`. **That failure is invisible to the caller: the pipeline already reported success.**

Guidance for fire-and-forget steps:

- Use it only for steps that own their dependencies outright and hold nothing scope-bound.
- If the step needs scoped services, capture `IServiceScopeFactory` in the step and create a fresh scope inside `ExecuteStepAsync`; resolve everything from that scope.
- Never use it for work whose failure matters. There is no delivery, completion, or durability guarantee.
- In short-lived hosts (a console job, a function invocation) the process may exit before the dispatched step runs at all.

A fire-and-forget step also contributes exactly **one placeholder entry** to `StepResults`, with `StepIteration = Dispatched`, `Status = Undefined` and `State = Run`. The entry records that the step was dispatched; it is **never** updated with the eventual outcome.

```csharp
public override PipelineExecutionCommandType ExecutionCommand
    => PipelineExecutionCommandType.Schedule;

public override PipelineFlowRetryPolicy RetrySchedulePolicy => new()
{
    WaitSchedulerExecution = true, // false = fire-and-forget
    RetryIterations = 3, // maps to the scheduler MaxIterations
    StopExecutionIfSuccessful = true, // stop after the first success
    ExecutionSchedulerSettings = new ScheduledJobOptions
    {
        StopOnFailure = false,
        ThrowOnFailure = false,
        FailInterval = TimeSpan.FromMinutes(1),
        SuccessInterval = TimeSpan.FromMinutes(1)
    }
};
```

**Important:** a scheduled step owns its own repetition through the scheduler (`MaxIterations`). A scheduled step running under a context whose `FailExecutionStrategy` is `StepRetry` is treated as terminal on failure — the context-level `StepRetry` does **not** additionally re-run a scheduled step (the two retry mechanisms do not stack).

> Observing fire-and-forget dispatch outcomes

**Why the outcome is not on the result.** A dispatched step finishes after `InvokeAsync` has already returned. The `PipeLineResult<T>` the caller holds is a snapshot taken at dispatch time and is owned by the caller from that moment on, so the engine deliberately never writes the eventual outcome into `Events` or `StepResults` — it would mutate an object somebody is already reading, and it could turn a result that reported success into a failure long after the fact. The dispatch outcome is therefore reported out of band, through an observer or through the log.

This is the missing half of the fire-and-forget DI-scope warning above: a dispatched step that dies with `ObjectDisposedException` because its scope was gone is exactly the failure that never reaches the caller. An observer is how you find out it happened.

**`IPipelineDispatchObserver<T>`** has a single method:

```csharp
void OnDispatchCompleted(PipelineDispatchOutcome outcome);
```

`PipelineDispatchOutcome` is immutable and carries `StepName` (the runtime type name of the step), `Status`, `Exception` (always an `AggregateException` when the dispatch faulted — the one the job exposes when it surfaced the failure, or a wrapper around the exception the scheduler absorbed — and `null` otherwise) and `CompletedOnUtc`. It carries no step instance, no context and no pipeline item: by then the service scope of the dispatched execution is disposed and the flow result belongs to the caller.

```csharp
public sealed class DocumentDispatchObserver : IPipelineDispatchObserver<DocumentItemDto>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentDispatchObserver> _logger;

    public DocumentDispatchObserver(
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentDispatchObserver> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void OnDispatchCompleted(PipelineDispatchOutcome outcome)
    {
        try
        {
            if (outcome.Status == PipelineDispatchStatusType.Completed)
                return;

            // The scope that ran the pipeline was disposed before this callback fired,
            // so scoped services must come from a scope created here.
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IDispatchFailureStore>();

            store.Record(outcome.StepName, outcome.Status, outcome.CompletedOnUtc, outcome.Exception);
        }
        catch (Exception e)
        {
            // The observer must never throw; a reporting failure stops here.
            _logger.LogError(e, "Failed to record dispatch outcome for {Step}.", outcome.StepName);
        }
    }
}
```

**Registration is always singleton.**

```csharp
_serviceCollection.RegisterPipelineFlowEngine<DocumentItemDto, DocumentProcessPipelineFlowContext>();
_serviceCollection.AddPipelineFlowDispatchObserver<DocumentItemDto, DocumentDispatchObserver>();
```

`AddPipelineFlowDispatchObserver<TPipelineItem, TObserver>` takes no lifetime argument and always registers a singleton, because no other lifetime is correct. The invoker captures the observer when it is constructed, and calls it on a background thread only after `InvokeAsync` returned — that is, after the scope that resolved the invoker was disposed. A scoped observer would either be invoked through a disposed scope or keep that scope and its connections alive for the whole background execution. If the observer needs scoped services, take `IServiceScopeFactory` in its constructor and create a scope inside the callback, as in the example above.

The observer is optional: register nothing and the engine behaves exactly as before, with dispatch failures visible through the logger only.

**One observer per pipeline item type, and the last registration wins.** `AddPipelineFlowDispatchObserver<TPipelineItem, TObserver>` uses `AddSingleton`, so calling it twice for the same `TPipelineItem` does not register two observers and does not raise an error: the second registration silently takes over and the first is never invoked again. This is easy to do by accident when two composition-root modules each register their own observer. If several things must react to one dispatch, register a single observer that fans out to them itself.

`PipelineDispatchStatusType` definition

| Type      | Description              |
|-----------|--------------------------|
| **Completed** | The **last** iteration ran the step body through without throwing |
| **Faulted** | The **last** iteration ended with an exception, whether the job surfaced it or the scheduler absorbed it; `outcome.Exception` carries it. Includes an `OperationCanceledException` raised by a **foreign** token |
| **Canceled** | The job was canceled, or its last iteration raised an `OperationCanceledException` **while the token you passed to `InvokeAsync` was cancelled**; no exception is carried |
| **NotObserved** | The scheduled job **ran to completion without ever invoking the step body** — the iteration budget was spent without an invocation, or the job was stopped before it reached one |

`Undefined` is the default enum value and is never reported by the engine. A schedule the scheduler **rejects** (`RetryIterations = 0`, see the warning above) reports nothing at all rather than `NotObserved`: the rejection throws out of the scheduling call, no job is ever created, and the failure surfaces on the invocation result instead.

`NotObserved` exists so that silence is not ambiguous. Without it, "no fault was reported" would mean either "the step ran and succeeded" or "the step never ran at all" — two very different operational situations, one of which needs no action and one of which means the work was silently dropped.

**One outcome per job, not per iteration.** A scheduled job may run its step many times, but the observer is called exactly **once**, when the job reaches its terminal state, and the status describes the **last** iteration only. A failure on an earlier iteration that the scheduler absorbed and then retried is invisible here: the job can end `Completed` even though some attempts along the way threw. If you need per-attempt visibility, take it from the logger, not from the observer.

**`Completed` is not "the step succeeded".** The pipeline never evaluates the `PipeLineStepResult<T>` a dispatched step returns — that is what fire-and-forget means. `Completed` states only that the body did not throw. A step that returns `Status = Fail` and a step that returns `Status = Success` both produce `Completed`. A dispatched step that must report a business failure has to throw, or report it through its own channel.

**A cancellation is judged by the token, not by the exception type.** `Canceled` means *your* pipeline was shut down — it is decided by whether the `CancellationToken` you passed to `InvokeAsync` was cancelled, exactly like every other cancellation decision the engine makes. An `OperationCanceledException` from any other token is a **failure**, not a shutdown, and is reported as `Faulted` with its exception attached. This matters because `HttpClient` throws `TaskCanceledException` (an `OperationCanceledException`) when a request times out: treating that as `Canceled` would hand you an outcome with no exception, and — since it is not `Faulted` — no `EventId` 3002 entry either, so a step failing on every timeout would be invisible on both channels at once.

**`outcome.Exception` is always an `AggregateException`.** A job that surfaces its failure exposes one, and a failure the scheduler absorbed is wrapped in one, so the property never changes shape depending on the route the failure took. Read the originating exception with `outcome.Exception.GetBaseException()`, or enumerate `((AggregateException)outcome.Exception).Flatten().InnerExceptions`.

Contract for the implementation:

- It **must not throw**. If it does, the engine catches and logs the exception (`EventId` 3005) and the pipeline is unaffected; an escaping exception would otherwise reach the scheduler or the caller's shutdown path. Nothing is retried.
- It runs on a background thread, after the caller already has its result. Nothing it does can change that result.
- It fires **only** for fire-and-forget dispatch (`WaitSchedulerExecution = false`). A step with `WaitSchedulerExecution = true` is awaited inline and its outcome is already on the result, so no observer call is made for it.

**Dispatch-path log identifiers.** The dispatch path writes its entries with stable `EventId` values. They are a log contract: sinks, alert rules and dashboards may match on the number, so an identifier is never renumbered or reused for another meaning.

| EventId | Name | Level | Written when |
|---------|------|-------|--------------|
| 3001 | `ScheduledStepDispatched` | Information | A scheduled step was dispatched fire-and-forget; its result is not awaited |
| 3002 | `ScheduledStepDispatchFaulted` | Error | A dispatched step faulted in the background, after the invocation returned. Written whether or not the scheduler surfaced the failure, so a fault the scheduler absorbed is visible even without an observer |
| 3003 | `ScheduledStepStopFaulted` | Error | A scheduled job failed to stop after pipeline cancellation was requested |
| 3004 | `ScheduledStepNotResolvable` | Warning | A dispatched step could not be resolved from the newly created scope, so the originally supplied instance is reused |
| 3005 | `DispatchObserverFaulted` | Error | A registered dispatch observer threw; the exception was caught and logged |
| 3006 | `ScheduledStepDispatchedWithoutScope` | Warning | A step was dispatched fire-and-forget while no `IServiceScopeFactory` was supplied, so it keeps running against the scope that resolved the invoker — the hazard described in the fire-and-forget warning above. Written once per dispatch. Unlike the other rows, this one is also recorded on `result.Events` as a Warning, because it is raised on the invoker thread while the invocation is still running; being a Warning it does **not** affect `IsSuccess` |

If you would rather route the existing entries into your own sink than implement an observer, match on the identifier — never on the message text, which is not a contract:

```csharp
public sealed class DispatchFaultLoggerProvider : ILoggerProvider
{
    private const int ScheduledStepDispatchFaulted = 3002;

    private readonly IDispatchFailureStore _store;

    public DispatchFaultLoggerProvider(IDispatchFailureStore store) => _store = store;

    public ILogger CreateLogger(string categoryName) => new DispatchFaultLogger(_store);

    public void Dispose() { }

    private sealed class DispatchFaultLogger : ILogger
    {
        private readonly IDispatchFailureStore _store;

        public DispatchFaultLogger(IDispatchFailureStore store) => _store = store;

        public IDisposable BeginScope<TState>(TState state) => NullLogger.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (eventId.Id != ScheduledStepDispatchFaulted) return;

            _store.Record(formatter(state, exception), exception);
        }
    }
}

// Startup
_serviceCollection.AddSingleton<ILoggerProvider, DispatchFaultLoggerProvider>();
```

The engine skips an entry entirely when the level is not enabled for its logger, so keep `Warning` and `Error` enabled for the `RzR.PipelineFlowEngine` category, otherwise the entries the bridge waits for are never produced.

> Register the pipeline context and steps to DI

Registration must be defined in the `Startup.cs` file/class or in your related startup application definition.
To register pipeline context and steps are available a few methods:
- `RegisterPipelineFlowEngine` -> Allows for registering pipeline context and/or pipeline steps.
- `AddPipelineFlowEngineStep` -> Allows for registering new pipeline steps.

```csharp
// Register pipeline context and steps
_serviceCollection.RegisterPipelineFlowEngine<DTO, PipelineFlowContext>(
new List<Type> { typeof(STEP1) }, ServiceLifetime.Scoped);

```

```csharp
// Register pipeline context
_serviceCollection.RegisterPipelineFlowEngine<DTO, PipelineFlowContext>();

// Register pipeline steps
_serviceCollection.AddPipelineFlowEngineSteps<DTO>(
    new List<Type>
    {
        typeof(STEP1),
        typeof(STEP2),
        typeof(STEP3)
    });
```

**Lifetime:** the invoker holds mutable per-invocation state, so it must be registered as `Scoped` (the default) or `Transient`. Passing `ServiceLifetime.Singleton` to `RegisterPipelineFlowEngine` throws `NotSupportedException`.

**Concurrency:** `InvokeAsync` is **not** safe for concurrent use on a single invoker instance. Reentering it while an invocation is still running throws `InvalidOperationException`. Resolve one invoker per invocation (this is what the `Scoped`/`Transient` requirement above is for); never share an invoker across parallel requests, tasks, or messages. Sequential re-invocation of the same instance is supported.

> Invoke pipeline execution

```csharp
var objectData = new DTO();

//  Get pipeline invoker service
var invoker = _serviceProvider.GetPipelineFlowEngineInvoker<DTO>();

//  Invoke pipeline execution
var result = await invoker.InvokeAsync(objectData);
```

`InvokeAsync` also accepts a `CancellationToken`. Cancellation of **that** token is honoured between steps and propagated as an `OperationCanceledException` (it is not swallowed into a failure result); a cancelled token also stops any in-flight scheduled jobs.

**Foreign cancellation is treated as a step failure.** If a cancellation is raised *inside* a step by any token other than the one passed to `InvokeAsync` — an `HttpClient` request timeout, a per-call timeout token, a library's internal deadline — it is converted into a failed step result and routed through `FailExecutionStrategy`, so it can be skipped or retried like any other failure. Only cancellation of the pipeline's own token propagates out of `InvokeAsync`.

This is deliberate: at the catch site an `HttpClient` timeout and an abort from a foreign token are indistinguishable, and treating both as step failures is what makes transient-fault retry work.

If a step must abort the whole pipeline, it has to observe the token passed to `ExecuteStepAsync`, or link its own token to it:

```csharp
public override async Task<PipeLineStepResult<DocumentItemDto>> ExecuteStepAsync(
    DocumentItemDto pipelineItem,
    IPipelineFlowContext<DocumentItemDto> context,
    ILogger<PipelineFlowInvoker<DocumentItemDto>> logger,
    CancellationToken cancellationToken = default)
{
    // Own 10s deadline, still cancelled when the pipeline token is cancelled.
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

    // Cancelled by `timeout`   -> step failure, subject to FailExecutionStrategy.
    // Cancelled by the pipeline -> propagates out of InvokeAsync.
    var response = await _httpClient.GetAsync(url, linked.Token);
    ...
}
```

> The result

`InvokeAsync` returns a `PipeLineResult<T>` exposing:

- `IsSuccess` (`bool`) — whether the pipeline completed successfully.
- `Status` (`PipelineStatusType`) and `State` (`PipelineStateType`) — the outcome and lifecycle state.
- `Message` (`string`) — the failure or summary message when set. On a failed run it may embed the **raw text of an exception thrown inside a step**, so treat it as internal diagnostic data: log it, but do not return it verbatim to untrusted callers, where it can disclose connection strings, file paths or internal type names.
- `FlowResponse` (`T`) — the processed object, populated **only when the run completed successfully**. On a failed run it is `null`; see the note below.
- `Events` (`IEnumerable<PipelineFlowEvent>`) — a UTC-timestamped audit log of the run.
- `StepResults` (`IEnumerable<PipelineFlowStepResult<T>>`) — per-step outcomes tagged `FirstExecution`, `RetryExecution`, `Dispatched` or `SkippedByPreValidation`, collected when the context's `IsEnabledStepResultCollector` is `true`.

> **NOTE — on a failed run, read the item you passed in, not `FlowResponse`.**
> `FlowResponse` is assigned only when the pipeline completes successfully, so a failed run returns it as `null`. Nothing is lost: the engine mutates the instance you handed to `InvokeAsync` **in place**, and you still hold that reference. What you hold after a failure is **partially processed** — every step that ran before the failure applied its mutations, and the engine never snapshots, clones or rolls the item back, so a `StepRetry` run may also have applied the same mutation more than once. Treat it as an intermediate state to inspect, discard or compensate for; do not persist it as a finished result. `StepResults` tells you how far the run got.

Notes on `StepResults`:

- A retried step contributes one entry per attempt: the first tagged `FirstExecution`, each retry tagged `RetryExecution`. The entry count is therefore not the step count.
- A fire-and-forget scheduled step (`WaitSchedulerExecution = false`) contributes one placeholder entry tagged `StepIteration = Dispatched`, with `Status = Undefined` and `State = Run`. It is never updated with the eventual outcome, because the pipeline does not wait for it.
- A step skipped by its precondition under `PreValidationFailStrategy = StepSkip` contributes one entry tagged `StepIteration = SkippedByPreValidation`, with `State = Skip`. The step itself never ran.
- Do not index `StepResults` positionally or infer success from the entry count. Read `Status` per entry.
- **Treat the entries as read-only.** `StepResults` is a copy of the list, not of its elements: every `PipelineFlowStepResult<T>` in it is the same instance the invoker created and still holds. Mutating one — or the `PipeLineStepResult<T>` it carries — mutates the engine's own object. Project the entries into your own type before changing anything.
- Never aggregate success over the raw collection: `Dispatched` and `SkippedByPreValidation` entries are permanently unsuccessful by design, so filter them out first — `result.StepResults.Where(x => x.StepIteration != PipelineFlowStepIterationType.Dispatched && x.StepIteration != PipelineFlowStepIterationType.SkippedByPreValidation).All(x => x.StepResult.IsSuccess)` — or simply use `result.IsSuccess`, which already excludes both. Note that `result.IsSuccess` is also true when a step failed under `StepMoveToNext` (see the `StepMoveToNext` warning above).

After registering the pipeline and its steps, can be added/registered new steps with the possibility to define how they will be executed with the method `AddPipelineStep`.

```csharp
var objectData = new DTO();

//  Get pipeline invoker service
var invoker = _serviceProvider.GetPipelineFlowEngineInvoker<DTO>();

// Add new step
invoker.AddPipelineStep(STEPX, PipelineStepExecutionStrategyType.AddInQueue);

//  Invoke pipeline execution
var result = await invoker.InvokeAsync(objectData);
```

**Type-based registration requires a parameterless constructor.** The `AddPipelineSteps(PipelineStepExecutionStrategyType, params Type[])` overload instantiates each type through `Activator.CreateInstance`, so it only supports steps with a parameterless constructor. Steps that need constructor-injected dependencies must be registered through the DI extensions (`AddPipelineFlowEngineStep` / `AddPipelineFlowEngineSteps`), or added as an already-constructed instance via `AddPipelineStep`.

`PipelineStepExecutionStrategyType` definition

| Type      | Description              |
|-----------|--------------------------|
| **AddInQueue** | Represents the strategy -> default option; execute steps by default filters |
| **ForceExecute** | Represents the strategy -> execute only added steps |
| **PriorityExecute** | Represents the strategy -> execute steps with priority tag, then all remaining |
