// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-11 19:50
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-11 19:50
// ***********************************************************************
//  <copyright file="DocumentService.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using PipelineInvokeTest.Models;
using RzR.Extensions.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PipelineInvokeTest.Services
{
    public class DocumentService
    {
        private static List<DocumentItemDto> Documents { get; set; }

        public DocumentService()
            => Documents = new List<DocumentItemDto>();
        
        public async Task<DocumentItemDto> GetAsync(Guid id)
        {
            if (id.IsEmpty())
                await Task.CompletedTask;

            var docIdx = Documents.FindIndex(x => x.Id == id);
            if (docIdx != -1)
            {
                return await Task.FromResult(Documents[docIdx]);
            }

            return null;
        }

        public async Task AddAsync(DocumentItemDto document)
        {
            if (Documents.Any(x => x.Id == document.Id).IsFalse())
                Documents.Add(document);

            await Task.CompletedTask;
        }

        public async Task EditAsync(DocumentItemDto document)
        {
            if (document.IsNull())
                await Task.CompletedTask;

            var doc = Documents.FirstOrDefault(x => x.Id == document.Id);
            if (doc.IsNotNull())
            {
                Documents.Remove(doc);

                doc = document;

                Documents.Add(doc);
            }

            await Task.CompletedTask;
        }

        public async Task ActivateInactivateAsync(Guid id)
        {
            if (id.IsEmpty())
                await Task.CompletedTask;

            var docIdx = Documents.FindIndex(x => x.Id == id);
            if (docIdx != -1)
            {
                var state = Documents[docIdx].IsActive;
                Documents[docIdx].IsActive = state.Negate();
            }

            await Task.CompletedTask;
        }
    }
}

