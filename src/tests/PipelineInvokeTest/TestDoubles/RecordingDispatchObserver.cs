#region U S A G E S

using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.TestDoubles
{

    public sealed class RecordingDispatchObserver : IPipelineDispatchObserver<PersonDto>
    {
        private readonly object _syncRoot = new object();
        private readonly List<PipelineDispatchOutcome> _outcomes = new List<PipelineDispatchOutcome>();

        private readonly TaskCompletionSource<PipelineDispatchOutcome> _firstOutcomeSignal =
            new TaskCompletionSource<PipelineDispatchOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<PipelineDispatchOutcome> Completed => _firstOutcomeSignal.Task;

        public int OutcomeCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _outcomes.Count;
                }
            }
        }

        public PipelineDispatchOutcome[] Snapshot()
        {
            lock (_syncRoot)
            {
                return _outcomes.ToArray();
            }
        }

        public void OnDispatchCompleted(PipelineDispatchOutcome outcome)
        {
            lock (_syncRoot)
            {
                _outcomes.Add(outcome);
            }

            _firstOutcomeSignal.TrySetResult(outcome);
        }
    }
}
