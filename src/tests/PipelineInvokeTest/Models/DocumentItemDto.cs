// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-09 17:45
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-09 17:45
// ***********************************************************************
//  <copyright file="DocumentItemDto.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using PipelineInvokeTest.Enums;
using System;

namespace PipelineInvokeTest.Models
{
    public class DocumentItemDto
    {
        public Guid Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid CreatedById { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public Guid? ModifiedById { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public Guid? ApprovedById { get; set; }

        public DocStateType State { get; set; }

        public DocStatusType Status { get; set; }

        public bool IsActive { get; set; }
    }
}

