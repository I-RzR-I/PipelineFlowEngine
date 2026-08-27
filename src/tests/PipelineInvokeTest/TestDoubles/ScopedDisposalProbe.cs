#region U S A G E S

using System;
using System.Threading;

#endregion

namespace PipelineInvokeTest.TestDoubles
{

    public sealed class ScopedDisposalProbe : IDisposable
    {
        private int _isDisposed;

        public bool IsDisposed => Volatile.Read(ref _isDisposed) != 0;

        public void Dispose() => Interlocked.Exchange(ref _isDisposed, 1);
    }
}
