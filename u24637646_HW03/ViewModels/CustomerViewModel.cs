using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace u24637646_HW03.ViewModels
{
    public class CustomerViewModel
    {

        [DisplayName("Customer ID")]
        public int customer_id { get; set; }

        [DisplayName("First Name")]
        public string first_name { get; set; }

        [DisplayName("Last Name")]
        public string last_name { get; set; }

        [DisplayName("Phone Number")]
        public string phone { get; set; }

        [DisplayName("Email Address")]
        public string email { get; set; }

        [DisplayName("Street")]
        public string street { get; set; }

        [DisplayName("City")]
        public string city { get; set; }

        [DisplayName("State")]
        public string state { get; set; }

        [DisplayName("Zip Code")]
        public string zip_code { get; set; }
    }
}