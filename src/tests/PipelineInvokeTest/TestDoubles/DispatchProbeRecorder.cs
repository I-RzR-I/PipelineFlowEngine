#region U S A G E S

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.TestDoubles
{

    public sealed class DispatchProbeRecorder
    {
        private readonly object _syncRoot = new object();
        private readonly List<RecordedExecution> _executions = new List<RecordedExecution>();

        private readonly TaskCompletionSource<bool> _firstExecutionSignal =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Completed => _firstExecutionSignal.Task;

        public int ExecutionCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _executions.Count;
                }
            }
        }

        public RecordedExecution[] Snapshot()
        {
            lock (_syncRoot)
            {
                return _executions.ToArray();
            }
        }

        public void Record(object stepInstance, bool probeWasDisposed)
        {
            lock (_syncRoot)
            {
                _executions.Add(new RecordedExecution(stepInstance, probeWasDisposed));
            }

            _firstExecutionSignal.TrySetResult(true);
        }

        public sealed class RecordedExecution
        {

            public RecordedExecution(object stepInstance, bool probeWasDisposed)
            {
                StepInstance = stepInstance;
                StepInstanceIdentity = RuntimeHelpers.GetHashCode(stepInstance);
                ProbeWasDisposed = probeWasDisposed;
            }

            public object StepInstance { get; }

            public int StepInstanceIdentity { get; }

            public bool ProbeWasDisposed { get; }
        }
    }
}
