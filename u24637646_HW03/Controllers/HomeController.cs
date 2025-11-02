using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using u24637646_HW03.Models;
using u24637646_HW03.ViewModels;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Newtonsoft.Json;

// --- ARCHIVE MANAGEMENT CLASSES ---
// Placeholder class for Archiving (simulate database table/storage)
public class ArchivedReport
{
    public string Filename { get; set; }
    public string Filetype { get; set; }
    public DateTime DateSaved { get; set; }
    public string Description { get; set; } // Stores HTML content from TinyMCE
}

// Static list to simulate persistent storage for archived reports
public static class ReportArchive
{
    public static List<ArchivedReport> Reports = new List<ArchivedReport>
    {
        // Example initial data
        new ArchivedReport { Filename = "Initial_Trend_Report.pdf", Filetype = "PDF", DateSaved = DateTime.Now.AddDays(-5), Description = "<em>Monthly Order Trend</em> chart saved at startup." },
        new ArchivedReport { Filename = "Q1_Sales_Distribution.pdf", Filetype = "PDF", DateSaved = DateTime.Now.AddDays(-2), Description = "Sales data by store for <strong>Q1</strong>." }
    };
}


namespace u24637646_HW03.Controllers
{
    public class HomeController : Controller
    {
        // NOTE: Ensure your BikeStoresEntities context is correctly set up
        private BikeStoresEntities db = new BikeStoresEntities();

        // --- EXISTING INDEX ACTION (Unchanged) ---
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
            bool isFiltered = !string.IsNullOrEmpty(selectedBrand) || !string.IsNullOrEmpty(selectedCategory);

            IEnumerable<ProductViewModel> filteredProducts = completeProductList;

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
                ProductsList = filteredProducts.ToList(),
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

        // --- REPORTS ACTION ---
        public async Task<ActionResult> Reports()
        {
            var jsonSetting = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

            // --- 1. Doughnut Chart Data Preparation (Sales by Store) ---
            var rawStoreSales = await db.order_items
                .Include(oi => oi.orders.stores)
                .Where(oi => oi.orders != null && oi.orders.stores != null)
                .Select(oi => new
                {
                    Store = oi.orders.stores.store_name,
                    oi.list_price,
                    oi.quantity,
                    oi.discount
                })
                .Where(raw => raw.Store != null)
                .ToListAsync();

            var storeSales = rawStoreSales
                .GroupBy(raw => raw.Store)
                .Select(g => new
                {
                    Store = g.Key,
                    TotalSales = g.Sum(oi => oi.list_price * oi.quantity * (1 - (decimal)oi.discount))
                })
                .Where(c => c.TotalSales > 0)
                .ToList();

            ViewBag.DoughnutLabels = JsonConvert.SerializeObject(storeSales.Select(c => c.Store).ToList(), jsonSetting);
            ViewBag.DoughnutData = JsonConvert.SerializeObject(storeSales.Select(c => c.TotalSales).ToList(), jsonSetting);


            // --- 2. Line Chart Data Preparation (Monthly Order Trend) ---

            var rawMonthlyOrders = await db.orders
                .GroupBy(o => new { o.order_date.Year, o.order_date.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    OrderCount = g.Count()
                })
                .ToListAsync();

            var monthlyOrders = rawMonthlyOrders
                .Select(item => new
                {
                    OrderDate = new DateTime(item.Year, item.Month, 1),
                    item.OrderCount
                })
                .ToList();

            ViewBag.LineLabels = JsonConvert.SerializeObject(monthlyOrders.Select(m => m.OrderDate.ToString("MMM yyyy")).ToList(), jsonSetting);
            ViewBag.LineData = JsonConvert.SerializeObject(monthlyOrders.Select(m => m.OrderCount).ToList(), jsonSetting);

            // Add the list of archived reports to the ViewBag
            ViewBag.ArchivedReports = ReportArchive.Reports.OrderByDescending(r => r.DateSaved).ToList();

            return View();
        }

        // --- ARCHIVE ACTIONS ---

        // NEW: Action to handle saving an individual chart report (PDF ONLY) with Rich Text Description
        [HttpPost]
        [ValidateInput(false)] // REQUIRED to accept HTML content from TinyMCE
        public ActionResult SaveChartReport(string Filename, string Filetype, string ChartName, string Description)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(Filename))
            {
                TempData["Message"] = "Error: Filename is required.";
                return RedirectToAction("Reports");
            }

            // Sanitize filename and construct full name (Filetype is forced to PDF)
            string safeFilename = Path.GetInvalidFileNameChars().Aggregate(Filename, (current, c) => current.Replace(c.ToString(), "_"));
            string fullFilename = $"{safeFilename}_{ChartName.Replace(" ", "")}.pdf";

            var newReport = new ArchivedReport
            {
                Filename = fullFilename,
                Filetype = "PDF", // Fixed requirement
                DateSaved = DateTime.Now,
                // Use the description submitted from the rich text box
                Description = Description ?? $"**{ChartName}** chart saved in PDF format."
            };

            // Add to the static archive list
            ReportArchive.Reports.Add(newReport);
            TempData["Message"] = $"Chart Report '{newReport.Filename}' saved successfully!";

            return RedirectToAction("Reports");
        }

        // UPDATED: Action to handle updating the description via the modal (Rich Text Box)
        [HttpPost]
        [ValidateInput(false)] // REQUIRED to accept HTML content from TinyMCE
        public ActionResult UpdateReportDescription(string filename, string Description)
        {
            var reportToUpdate = ReportArchive.Reports.FirstOrDefault(r => r.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));

            if (reportToUpdate != null)
            {
                reportToUpdate.Description = Description;
                TempData["Message"] = $"Description for '{filename}' updated successfully.";
            }
            else
            {
                TempData["Message"] = $"Error: Report '{filename}' not found for update.";
            }

            return RedirectToAction("Reports");
        }

        // Action to handle downloading a report (PDF only expected)
        public FileResult DownloadReport(string filename, string filetype)
        {
            if (!filetype.Equals("PDF", StringComparison.OrdinalIgnoreCase))
            {
                return File(Encoding.UTF8.GetBytes("File type not supported for download."), "text/plain", "Error.txt");
            }

            // Simulated content - Replace this section with actual PDF generation code
            string content = $"Simulated PDF content for: {filename}\nGenerated on: {DateTime.Now}";
            byte[] fileBytes = Encoding.UTF8.GetBytes(content);

            string contentType = "application/pdf";

            return File(fileBytes, contentType, filename);
        }

        // Action to handle deleting a report
        [HttpPost]
        public ActionResult DeleteReport(string filename)
        {
            var reportToDelete = ReportArchive.Reports.FirstOrDefault(r => r.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));

            if (reportToDelete != null)
            {
                ReportArchive.Reports.Remove(reportToDelete);
                TempData["Message"] = $"Report '{filename}' deleted successfully.";
            }
            else
            {
                TempData["Message"] = $"Error: Report '{filename}' not found.";
            }

            return RedirectToAction("Reports");
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