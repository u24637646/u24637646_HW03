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
using System.Net;

namespace u24637646_HW03.Controllers
{
    public class HomeController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();
        private const string ArchiveFolder = "~/ArchivedReports/";

        // Helper method to load reports from the file system and sync with metadata
        private List<ArchivedReport> LoadArchivedReports(string serverPath)
        {
            var archivedList = new List<ArchivedReport>();

            if (Directory.Exists(serverPath))
            {
                // Get all PDF files in the archive directory
                var pdfFiles = Directory.EnumerateFiles(serverPath, "*.pdf");

                foreach (var filePath in pdfFiles)
                {
                    string filename = Path.GetFileName(filePath);

                    // 🚨 FIX: Explicitly use System.IO.File to resolve the conflict
                    DateTime lastModified = System.IO.File.GetLastWriteTime(filePath);

                    // Check if we have existing metadata for this file
                    var existingMetadata = ReportArchive.Reports
                        .FirstOrDefault(r => r.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));

                    // Use existing metadata or create default entry
                    var report = existingMetadata ?? new ArchivedReport
                    {
                        Filename = filename,
                        Filetype = "PDF",
                        DateSaved = lastModified,
                        ChartType = filename.Contains("Monthly_Order_Trend") ? "Monthly Order Trend" :
                                   (filename.Contains("Sales_Revenue_Distribution") ? "Sales Revenue Distribution" : "Custom Chart"),
                        Description = $"Chart saved on {lastModified:yyyy-MM-dd HH:mm}"
                    };

                    // Update date saved if no metadata exists
                    if (existingMetadata == null)
                    {
                        report.DateSaved = lastModified;
                        ReportArchive.Reports.Add(report);
                    }

                    archivedList.Add(report);
                }

                // Clean up metadata for files that no longer exist
                var filesInDir = new HashSet<string>(pdfFiles.Select(Path.GetFileName), StringComparer.OrdinalIgnoreCase);
                ReportArchive.Reports.RemoveAll(r => !filesInDir.Contains(r.Filename));
            }

