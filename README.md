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

* **Pre-Execution Validation**: Before a step is executed, it can optionally perform validation to ensure all preconditions are met. If validation fails, the pipeline can halt, skip or retry the step based on configuration.

* **Retry Policies**: Built-in support for retry mechanisms allows transient failures to be handled gracefully, with options for a bounded retry count (`RetryIterations`) for simple steps and a full interval/iteration policy (`ScheduledJobOptions`) for scheduled steps.

* **Execution Logging**: Each step's execution details—including status, duration, and any exceptions—are logged to facilitate debugging, monitoring, and auditability.

* **Pipeline failure strategy**: After defining the pipeline, you have the ability to specify how the system should respond to step failures, including strategies such as skipping the step, halting execution, or triggering retries.

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
2. [BRANCH-GUIDE](docs/branch-guide.md)