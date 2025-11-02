using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel; // Added for DisplayName

namespace u24637646_HW03.ViewModels
{
    public class StaffViewModel
    {
        // Properties from the staffs table
        public int staff_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }

        [DisplayName("Active")]
        public byte active { get; set; }

        // --- NEW FK IDs ADDED FOR UPDATE/EDIT OPERATIONS ---
        [DisplayName("Store")]
        public int store_id { get; set; }

        [DisplayName("Manager")]
        public int? manager_id { get; set; }
        // ---------------------------------------------------

        // Mapped properties that replace the foreign keys (for display)
        public string store_name { get; set; }
        public string manager_name { get; set; }

        // This SelectList property is often better managed via ViewBag/Controller for Maintain page
        public SelectList AvailableStores { get; set; }

        public int ListIndex { get; set; }
    }
}