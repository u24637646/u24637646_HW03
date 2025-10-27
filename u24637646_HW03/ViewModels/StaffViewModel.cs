using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace u24637646_HW03.ViewModels
{
    public class StaffViewModel
    {
        //Properties from the staffs table
        public int staff_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public byte active { get; set; }

        //Mapped properties that replace the foreign keys
        public string store_name { get; set; }
        public string manager_name { get; set; }
    }
}