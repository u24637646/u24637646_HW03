using System;
using System.Collections.Generic;
using System.Data.Entity;
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

        public async Task<ActionResult> Index(int? staffId, string selectedBrand, string selectedCategory)
        {

            // --- 1. Define Base Queries (Staffs, Customers, Products) ---

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

            // --- PRODUCT QUERY SETUP ---
            IQueryable<ProductViewModel> allProductsQuery = db.products.Select(p => new ProductViewModel
            {
                product_id = p.product_id,
                product_name = p.product_name,
                model_year = p.model_year,
                list_price = p.list_price,
                brand_name = p.brands.brand_name,
                category_name = p.categories.category_name,
                TotalStock = p.stocks.Sum(st => (int?)st.quantity) ?? 0
            }).OrderBy(p => p.product_id);

            // Execute the query once to get the FULL list for dropdowns
            var completeProductList = await allProductsQuery.ToListAsync();

            // Pass the COMPLETE, DISTINCT lists for persistent dropdowns
            ViewData["AllBrands"] = completeProductList.Select(p => p.brand_name).Distinct().OrderBy(b => b).ToList();
            ViewData["AllCategories"] = completeProductList.Select(p => p.category_name).Distinct().OrderBy(c => c).ToList();

            // --- 2. Apply Filtering Logic ---

            // Check if ANY filter parameter was provided
            bool isFiltered = !string.IsNullOrEmpty(selectedBrand) ||
                             !string.IsNullOrEmpty(selectedCategory);

            // Start filtering the complete list
            IEnumerable<ProductViewModel> filteredProducts = completeProductList;

            // Apply filters and persist selection in ViewData
            if (!string.IsNullOrEmpty(selectedBrand))
            {
                filteredProducts = filteredProducts.Where(p => p.brand_name == selectedBrand);
                ViewData["SelectedBrand"] = selectedBrand;
            }

            if (!string.IsNullOrEmpty(selectedCategory))
            {
                filteredProducts = filteredProducts.Where(p => p.category_name == selectedCategory);
                ViewData["SelectedCategory"] = selectedCategory;
            }

            // Pass the filter state to the view (used for showing the status message, NOT for display logic)
            ViewData["IsFiltered"] = isFiltered;

            // --- 3. Sales Queries (Unchanged) ---
            var staffSalesQuery = db.orders
                .Where(o => o.staff_id != null)
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .Join(db.products, temp => temp.oi.product_id, p => p.product_id, (temp, p) => new { temp.o, temp.oi, p })
                .OrderByDescending(x => x.o.order_date)
                .Select(x => new StaffSaleViewModel
                {
                    staff_id = x.o.staff_id,
                    order_date = x.o.order_date,
                    product_name = x.p.product_name,
                    list_price = x.oi.list_price,
                    quantity = x.oi.quantity,
                    total_sale_price = x.oi.list_price * x.oi.quantity * (1 - (decimal)x.oi.discount)
                });

            var customerPurchasesQuery = db.orders
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .Join(db.products, temp => temp.oi.product_id, p => p.product_id, (temp, p) => new CustomerPurchaseViewModel
                {
                    customer_id = temp.o.customer_id.Value,
                    product_name = p.product_name,
                    quantity = temp.oi.quantity
                })
                .OrderByDescending(x => x.customer_id)
                .ThenByDescending(x => x.product_name)
                .Select(x => x);


            // 4. Execute Queries and Build ViewModel
            var viewModel = new HomeIndexViewModel
            {
                StaffsList = await staffQuery.ToListAsync(),
                CustomersList = await customersQuery.ToListAsync(),
                ProductsList = filteredProducts.ToList(), // Send the full filtered list

                StaffSalesList = await staffSalesQuery.ToListAsync(),
                CustomerPurchasesList = await customerPurchasesQuery.ToListAsync(),

                ShowStaffCreateModal = false,
                ModalStaff = null
            };

            // 5. Non-AJAX Modal Preparation (Unchanged)
            if (staffId.HasValue)
            {
                var modalStaff = viewModel.StaffsList.FirstOrDefault(s => s.staff_id == staffId.Value);
                if (modalStaff != null)
                {
                    viewModel.ModalStaff = new StaffViewModel();
                    viewModel.ShowStaffCreateModal = true;
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