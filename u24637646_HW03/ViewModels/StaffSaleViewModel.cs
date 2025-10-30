using System;
using System.ComponentModel.DataAnnotations;

namespace u24637646_HW03.ViewModels
{
    public class StaffSaleViewModel
    {
        // Foreign Key to link the sale back to the staff member
        public int staff_id { get; set; }

        // Properties from order_items and orders tables
        public DateTime order_date { get; set; }

        // Properties from products table
        public string product_name { get; set; }
        public decimal list_price { get; set; } // Could be the price at time of sale from order_items
        public int quantity { get; set; } // Quantity sold in this specific order item

        // Optional: The specific price/discount combination for the order_item
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal total_sale_price { get; set; }
    }
}