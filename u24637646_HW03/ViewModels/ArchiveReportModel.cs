using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace u24637646_HW03.ViewModels
{
    public class ArchiveReportModel
    {
        public int Id { get; set; }
        public string ChartId { get; set; }
        public string Filename { get; set; }
        public string Filetype { get; set; } // PDF or PNG
        public string Description { get; set; } // Rich text HTML
        public DateTime DateSaved { get; set; }
        public string FilePath { get; set; } // Path to the physical file on the server
    }
}