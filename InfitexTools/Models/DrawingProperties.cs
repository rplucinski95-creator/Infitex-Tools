using System;

namespace InfitexTools.Models
{
    public class DrawingProperties
    {
        public string Index { get; set; }
        public string Revision { get; set; }

        public string Description_EN { get; set; }
        public string Description_PL { get; set; }

        public string ProjectNumber { get; set; }
        public string ProjectName { get; set; }

        public string ChangeStatus { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public string CheckedBy { get; set; }
        public DateTime CheckedDate { get; set; }

        public string ApprovedBy { get; set; }
        public DateTime ApprovedDate { get; set; }

        public string RevisionComment { get; set; }
        public DateTime RevisionDate { get; set; }
    }
}