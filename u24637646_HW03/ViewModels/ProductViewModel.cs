using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace u24637646_HW03.ViewModels
{
    public class ProductViewModel
    {
        //Properties from the Products table
        [DisplayName("Product ID")]
        public int product_id { get; set; }

        [DisplayName("Product Name")]
        public string product_name { get; set; }

        [DisplayName("Model Year")]
        public short model_year { get; set; }

        [DisplayName("List Price")]
        public decimal list_price { get; set; }

        // --- NEW FK IDs ADDED FOR UPDATE/EDIT OPERATIONS ---
        [DisplayName("Brand ID")]
        public int brand_id { get; set; }

        [DisplayName("Category ID")]
        public int category_id { get; set; }
        // ---------------------------------------------------

        //Mapped properties that replace the foreign keys (for display)
        [DisplayName("Brand Name")]
        public string brand_name { get; set; }

        [DisplayName("Category Name")]
        public string category_name { get; set; }

        //Additional properties
        [DisplayName("Total Stock")]
        public int TotalStock { get; set; }

        public int ListIndex { get; set; }
    }
}