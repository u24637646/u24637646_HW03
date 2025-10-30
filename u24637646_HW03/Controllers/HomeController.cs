using System;
using System.Collections.Generic;
using System.Data.Entity; // Necessary for .Include() and async methods in EF 6
using System.Linq;
using System.Web.Mvc;
using u24637646_HW03.Models;
using u24637646_HW03.ViewModels;
using System.Threading.Tasks;

namespace u24637646_HW03.Controllers
{
    public class HomeController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // ⭐ MODIFIED: Accepts an optional staffId parameter
        public async Task<ActionResult> Index(int? staffId)
        {
            // 1. Define Queries (Asynchronously)
            var staffQuery = db.staffs.Include(s => s.stores).Include(s => s.staffs2)
                .Select(s => new StaffViewModel
                {
                    staff_id = s.staff_id,
                    first_name = s.first_name,
                    last_name = s.last_name,
                    email = s.email,
                    phone = s.phone,
                    active = s.active,
                    store_name = s.stores.store_name,
                    manager_name = s.manager_id == null ? "No Manager" : s.staffs2.first_name + " " + s.staffs2.last_name
                });

            var customersQuery = db.customers.Select(c => new CustomerViewModel
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
            });

            var productsQuery = db.products.Select(p => new ProductViewModel
            {
                product_id = p.product_id,
                product_name = p.product_name,
                model_year = p.model_year,
                list_price = p.list_price,
                brand_name = p.brands.brand_name,
                category_name = p.categories.category_name,
                TotalStock = p.stocks.Sum(st => (int?)st.quantity) ?? 0
            });

            // ⭐ Staff Sales Query Definition
            var staffSalesQuery = db.orders
                .Where(o => o.staff_id != null) // Only include orders assigned to a staff member
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .Join(db.products, temp => temp.oi.product_id, p => p.product_id, (temp, p) => new { temp.o, temp.oi, p })
                .OrderByDescending(x => x.o.order_date)
                // Select and project the data into the StaffSaleViewModel
                .Select(x => new StaffSaleViewModel
                {
                    staff_id = x.o.staff_id, // staff_id from the orders table
                    order_date = x.o.order_date,
                    product_name = x.p.product_name,
                    list_price = x.oi.list_price,
                    quantity = x.oi.quantity,
                    // Calculate the total sale price for this item
                    total_sale_price = x.oi.list_price * x.oi.quantity * (1 - (decimal)x.oi.discount)
                });

            // ⭐ NEW: Customer Purchases Query Definition
            var customerPurchasesQuery = db.orders
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .Join(db.products, temp => temp.oi.product_id, p => p.product_id, (temp, p) => new CustomerPurchaseViewModel
                {
                    customer_id = temp.o.customer_id.Value,
                    product_name = p.product_name,
                    quantity = temp.oi.quantity
                })
                // Sorting helps ensure the query is deterministic, though the Razor view does the final filtering/taking
                .OrderByDescending(x => x.customer_id)
                .ThenByDescending(x => x.product_name)
                .Select(x => x);


            // 2. Execute Queries Asynchronously and Build Base ViewModel
            var viewModel = new HomeIndexViewModel
            {
                StaffsList = await staffQuery.ToListAsync(),
                CustomersList = await customersQuery.ToListAsync(),
                ProductsList = await productsQuery.ToListAsync(),

                // ⭐ Execute Staff Sales Query
                StaffSalesList = await staffSalesQuery.ToListAsync(),

                // ⭐ NEW: Execute Customer Purchases Query
                CustomerPurchasesList = await customerPurchasesQuery.ToListAsync(),

                ShowStaffModal = false,
                ModalStaff = null
            };

            // 3. ⭐ Non-AJAX Modal Preparation: If a staffId is present, prepare the modal data
            if (staffId.HasValue)
            {
                // Retrieve the specific staff member from the list already loaded
                var modalStaff = viewModel.StaffsList.FirstOrDefault(s => s.staff_id == staffId.Value);

                if (modalStaff != null)
                {
                    viewModel.ModalStaff = modalStaff;
                    viewModel.ShowStaffModal = true; // Sets the flag for JavaScript
                }
            }

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
