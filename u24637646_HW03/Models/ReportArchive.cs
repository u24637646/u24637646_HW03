using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace u24637646_HW03.Models
{
    public static class ReportArchive
    {
        public static List<u24637646_HW03.Models.ArchivedReport> Reports = new List<u24637646_HW03.Models.ArchivedReport>
    {
        // Note: I added a ChartType field to match your model definition snippet
        new u24637646_HW03.Models.ArchivedReport { Filename = "Initial_Trend_Report.pdf", Filetype = "PDF", ChartType = "Line Chart", DateSaved = DateTime.Now.AddDays(-5), Description = "<em>Monthly Order Trend</em> chart saved at startup." },
        new u24637646_HW03.Models.ArchivedReport { Filename = "Q1_Sales_Distribution.pdf", Filetype = "PDF", ChartType = "Doughnut Chart", DateSaved = DateTime.Now.AddDays(-2), Description = "Sales data by store for <strong>Q1</strong>." }
    };
    }
}