            // Return files ordered by most recent first
            return archivedList.OrderByDescending(r => r.DateSaved).ToList();
        }

        // Display the reports dashboard with charts and archives
        public async Task<ActionResult> Reports()
        {
            var jsonSetting = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

            // Prepare doughnut chart data showing sales by store
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

            // Prepare line chart data showing monthly order trends
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

            // Load archived reports from disk
            string serverPath = Server.MapPath(ArchiveFolder);
            ViewBag.ArchivedReports = LoadArchivedReports(serverPath);

            return View();
        }

        // Display the main dashboard with staff, customers, and products
        public async Task<ActionResult> Index(string selectedBrand, string selectedCategory)
        {
            // Display any messages from redirects (edit/delete operations)
            if (TempData["Message"] != null)
            {
                ViewBag.Message = TempData["Message"].ToString();
                ViewBag.MessageClass = TempData["MessageClass"]?.ToString() ?? "alert-info";
            }

            // Build product query with optional filtering
            IQueryable<products> productsQuery = db.products;

            if (!string.IsNullOrEmpty(selectedBrand) && selectedBrand != "All Brands")
            {
                productsQuery = productsQuery.Where(p => p.brands.brand_name == selectedBrand);
            }

            if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "All Categories")
            {
                productsQuery = productsQuery.Where(p => p.categories.category_name == selectedCategory);
            }

            // Populate filter dropdowns
            ViewData["AllBrands"] = await db.brands.Select(b => b.brand_name).Distinct().OrderBy(n => n).ToListAsync();
            ViewData["AllCategories"] = await db.categories.Select(c => c.category_name).Distinct().OrderBy(n => n).ToListAsync();
            ViewData["SelectedBrand"] = selectedBrand;
            ViewData["SelectedCategory"] = selectedCategory;

            var viewModel = new HomeIndexViewModel();

            // Fetch and prepare product data
            var productList = await productsQuery
                .Select(p => new ProductViewModel
                {
                    product_id = p.product_id,
                    product_name = p.product_name,
                    model_year = p.model_year,
                    list_price = p.list_price,
                    brand_name = p.brands.brand_name,
                    category_name = p.categories.category_name,
                    TotalStock = p.stocks.Sum(s => (int?)s.quantity) ?? 0
                })
                .OrderBy(p => p.product_id)
                .ToListAsync();

            // Assign list indices for navigation
            for (int i = 0; i < productList.Count; i++)
            {
                productList[i].ListIndex = i + 1;
            }
            viewModel.ProductsList = productList;

            // Fetch and prepare staff data
            var staffList = await db.staffs
                .Include(s => s.stores).Include(s => s.staffs2)
                .Select(s => new StaffViewModel
                {
                    staff_id = s.staff_id,
                    first_name = s.first_name,
                    last_name = s.last_name,
                    email = s.email,
                    phone = s.phone,
                    active = s.active,
                    store_name = s.stores.store_name,
                    manager_name = s.manager_id == null ? "No Manager" : s.staffs2.first_name + " " + s.staffs2.last_name,
                })
                .OrderBy(s => s.staff_id)
                .ToListAsync();

            for (int i = 0; i < staffList.Count; i++)
            {
                staffList[i].ListIndex = i + 1;
            }
            viewModel.StaffsList = staffList;

            // Fetch staff sales history for display
            // FIX for CS0472 (redundant null check removed)
            viewModel.StaffSalesList = await db.order_items
                .Include(oi => oi.orders).Include(oi => oi.products)
                .OrderByDescending(oi => oi.orders.order_date)
                .Select(oi => new StaffSaleViewModel
                {
                    staff_id = oi.orders.staff_id,
                    product_name = oi.products.product_name
                })
                .ToListAsync();

            // Fetch and prepare customer data
            var customerList = await db.customers
                .Select(c => new CustomerViewModel
                {
                    customer_id = c.customer_id,
                    first_name = c.first_name,
                    last_name = c.last_name,
                    phone = (c.phone == null || c.phone == "") ? "-" : c.phone,
                    email = c.email,
                    street = c.street,
                    city = c.city,
                    state = c.state.Length > 2 ? c.state.Substring(0, 2).ToUpper() : c.state,
                    zip_code = c.zip_code,
                })
                .OrderBy(c => c.customer_id)
                .ToListAsync();

            for (int i = 0; i < customerList.Count; i++)
            {
                customerList[i].ListIndex = i + 1;
            }
            viewModel.CustomersList = customerList;

            // Fetch customer purchase history
            // FIX for CS0472 (redundant null check removed)
            viewModel.CustomerPurchasesList = await db.order_items
                .Include(oi => oi.orders).Include(oi => oi.products)
                .OrderByDescending(oi => oi.orders.order_date)
                .Select(oi => new CustomerPurchaseViewModel
                {
                    customer_id = oi.orders.customer_id.Value,
                    product_name = oi.products.product_name,
                    quantity = oi.quantity
                })
                .ToListAsync();

            return View(viewModel);
        }

        // Save a chart report as PDF to the archive
        [HttpPost]
        [ValidateInput(false)] // Allow HTML content from TinyMCE
        public ActionResult SaveChartReport(ReportSubmissionModel model)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(model.PdfBase64Data))
            {
                TempData["Message"] = "Error: PDF data is missing. Please regenerate the chart and try again.";
                TempData["MessageClass"] = "alert-danger";
                return RedirectToAction("Reports");
            }

            if (string.IsNullOrWhiteSpace(model.Filename))
            {
                TempData["Message"] = "Error: Filename is required. Please provide a valid filename.";
                TempData["MessageClass"] = "alert-danger";
                return RedirectToAction("Reports");
            }

            if (string.IsNullOrWhiteSpace(model.ChartName))
            {
                TempData["Message"] = "Error: Chart name is missing.";
                TempData["MessageClass"] = "alert-danger";
                return RedirectToAction("Reports");
            }

            try
            {
                // Sanitize filename by removing invalid characters
                string baseFilename = Path.GetInvalidFileNameChars()
                    .Aggregate(model.Filename, (current, c) => current.Replace(c.ToString(), "_"));

                // Remove existing .pdf extension to avoid duplication
                if (baseFilename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    baseFilename = baseFilename.Substring(0, baseFilename.Length - 4);
                }

                // Create standardized filename
                string fullFilename = $"{baseFilename}_{model.ChartName.Replace(" ", "_")}.pdf";

                string serverPath = Server.MapPath(ArchiveFolder);
                string fullPath = Path.Combine(serverPath, fullFilename);

                // Ensure archive directory exists
                if (!Directory.Exists(serverPath))
                {
                    Directory.CreateDirectory(serverPath);
                }

                // Clean Base64 string (remove data URI prefix and whitespace)
                string cleanBase64 = model.PdfBase64Data
                    .Replace("data:application/pdf;base64,", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace(" ", "");

                // Validate Base64 format
                if (cleanBase64.Length % 4 != 0)
                {
                    TempData["Message"] = "Error: Invalid PDF data format. Please try again.";
                    TempData["MessageClass"] = "alert-danger";
                    return RedirectToAction("Reports");
                }

                // Convert Base64 to bytes and save
                byte[] pdfBytes = Convert.FromBase64String(cleanBase64);

                if (pdfBytes.Length == 0)
                {
                    TempData["Message"] = "Error: PDF data is empty. Please regenerate the chart.";
                    TempData["MessageClass"] = "alert-danger";
                    return RedirectToAction("Reports");
                }

                System.IO.File.WriteAllBytes(fullPath, pdfBytes);

                // Update or create report metadata
                var existingReport = ReportArchive.Reports
                    .FirstOrDefault(r => r.Filename.Equals(fullFilename, StringComparison.OrdinalIgnoreCase));

                if (existingReport != null)
                {
                    // Update existing metadata
                    existingReport.DateSaved = DateTime.Now;
                    existingReport.Description = string.IsNullOrWhiteSpace(model.Description)
                        ? $"**{model.ChartName}** chart saved on {DateTime.Now:yyyy-MM-dd HH:mm}"
                        : model.Description;
                    existingReport.ChartType = model.ChartName;
                }
                else
                {
                    // Create new metadata entry
                    var newReport = new ArchivedReport
                    {
                        Filename = fullFilename,
                        Filetype = "PDF",
                        ChartType = model.ChartName,
                        DateSaved = DateTime.Now,
                        Description = string.IsNullOrWhiteSpace(model.Description)
                            ? $"**{model.ChartName}** chart saved on {DateTime.Now:yyyy-MM-dd HH:mm}"
                            : model.Description
                    };
                    ReportArchive.Reports.Add(newReport);
                }

                TempData["Message"] = $"✓ Chart report '{fullFilename}' successfully archived! ({pdfBytes.Length:N0} bytes)";
                TempData["MessageClass"] = "alert-success";
            }
            catch (FormatException ex)
            {
                TempData["Message"] = $"Error: Invalid PDF data format. {ex.Message}";
                TempData["MessageClass"] = "alert-danger";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error saving report: {ex.Message}";
                TempData["MessageClass"] = "alert-danger";
            }

            return RedirectToAction("Reports");
        }

        // Update report description via modal
        [HttpPost]
        [ValidateInput(false)] // Allow HTML content from TinyMCE
        public ActionResult UpdateReportDescription(string filename, string Description)
        {
            var reportToUpdate = ReportArchive.Reports
                .FirstOrDefault(r => r.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));

            if (reportToUpdate != null)
            {
                reportToUpdate.Description = Description;
                TempData["Message"] = $"Description for '{filename}' updated successfully.";
                TempData["MessageClass"] = "alert-success";
            }
            else
            {
                TempData["Message"] = $"Error: Report '{filename}' not found.";
                TempData["MessageClass"] = "alert-danger";
            }

            return RedirectToAction("Reports");
        }

        // Download an archived report
        public FileResult DownloadReport(string filename, string filetype)
        {
            string fullFilename = Path.GetFileName(filename);
            string fullPath = Path.Combine(Server.MapPath(ArchiveFolder), fullFilename);

            if (!filetype.Equals("PDF", StringComparison.OrdinalIgnoreCase))
            {
                // Returning a FileResult with a text file containing the error
                return base.File(Encoding.UTF8.GetBytes("File type not supported for download."), "text/plain", "Error.txt");
            }

            if (System.IO.File.Exists(fullPath))
            {
                // Returning a FileResult with the actual file
                return base.File(fullPath, "application/pdf", fullFilename);
            }
            else
            {
                string errorContent = $"Error: PDF file '{fullFilename}' not found on the server at path: {fullPath}";
                // Returning a FileResult with a text file containing the error
                return base.File(Encoding.UTF8.GetBytes(errorContent), "text/plain", "Download_Error.txt");
            }
        }

        // Delete a report (both file and metadata)
        [HttpPost]
        public ActionResult DeleteReport(string filename)
        {
            var reportToDelete = ReportArchive.Reports
                .FirstOrDefault(r => r.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));
            string fullPath = Path.Combine(Server.MapPath(ArchiveFolder), filename);
            bool fileDeleted = false;

            // Delete physical file
            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                    fileDeleted = true;
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Warning: Could not delete physical file '{filename}'. Error: {ex.Message}";
                    TempData["MessageClass"] = "alert-warning";
                }
            }

            // Delete metadata
            if (reportToDelete != null)
            {
                ReportArchive.Reports.Remove(reportToDelete);
                TempData["Message"] = $"Report '{filename}' deleted successfully.";
                TempData["MessageClass"] = "alert-success";
            }
            else if (fileDeleted)
            {
                TempData["Message"] = $"Report '{filename}' file deleted, but metadata was not found.";
                TempData["MessageClass"] = "alert-warning";
            }
            else
            {
                TempData["Message"] = $"Error: Report '{filename}' not found.";
                TempData["MessageClass"] = "alert-danger";
            }

            return RedirectToAction("Reports");
        }

        // Load the maintenance screen with all entities
        public async Task<ActionResult> Maintain()
        {
            // Fetch staff with related data for editing
            var staffQuery = db.staffs.Include(s => s.stores).Include(s => s.staffs2)
                .Select(s => new StaffViewModel
                {
                    staff_id = s.staff_id,
                    first_name = s.first_name,
                    last_name = s.last_name,
                    email = s.email,
                    phone = s.phone,
                    active = s.active,
                    store_id = s.store_id,
                    manager_id = s.manager_id,
                    store_name = s.stores.store_name,
                    manager_name = s.manager_id == null ? "No Manager" : s.staffs2.first_name + " " + s.staffs2.last_name
                });

            // Fetch customers
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

            // Fetch products with related data
            var productsQuery = db.products.Select(p => new ProductViewModel
            {
                product_id = p.product_id,
                product_name = p.product_name,
                model_year = p.model_year,
                list_price = p.list_price,
                brand_id = p.brand_id,
                category_id = p.category_id,
                brand_name = p.brands.brand_name,
                category_name = p.categories.category_name,
                TotalStock = p.stocks.Sum(st => (int?)st.quantity) ?? 0
            });

            var viewModel = new MaintainViewModel
            {
                StaffsList = await staffQuery.ToListAsync(),
                CustomersList = await customersQuery.ToListAsync(),
                ProductsList = await productsQuery.ToListAsync()
            };

            // Populate dropdown data for modals
            ViewBag.Stores = new SelectList(await db.stores.ToListAsync(), "store_id", "store_name");
            ViewBag.StaffManagers = new SelectList(await db.staffs.OrderBy(s => s.last_name).ToListAsync(), "staff_id", "last_name");
            ViewBag.Brands = new SelectList(await db.brands.ToListAsync(), "brand_id", "brand_name");
            ViewBag.Categories = new SelectList(await db.categories.ToListAsync(), "category_id", "category_name");

            return View(viewModel);
        }

        // STAFF CRUD OPERATIONS

        // Get staff data for editing
        [HttpGet]
        public async Task<JsonResult> EditStaff(int id)
        {
            var staff = await db.staffs.FindAsync(id);

            if (staff == null)
            {
                return Json(new { success = false, message = "Staff member not found." }, JsonRequestBehavior.AllowGet);
            }

            var staffModel = new StaffViewModel
            {
                staff_id = staff.staff_id,
                first_name = staff.first_name,
                last_name = staff.last_name,
                email = staff.email,
                phone = staff.phone,
                active = staff.active,
                store_id = staff.store_id,
                manager_id = staff.manager_id
            };

            return Json(new { success = true, staff = staffModel }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<ActionResult> StaffEditPartial(int id)
        {
            var staff = await db.staffs.FindAsync(id);

            if (staff == null)
            {
                return HttpNotFound("Staff member not found.");
            }

            ViewBag.store_id = new SelectList(await db.stores.ToListAsync(), "store_id", "store_name", staff.store_id);
            ViewBag.manager_id = new SelectList(await db.staffs.OrderBy(s => s.last_name).ToListAsync(), "staff_id", "last_name", staff.manager_id);

            return PartialView("_EditPartial", staff); // Note the expected partial view name
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateStaff(u24637646_HW03.Models.staffs staff)
        {
            if (ModelState.IsValid)
            {
                db.Entry(staff).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();
                    TempData["Message"] = $"Staff member **{staff.first_name} {staff.last_name}** (ID: {staff.staff_id}) updated successfully.";
                    TempData["MessageClass"] = "alert-success";
                    return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
                }
                catch (Exception ex)
                {
                    // If DB save fails (e.g., integrity constraint)
                    return Json(new { success = false, message = $"Update failed: {ex.Message}" });
                }
            }

            // IF VALIDATION FAILS (The FIX): Return the partial view HTML with errors
            ViewBag.store_id = new SelectList(await db.stores.ToListAsync(), "store_id", "store_name", staff.store_id);
            ViewBag.manager_id = new SelectList(await db.staffs.OrderBy(s => s.last_name).ToListAsync(), "staff_id", "last_name", staff.manager_id);

            Response.StatusCode = (int)HttpStatusCode.OK; // Critical: Forces 200 status for AJAX to read the HTML response
            return PartialView("_EditPartial", staff);
        }

        [HttpGet]
        public async Task<ActionResult> StaffDeletePartial(int id)
        {
            var staff = await db.staffs
                .Include(s => s.stores)
                .Include(s => s.staffs2)
                .SingleOrDefaultAsync(s => s.staff_id == id);

            if (staff == null)
            {
                return HttpNotFound("Staff member not found for deletion.");
            }

            return PartialView("_DeletePartial", staff); // Note the expected partial view name
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteStaff(int staff_id)
        {
            var staff = await db.staffs.FindAsync(staff_id);

            if (staff == null)
            {
                return Json(new { success = false, message = "Staff member not found." });
            }

            // Check for dependent records (example check)
            var dependentStaffCount = await db.staffs.CountAsync(s => s.manager_id == staff_id);
            var ordersCount = await db.orders.CountAsync(o => o.staff_id == staff_id);

            if (dependentStaffCount > 0 || ordersCount > 0)
            {
                return Json(new { success = false, message = $"Cannot delete. This staff member manages {dependentStaffCount} other staff and is linked to {ordersCount} orders." });
            }

            try
            {
                db.staffs.Remove(staff);
                await db.SaveChangesAsync();
                TempData["Message"] = $"Staff member **{staff.first_name} {staff.last_name}** (ID: {staff.staff_id}) deleted successfully.";
                TempData["MessageClass"] = "alert-success";
                return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed: {ex.Message}" });
            }
        }

        // CUSTOMER CRUD OPERATIONS

        [HttpGet]
        public async Task<ActionResult> CustomerEditPartial(int id)
        {
            // Ensure you are using the correct model (u24637646_HW03.Models.customers)
            var customer = await db.customers.FindAsync(id);

            if (customer == null)
            {
                return HttpNotFound("Customer not found.");
            }

            // NOTE: Ensure your partial view is named _CustomerEditPartial.cshtml
            return PartialView("_EditPartial", customer);
        }

        // Get customer data for editing
        [HttpGet]
        public async Task<JsonResult> EditCustomer(int id)
        {
            var customer = await db.customers.FindAsync(id);

            if (customer == null)
            {
                return Json(new { success = false, message = "Customer not found." }, JsonRequestBehavior.AllowGet);
            }

            var customerModel = new CustomerViewModel
            {
                customer_id = customer.customer_id,
                first_name = customer.first_name,
                last_name = customer.last_name,
                email = customer.email,
                phone = customer.phone,
                street = customer.street,
                city = customer.city,
                state = customer.state,
                zip_code = customer.zip_code
            };

            return Json(new { success = true, customer = customerModel }, JsonRequestBehavior.AllowGet);
        }

        // Update customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateCustomer(u24637646_HW03.Models.customers customer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(customer).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();
                    TempData["Message"] = $"Customer {customer.first_name} {customer.last_name} (ID: {customer.customer_id}) updated successfully.";
                    TempData["MessageClass"] = "alert-success";
                    // Return JSON success object for JavaScript to handle redirect
                    return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
                }
                catch (Exception ex)
                {
                    // If DB save fails (e.g., unexpected error)
                    return Json(new { success = false, message = $"Update failed: {ex.Message}" });
                }
            }

            // IF VALIDATION FAILS (The FIX): Return the HTML partial view with error messages
            Response.StatusCode = (int)HttpStatusCode.OK; // Critical: Forces 200 status for AJAX script to read the content
            return PartialView("_EditPartial", customer);
        }

        [HttpGet]
        public async Task<ActionResult> CustomerDeletePartial(int id)
        {
            var customer = await db.customers.FindAsync(id);

            if (customer == null)
            {
                return HttpNotFound("Customer not found for deletion.");
            }

            // NOTE: Ensure your partial view is named _CustomerDeletePartial.cshtml
            return PartialView("_DeletePartial", customer);
        }

        // Delete customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteCustomer(int customer_id)
        {
            var customer = await db.customers.FindAsync(customer_id);

            if (customer == null)
            {
                return Json(new { success = false, message = "Customer not found." });
            }

            // Check for dependent records (e.g., orders)
            var ordersCount = await db.orders.CountAsync(o => o.customer_id == customer_id);

            if (ordersCount > 0)
            {
                return Json(new { success = false, message = $"Cannot delete. This customer has {ordersCount} related orders and cannot be removed." });
            }

            try
            {
                db.customers.Remove(customer);
                await db.SaveChangesAsync();
                TempData["Message"] = $"Customer {customer.first_name} {customer.last_name} (ID: {customer.customer_id}) deleted successfully.";
                TempData["MessageClass"] = "alert-success";
                return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed: {ex.Message}" });
            }
        }

        // PRODUCT CRUD OPERATIONS

        [HttpGet]
        public async Task<ActionResult> ProductEditPartial(int id)
        {
            var product = await db.products.FindAsync(id);

            if (product == null)
            {
                return HttpNotFound("Product not found.");
            }

            // Populate dropdowns for brands and categories
            ViewBag.brand_id = new SelectList(await db.brands.ToListAsync(), "brand_id", "brand_name", product.brand_id);
            ViewBag.category_id = new SelectList(await db.categories.ToListAsync(), "category_id", "category_name", product.category_id);

            // NOTE: Ensure your partial view is named _ProductEditPartial.cshtml
            return PartialView("_EditPartial", product);
        }

        // Get product data for editing
        [HttpGet]
        public async Task<JsonResult> EditProduct(int id)
        {
            var product = await db.products.FindAsync(id);

            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." }, JsonRequestBehavior.AllowGet);
            }

            var productModel = new ProductViewModel
            {
                product_id = product.product_id,
                product_name = product.product_name,
                model_year = product.model_year,
                list_price = product.list_price,
                brand_id = product.brand_id,
                category_id = product.category_id
            };

            return Json(new { success = true, product = productModel }, JsonRequestBehavior.AllowGet);
        }

        // Update product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateProduct(u24637646_HW03.Models.products product)
        {
            if (ModelState.IsValid)
            {
                db.Entry(product).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();
                    TempData["Message"] = $"Product {product.product_name} (ID: {product.product_id}) updated successfully.";
                    TempData["MessageClass"] = "alert-success";
                    // Return JSON success object for JavaScript to handle redirect
                    return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
                }
                catch (Exception ex)
                {
                    // If DB save fails (e.g., unexpected error)
                    return Json(new { success = false, message = $"Update failed: {ex.Message}" });
                }
            }

            // IF VALIDATION FAILS (The FIX): Regenerate dropdowns and return the HTML partial view
            ViewBag.brand_id = new SelectList(await db.brands.ToListAsync(), "brand_id", "brand_name", product.brand_id);
            ViewBag.category_id = new SelectList(await db.categories.ToListAsync(), "category_id", "category_name", product.category_id);

            Response.StatusCode = (int)HttpStatusCode.OK; // Critical: Forces 200 status for AJAX script
            return PartialView("_EditPartial", product);
        }

        [HttpGet]
        public async Task<ActionResult> ProductDeletePartial(int id)
        {
            var product = await db.products
                .Include(p => p.brands) // Eager load for display
                .Include(p => p.categories) // Eager load for display
                .SingleOrDefaultAsync(p => p.product_id == id);

            if (product == null)
            {
                return HttpNotFound("Product not found for deletion.");
            }

            // NOTE: Ensure your partial view is named _ProductDeletePartial.cshtml
            return PartialView("_DeletePartial", product);
        }

        // NEWLY ADDED: Delete product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteProduct(int product_id)
        {
            var product = await db.products.FindAsync(product_id);

            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            // Check for dependent records (order_items)
            var orderItemsCount = await db.order_items.CountAsync(oi => oi.product_id == product_id);
            if (orderItemsCount > 0)
            {
                return Json(new { success = false, message = $"Cannot delete. This product has {orderItemsCount} related order items." });
            }

            try
            {
                // Handle related 'stocks' records explicitly before deleting the product
                var stocks = await db.stocks.Where(s => s.product_id == product_id).ToListAsync();
                if (stocks.Any())
                {
                    db.stocks.RemoveRange(stocks);
                }

                db.products.Remove(product);
                await db.SaveChangesAsync();
                TempData["Message"] = $"Product **{product.product_name}** (ID: {product.product_id}) deleted successfully.";
                TempData["MessageClass"] = "alert-success";
                return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed: {ex.Message}" });
            }
        }
    }
}