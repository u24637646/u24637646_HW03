using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using u24637646_HW03.Models;

namespace u24637646_HW03.ViewModels
{
    public class HomeIndexViewModel
    {
        //List to display all entities 
        public IEnumerable<StaffViewModel> StaffsList { get; set; }
        public IEnumerable<CustomerViewModel> CustomersList { get; set; }
        public IEnumerable<ProductViewModel> ProductsList { get; set; }

        // --- NEW PROPERTY FOR STAFF SALES ---
        public IEnumerable<StaffSaleViewModel> StaffSalesList { get; set; }
        public List<CustomerPurchaseViewModel> CustomerPurchasesList { get; set; }

        public StaffViewModel ModalStaff { get; set; }
        public bool ShowStaffCreateModal { get; set; } 
    }
}