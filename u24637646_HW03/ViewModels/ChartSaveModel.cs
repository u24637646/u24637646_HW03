using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace u24637646_HW03.ViewModels
{
    public class ChartSaveModel
    {
        // This MUST be named 'File' to match the key used in the client-side FormData.
        public HttpPostedFileBase File { get; set; }

        public string ChartId { get; set; }
        public string Filename { get; set; }
        public string Filetype { get; set; }
        public string Description { get; set; } // Rich text HTML content
    }
}