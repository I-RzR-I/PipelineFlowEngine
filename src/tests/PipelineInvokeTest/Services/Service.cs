#region U S A G E S

using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Services
{
    public class Service
    {
        public async Task<bool> ValidateInTrueDataAsync() => await Task.FromResult(true);
    }
}
