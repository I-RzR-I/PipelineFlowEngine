# USING

This repository uses the Microsoft reference packages:
* `Microsoft.Extensions.DependencyInjection`
* `Microsoft.Extensions.Logging.Abstractions`

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

 - `IsEnabledStepResultCollector` -> define the pipeline behavior to collection event logs or not.
 
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
`IsEnabled`, `ExecutionOrderIndex`, `Status`, `State`, `ExecutionCommand`, `RetrySchedulePolicy`.
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

 - `ExecutionCommand` -> The pipeline step 'execution' command.

`PipelineExecutionCommandType` definition
 
| Type      | Description              |
|-----------|--------------------------|
| **Simple** | Represents the simple/default step execution |
| **Schedule** | Represents the scheduled/cycled step execution |

 - `RetrySchedulePolicy` -> The pipeline step retry schedule policy.
 - `PreExecutionValidationAsync` -> The pipeline step execute precondition.
 - `ExecuteStepAsync` -> Executes the pipeline step asynchronous operation.

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
    public override Func<DocumentItemDto, Task<bool>> PreExecutionValidationAsync
    => async currentObject =>
    {
        _logger.LogInformation($"Do pre-execution validation on step {nameof(DocSetCreatedPipelineStep)}");

        var service = _serviceProvider.GetRequiredService<DocumentService>();

        var obj = await service.GetAsync(currentObject.Id);
        if (obj.IsNotNull() && obj.Id.IsEmpty().IsFalse())
            return await Task.FromResult(true);
        else
            return await Task.FromResult(false);
    };

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

> Invoke pipeline execution

```csharp
var objectData = new DTO();

//  Get pipeline invoker service
var invoker = _serviceProvider.GetPipelineFlowEngineInvoker<DTO>();

//  Invoke pipeline execution
var result = await invoker.InvokeAsync(objectData);
```

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

`PipelineStepExecutionStrategyType` definition

| Type      | Description              |
|-----------|--------------------------|
| **AddInQueue** | Represents the strategy -> default option; execute steps by default filters |
| **ForceExecute** | Represents the strategy -> execute only added steps |
| **PriorityExecute** | Represents the strategy -> execute steps with priority tag, then all remaining |
