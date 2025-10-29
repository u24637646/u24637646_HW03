using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using u24637646_HW03.Models;
using u24637646_HW03.ViewModels;
using System.Threading.Tasks;

namespace u24637646_HW03.Controllers
{
    public class HomeController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();
        public async Task<ActionResult> Index()
        {
            var viewModel = new HomeIndexViewModel 
            { 
                StaffsList = db.staffs.Select(s => new StaffViewModel
                {
                    staff_id = s.staff_id,
                    first_name = s.first_name,
                    last_name = s.last_name,
                    email = s.email,
                    phone = s.phone,
                    active = s.active,
                    store_name = s.stores.store_name,
                    manager_name = s.staffs2.first_name + " " + s.staffs2.last_name
                }).ToList(),


                CustomersList = db.customers.Select(c => new CustomerViewModel
                {
                    customer_id = c.customer_id,
                    first_name = c.first_name,
                    last_name = c.last_name,
                    email = c.email,
                    phone = c.phone,
                    street = c.street,
                    city = c.city,
                    state = c.state,
                    zip_code = c.zip_code
                }).ToList(),

                ProductsList = db.products.Select(p => new ProductViewModel
                {
                    product_id = p.product_id,
                    product_name = p.product_name,
                    model_year = p.model_year,
                    list_price = p.list_price,
                    brand_name = p.brands.brand_name,
                    category_name = p.categories.category_name,                                              
                    TotalStock = p.stocks.Sum(st => (int?)st.quantity) ?? 0
                }).ToList()
            };

            return View(viewModel);
        }

        public ActionResult Reports()
        {
            return View();
        }

        public ActionResult Maintain()
        {

            return View();
        }
    }
}