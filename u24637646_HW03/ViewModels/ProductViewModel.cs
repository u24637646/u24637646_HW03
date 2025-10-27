using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace u24637646_HW03.ViewModels
{
    public class ProductViewModel
    {
        //Properties from the Products table
        public int product_id { get; set; }
        public string product_name { get; set; }
        public short model_year { get; set; }
        public decimal list_price { get; set; }

        //Mapped properties that replace the foreign keys
        public string brand_name { get; set; }
        public string category_name { get; set; }

        //Additional properties
        public int TotalStock { get; set; }
    }
}