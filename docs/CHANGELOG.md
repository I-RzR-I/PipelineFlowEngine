### **v2.0.0.8471** [[RzR](mailto:108324929+I-RzR-I@users.noreply.github.com)] 19-06-2026
* [DEV] - (RzR) -> Migrate scheduler from obsolete `MultipleScheduler`/`SchedulerSettings` to `IMethodScheduler.Schedule(ScheduledJobOptions, ...)`; scheduled steps now await `IScheduledJob.Completion` deterministically and stop gracefully on cancellation.
* [DEV] - (RzR) -> Replace package dependencies: `DomainCommonExtensions` with `RzR.Extensions.Domain` (6.0.0.8301), `MethodScheduler` → `RzR.Scheduling.RecurringJobs` (3.1.0.8026); bump `Microsoft.Extensions.Logging.Abstractions` 3.1.32 to 6.0.1.
* [DEV] - (RzR) -> Set `RootNamespace`/`AssemblyName` to `RzR.PipelineFlowEngine`.
* [DEV] - (RzR) -> Honor and propagate cancellation: `InvokeAsync` surfaces `OperationCanceledException` instead of swallowing it and stops in-flight scheduled jobs.
* [DEV] - (RzR) -> Treat a scheduled step as terminal under a `StepRetry` context so scheduler iterations and context-level retry no longer stack (double-retry removed).
* [DEV] - (RzR) -> Honor `ThreadSleepBeforeExecution` (defers first iteration by `SuccessInterval`).
* [DEV] - (RzR) -> Register `AddMethodScheduler()` idempotently; throw `NotSupportedException` when the pipeline is registered as `Singleton`.
* [DEV] - (RzR) -> Add fire-and-forget, scheduling-robustness, and regression test suites.
* [FIX] - (RzR) -> Fix fire-and-forget (`WaitSchedulerExecution = false`) dispatched-job leak and observe background faults via `OnlyOnFaulted` logging.

### **v1.0.0.0** [[RzR](mailto:108324929+I-RzR-I@users.noreply.github.com)] 04-08-2025
* [b2a5762] (RzR) -> Init commit.
