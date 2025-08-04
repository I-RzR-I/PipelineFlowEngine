> **Note** This repository is developed from the beginning using .netstandard2.0.

| Name     | Details |
|----------|----------|
| PipelineFlowEngine | [![NuGet Version](https://img.shields.io/nuget/v/PipelineFlowEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/PipelineFlowEngine/) [![Nuget Downloads](https://img.shields.io/nuget/dt/PipelineFlowEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/PipelineFlowEngine)|

This repository provides a robust and extensible implementation of a unidirectional processing pipeline flow designed to execute a series of interdependent methods. The pipeline architecture implementation is based on the step-by-step execution model, where each step represents a distinct unit of logic that contributes to the overall workflow.

##### **Key Features**

* **Ordered Execution**: Steps are executed sequentially in a single direction, preserving a clear and predictable execution flow.

* **Step Enablement Control**: Each step can be individually enabled or disabled based on runtime conditions or configuration, allowing for dynamic pipeline customization.

* **Custom Execution Order**: Although the default flow is linear, the execution order can be explicitly configured to suit specific business logic or integration scenarios.

* **Pre-Execution Validation**: Before a step is executed, it can optionally perform validation to ensure all preconditions are met. If validation fails, the pipeline can halt, skip or retry the step based on configuration.

* **Retry Policies**: Built-in support for retry mechanisms allows transient failures to be handled gracefully, with options for retry count, delay strategies, and exception filtering.

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

**In case you wish to use it in your project, u can install the package from <a href="https://www.nuget.org/packages/PipelineFlowEngine" target="_blank">nuget.org</a>** or specify what version you want:

> `Install-Package PipelineFlowEngine -Version x.x.x.x`

## Content
1. [USING](docs/usage.md)
1. [CHANGELOG](docs/CHANGELOG.md)
1. [BRANCH-GUIDE](docs/branch-guide.md)