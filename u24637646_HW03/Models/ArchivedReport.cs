using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace u24637646_HW03.Models
{
    public class ArchivedReport
    {
        public string Filename { get; set; }
        public string Filetype { get; set; }
        public string ChartType { get; set; }
        public DateTime DateSaved { get; set; }
        public string Description { get; set; }
    }
}