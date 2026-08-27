using PipelineInvokeTest.Models;
using RzR.Extensions.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineInvokeTest.Services
{
    public class DocumentService
    {
        private readonly List<DocumentItemDto> _documents;

        public DocumentService()
            => _documents = new List<DocumentItemDto>();

        public async Task<DocumentItemDto> GetAsync(Guid id)
        {
            if (id.IsEmpty())
                await Task.CompletedTask;

            var docIdx = _documents.FindIndex(x => x.Id == id);
            if (docIdx != -1)
            {
                return await Task.FromResult(_documents[docIdx]);
            }

            return null;
        }

        public async Task<DocumentItemDto> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await GetAsync(id);
        }

        public async Task AddAsync(DocumentItemDto document)
        {
            if (_documents.Any(x => x.Id == document.Id).IsFalse())
                _documents.Add(document);

            await Task.CompletedTask;
        }

        public async Task EditAsync(DocumentItemDto document)
        {
            if (document.IsNull())
                await Task.CompletedTask;

            var doc = _documents.FirstOrDefault(x => x.Id == document.Id);
            if (doc.IsNotNull())
            {
                _documents.Remove(doc);

                doc = document;

                _documents.Add(doc);
            }

            await Task.CompletedTask;
        }

        public async Task ActivateInactivateAsync(Guid id)
        {
            if (id.IsEmpty())
                await Task.CompletedTask;

            var docIdx = _documents.FindIndex(x => x.Id == id);
            if (docIdx != -1)
            {
                var state = _documents[docIdx].IsActive;
                _documents[docIdx].IsActive = state.Negate();
            }

            await Task.CompletedTask;
        }
    }
}
