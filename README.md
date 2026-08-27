> **Note** This repository is developed from the beginning using .netstandard2.0.

| Name     | Details |
|----------|----------|
| RzR.PipelineFlowEngine | [![NuGet Version](https://img.shields.io/nuget/v/RzR.PipelineFlowEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/RzR.PipelineFlowEngine/) [![Nuget Downloads](https://img.shields.io/nuget/dt/RzR.PipelineFlowEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/RzR.PipelineFlowEngine)|

<details>

  <summary>Old version</summary>
  
[![NuGet Version](https://img.shields.io/nuget/v/PipelineFlowEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/PipelineFlowEngine/)
[![Nuget Downloads](https://img.shields.io/nuget/dt/PipelineFlowEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/PipelineFlowEngine)

</details>

<br />

This repository provides a robust and extensible implementation of a unidirectional processing pipeline flow designed to execute a series of interdependent methods. The pipeline architecture implementation is based on the step-by-step execution model, where each step represents a distinct unit of logic that contributes to the overall workflow.

##### **Key Features**

* **Ordered Execution**: Steps are executed sequentially in a single direction, preserving a clear and predictable execution flow.

* **Step Enablement Control**: Each step can be individually enabled or disabled based on runtime conditions or configuration, allowing for dynamic pipeline customization.

* **Custom Execution Order**: Although the default flow is linear, the execution order can be explicitly configured to suit specific business logic or integration scenarios.

* **Pre-Execution Validation**: Before a step is executed, it can optionally perform validation to ensure all preconditions are met. The precondition receives the pipeline item and the `CancellationToken` passed to `InvokeAsync`, so a precondition that performs I/O can observe cancellation. What happens when it returns `false` is declared per step through `PreValidationFailStrategy`: `PipelineStop` (the default) halts the pipeline immediately and returns a failure result, while `StepSkip` skips only that step and lets the pipeline continue, recording a `Warning` event and a `SkippedByPreValidation` entry in `StepResults`. A precondition that **throws** always halts, whatever the strategy — the answer is unknown rather than "no". Pre-validation is **not** subject to the step failure strategy: `FailExecutionStrategy` is never consulted on this branch, so a rejected precondition is never retried.

* **Retry Policies**: Built-in support for retry mechanisms allows transient failures to be handled gracefully. Under the `StepRetry` failure strategy, a simple step is retried both when it returns a failed result and when it **throws** — an exception is converted into a failed step result and routed through the same strategy. The retry budget is `RetryIterations`; scheduled steps instead use a full interval/iteration policy (`ScheduledJobOptions`).

  > **WARNING — steps used under `StepRetry` must be idempotent.**
  > A retry re-executes the step against the **same pipeline item instance**. The engine never snapshots, clones, or rolls back the item. Every mutation and every external side effect performed before the failure (HTTP POST, INSERT, payment call) persists into the retry. Because a step that throws is now retried as well, a single side effect can become `RetryIterations + 1` side effects. Make the step idempotent, or do not run it under `StepRetry`.
  >
  > **WARNING — `RetryIterations = 0` is not "use the default", and it means two different things.**
  > For a **simple** step under a `StepRetry` context it makes the step **non-retryable**: the retry budget is checked as `stepRetryCount >= RetryIterations` with the counter starting at `0`, so the first failure exhausts the budget immediately, the step runs exactly once and the pipeline stops. For a **scheduled** step (`ExecutionCommand = Schedule`) `RetryIterations` maps onto the scheduler's `MaxIterations`, which the scheduler **rejects**: `Schedule(...)` throws `ArgumentException: MaxIterations must be >= 1 when set.` That throw is raised outside the per-step handling, so it is recorded as a `Critical` event, becomes `result.Message`, and fails the whole pipeline **before the step body ever runs** — it is never a quiet no-op.

* **Execution Logging**: Each step's execution is captured as structured pipeline events and per-step results — status, the attempt it belongs to (`FirstExecution`, `RetryExecution`, `Dispatched` for a fire-and-forget scheduled step, or `SkippedByPreValidation` for a step skipped by its precondition), and any exception — to facilitate debugging, monitoring, and auditability. No timing or duration is measured.

* **Scheduled Step Outcomes**: A scheduled step the pipeline waits for (`WaitSchedulerExecution = true`) is judged on the result of its **last** scheduler iteration. A failure the scheduler absorbed on an earlier attempt is not carried forward — a step that recovers is reported as successful — and a final failure is reported with its cause even when an earlier attempt succeeded. Whatever the iteration count, such a step contributes exactly one `StepResults` entry tagged `FirstExecution`; scheduler iterations are not pipeline retries.

* **Background Dispatch Observability**: A fire-and-forget scheduled step (`WaitSchedulerExecution = false`) reports its terminal state — completed, faulted, canceled, or never observed — to an optional `IPipelineDispatchObserver<T>`, because that outcome can no longer reach the result the caller already received. Dispatch-path log entries also carry stable `EventId`s in the 3000-3099 range, so they can be bridged into an existing logging pipeline by identifier instead of by message text.

* **Pipeline failure strategy**: After defining the pipeline, you have the ability to specify how the system should respond to step failures, including strategies such as skipping the step, halting execution, or triggering retries.

  > **NOTE — `result.FlowResponse` is populated only on a successful run.**
  > On a failed run it stays `null`, and that is not a loss of data: the engine mutates the item you passed to `InvokeAsync` **in place**, so you already hold the same instance. Read a failed run's outcome from your own reference, not from `FlowResponse`. That instance is only **partially processed** — every step that ran before the failure applied its mutations and nothing rolls them back — so inspect, discard or compensate for it; never persist it as if the flow had finished.

##### This pattern is especially useful for workflows that involve multiple conditional processing steps, such as:
* Data transformation and enrichment pipelines;
* Business rule evaluations;
* Integration workflows with external services;
* Validation and preprocessing chains.

##### The pipeline is designed with extensibility in mind. Developers can:
* Define new steps by implementing a common interface or base class;
* Inject dependencies into steps using constructor injection;
* Plug in custom validation or retry strategies;
* Persist or export execution logs as needed.

To understand more efficiently how you can use available functionalities please consult the [using documentation/file](docs/usage.md).

##### When to use / when not to use

This library fits well when your processing flow is **in-process, linear, and scoped to a single entity per invocation** — for example, state-transition pipelines, validation chains, data enrichment, or business rule sequences where per-step audit and retry are useful.

It is **not** designed for: durable or long-running workflows that survive process restarts, branching/DAG execution graphs, high-throughput parallel workloads, cross-service orchestration, or scenarios that require rollback and compensation logic.

**In case you wish to use it in your project, you can install the package from <a href="https://www.nuget.org/packages/RzR.PipelineFlowEngine" target="_blank">nuget.org</a>** or specify what version you want:

> `Install-Package RzR.PipelineFlowEngine -Version x.x.x.x`

## Content
1. [USING](docs/usage.md)
2. [CHANGELOG](docs/CHANGELOG.md)
3. [BRANCH-GUIDE](docs/branch-guide.md)