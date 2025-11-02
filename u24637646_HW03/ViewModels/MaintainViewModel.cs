using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace u24637646_HW03.ViewModels
{
    // A clean, dedicated ViewModel for the Maintain Page
    public class MaintainViewModel
    {
        // List of Staffs to display, including necessary FK IDs for CRUD
        public List<StaffViewModel> StaffsList { get; set; }

        // List of Customers to display and edit
        public List<CustomerViewModel> CustomersList { get; set; }

        // List of Products to display, including necessary FK IDs for CRUD
        public List<ProductViewModel> ProductsList { get; set; }
    }
}