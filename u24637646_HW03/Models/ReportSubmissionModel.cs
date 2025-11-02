using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace u24637646_HW03.Models
{
    public class ReportSubmissionModel
    {
        public string Filename { get; set; }
        public string ChartName { get; set; }
        public string Filetype { get; set; }
        public string Description { get; set; }
        public string PdfBase64Data { get; set; } // The new field from the client
    }
}