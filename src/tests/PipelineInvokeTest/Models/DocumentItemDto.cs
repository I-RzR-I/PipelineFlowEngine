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
