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

// NOTE: Ensure your ViewModels (StaffViewModel, CustomerViewModel, ProductViewModel, MaintainViewModel)
// and Models (BikeStoresEntities, staffs, customers, products, etc.) are correctly defined in your project.

namespace u24637646_HW03.Controllers
{
    public class HomeController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();
        private const string ArchiveFolder = "~/ArchivedReports/";

        // Helper function to load reports from the physical directory (Keep as is)
        private List<ArchivedReport> LoadArchivedReports(string serverPath)
        {
            // ... (LoadArchivedReports content as before) ...
            var archivedList = new List<ArchivedReport>();

            if (Directory.Exists(serverPath))
            {
                // Get all PDF files in the directory
                var pdfFiles = Directory.EnumerateFiles(serverPath, "*.pdf");

                foreach (var filePath in pdfFiles)
                {
                    string filename = Path.GetFileName(filePath);
                    DateTime lastModified = System.IO.File.GetLastWriteTime(filePath);

                    // Try to find existing metadata
                    var existingMetadata = ReportArchive.Reports
            .FirstOrDefault(r => r.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));

                    // Use existing metadata or create a default
                    var report = existingMetadata ?? new ArchivedReport
                    {
                        Filename = filename,
                        Filetype = "PDF",
                        DateSaved = lastModified,
                        ChartType = filename.Contains("Monthly_Order_Trend") ? "Monthly Order Trend" :
            (filename.Contains("Sales_Revenue_Distribution") ? "Sales Revenue Distribution" : "Custom Chart"),
                        Description = $"Chart saved on {lastModified:yyyy-MM-dd HH:mm}"
                    };

                    // CRITICAL: Ensure the DateSaved reflects the file's date if no metadata exists
                    if (existingMetadata == null)
                    {
                        report.DateSaved = lastModified;
                        // Add newly discovered file's metadata to the static list for future reference
                        ReportArchive.Reports.Add(report);
                    }

                    archivedList.Add(report);
                }

                // CRITICAL: Clean up metadata for files that no longer exist
                var filesInDir = new HashSet<string>(pdfFiles.Select(Path.GetFileName), StringComparer.OrdinalIgnoreCase);
                ReportArchive.Reports.RemoveAll(r => !filesInDir.Contains(r.Filename));
            }

            // Return only the files that currently exist, ordered by date
            return archivedList.OrderByDescending(r => r.DateSaved).ToList();
        }

        // --- REPORTS ACTION (Keep as is) ---
        public async Task<ActionResult> Reports()
        {
            // ... (Reports content as before) ...
            var jsonSetting = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

            // 1. Doughnut Chart Data Preparation (Sales by Store)
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


            // 2. Line Chart Data Preparation (Monthly Order Trend)
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

            // FIX: Load reports directly from the file system and update metadata
            string serverPath = Server.MapPath(ArchiveFolder);
            ViewBag.ArchivedReports = LoadArchivedReports(serverPath); // Uses the helper to sync file system with metadata

            return View();
        }

        // --- EXISTING INDEX ACTION (Keep as is) ---
        public async Task<ActionResult> Index(string selectedBrand, string selectedCategory)
        {
            // --- 1. HANDLE MAINTENANCE MESSAGES ---
            // If the user was redirected from an Edit or Delete operation (using standard TempData keys)
            if (TempData["Message"] != null)
            {
                ViewBag.Message = TempData["Message"].ToString();
                ViewBag.MessageClass = TempData["MessageClass"]?.ToString() ?? "alert-info";
            }
            // TempData["SuccessMessage"] is handled directly in the view (for Create)


            // --- 2. PRODUCT FILTERING LOGIC ---
            IQueryable<products> productsQuery = db.products;

            if (!string.IsNullOrEmpty(selectedBrand) && selectedBrand != "All Brands")
            {
                productsQuery = productsQuery.Where(p => p.brands.brand_name == selectedBrand);
            }

            if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "All Categories")
            {
                productsQuery = productsQuery.Where(p => p.categories.category_name == selectedCategory);
            }

            // Get all unique brands/categories for filter dropdowns (before main query execution)
            ViewData["AllBrands"] = await db.brands.Select(b => b.brand_name).Distinct().OrderBy(n => n).ToListAsync();
            ViewData["AllCategories"] = await db.categories.Select(c => c.category_name).Distinct().OrderBy(n => n).ToListAsync();
            ViewData["SelectedBrand"] = selectedBrand;
            ViewData["SelectedCategory"] = selectedCategory;


            // --- 3. FETCH DATA (STAFF, CUSTOMER, PRODUCT) ---
            var viewModel = new HomeIndexViewModel();

            // Fetch Products (with filtering applied)
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

            // Assign ListIndex to Products
            for (int i = 0; i < productList.Count; i++)
            {
                productList[i].ListIndex = i + 1;
            }
            viewModel.ProductsList = productList;


            // Fetch Staff
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

            // Assign ListIndex to Staff
            for (int i = 0; i < staffList.Count; i++)
            {
                staffList[i].ListIndex = i + 1;
            }
            viewModel.StaffsList = staffList;

            // Fetch Staff Sales (Used for Staff panel details)
            viewModel.StaffSalesList = await db.order_items
                .Include(oi => oi.orders).Include(oi => oi.products)
                .Where(oi => oi.orders.staff_id != null)
                .OrderByDescending(oi => oi.orders.order_date)
                .Select(oi => new StaffSaleViewModel
                {
                    staff_id = oi.orders.staff_id,
                    product_name = oi.products.product_name
                })
                .ToListAsync();


            // Fetch Customers
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

            // Assign ListIndex to Customers
            for (int i = 0; i < customerList.Count; i++)
            {
                customerList[i].ListIndex = i + 1;
            }
            viewModel.CustomersList = customerList;

            // Fetch Customer Purchases (Used for Customer panel details)
            viewModel.CustomerPurchasesList = await db.order_items
                .Include(oi => oi.orders).Include(oi => oi.products)
                .Where(oi => oi.orders.customer_id != null)
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

        // --- ARCHIVE ACTIONS (Keep as is) ---
        [HttpPost]
        [ValidateInput(false)] // REQUIRED to accept HTML content from TinyMCE
        public ActionResult SaveChartReport(ReportSubmissionModel model)
        {
            // ... (SaveChartReport content as before) ...
            // Enhanced validation and logging
            if (string.IsNullOrWhiteSpace(model.PdfBase64Data))
            {
                TempData["Message"] = "Error: PDF data is missing. Please try generating the chart again.";
                return RedirectToAction("Reports");
            }

            if (string.IsNullOrWhiteSpace(model.Filename))
            {
                TempData["Message"] = "Error: Filename is missing. Please provide a valid filename.";
                return RedirectToAction("Reports");
            }

            if (string.IsNullOrWhiteSpace(model.ChartName))
            {
                TempData["Message"] = "Error: Chart name is missing.";
                return RedirectToAction("Reports");
            }

            try
            {
                // 1. Clean and sanitize filename
                string baseFilename = Path.GetInvalidFileNameChars()
            .Aggregate(model.Filename, (current, c) => current.Replace(c.ToString(), "_"));

                // Remove any existing .pdf extension to avoid duplication
                if (baseFilename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    baseFilename = baseFilename.Substring(0, baseFilename.Length - 4);
                }

                // Create standardized filename
                string fullFilename = $"{baseFilename}_{model.ChartName.Replace(" ", "_")}.pdf";

                string serverPath = Server.MapPath(ArchiveFolder);
                string fullPath = Path.Combine(serverPath, fullFilename);

                // 2. Ensure the Archive directory exists
                if (!Directory.Exists(serverPath))
                {
                    Directory.CreateDirectory(serverPath);
                }

                // 3. Clean Base64 string (remove any whitespace or data URI prefix)
                string cleanBase64 = model.PdfBase64Data
            .Replace("data:application/pdf;base64,", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(" ", "");

                // Validate Base64 string
                if (cleanBase64.Length % 4 != 0)
                {
                    TempData["Message"] = "Error: Invalid PDF data format. Please try again.";
                    return RedirectToAction("Reports");
                }

                // 4. Convert Base64 string to PDF bytes and save
                byte[] pdfBytes = Convert.FromBase64String(cleanBase64);

                // Validate that we have actual data
                if (pdfBytes.Length == 0)
                {
                    TempData["Message"] = "Error: PDF data is empty. Please try generating the chart again.";
                    return RedirectToAction("Reports");
                }

                // Write the file
                System.IO.File.WriteAllBytes(fullPath, pdfBytes);

                // 5. Update Report Metadata
                var existingReport = ReportArchive.Reports
            .FirstOrDefault(r => r.Filename.Equals(fullFilename, StringComparison.OrdinalIgnoreCase));

                if (existingReport != null)
                {
                    // Update existing entry (in case of overwrite)
                    existingReport.DateSaved = DateTime.Now;
                    existingReport.Description = string.IsNullOrWhiteSpace(model.Description)
                        ? $"**{model.ChartName}** chart saved on {DateTime.Now:yyyy-MM-dd HH:mm}"
                        : model.Description;
                    existingReport.ChartType = model.ChartName;
                }
                else
                {
                    // Add new report metadata
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

                TempData["Message"] = $"✓ Chart Report '{fullFilename}' successfully archived! ({pdfBytes.Length:N0} bytes)";
            }
            catch (FormatException ex)
            {
                TempData["Message"] = $"Error: Invalid PDF data format. {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error saving report: {ex.Message}";
            }

            return RedirectToAction("Reports");
        }

        // Action to handle updating the description via the modal (Rich Text Box)
        [HttpPost]
        [ValidateInput(false)] // REQUIRED to accept HTML content from TinyMCE
        public ActionResult UpdateReportDescription(string filename, string Description)
        {
            // ... (UpdateReportDescription content as before) ...
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

        /// <summary>
        /// Action to handle downloading an archived report (PDF expected).
        /// </summary>
        public FileResult DownloadReport(string filename, string filetype)
        {
            // ... (DownloadReport content as before) ...
            string fullFilename = Path.GetFileName(filename); // Ensure we only get the filename
            string fullPath = Path.Combine(Server.MapPath(ArchiveFolder), fullFilename);

            if (!filetype.Equals("PDF", StringComparison.OrdinalIgnoreCase))
            {
                return File(Encoding.UTF8.GetBytes("File type not supported for download."), "text/plain", "Error.txt");
            }

            if (System.IO.File.Exists(fullPath))
            {
                // Serve the physical PDF file
                string contentType = "application/pdf";
                return File(fullPath, contentType, fullFilename);
            }
            else
            {
                // Handle case where metadata exists but the file is missing
                string errorContent = $"Error: PDF file '{fullFilename}' not found on the server at path: {fullPath}";
                return File(Encoding.UTF8.GetBytes(errorContent), "text/plain", "Download_Error.txt");
            }
        }

        /// <summary>
        /// Action to handle deleting a report (metadata and physical file).
        /// </summary>
        [HttpPost]
        public ActionResult DeleteReport(string filename)
        {
            // ... (DeleteReport content as before) ...
            var reportToDelete = ReportArchive.Reports.FirstOrDefault(r => r.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));
            string fullPath = Path.Combine(Server.MapPath(ArchiveFolder), filename);
            bool fileDeleted = false;

            // 1. Delete Physical File
            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                    fileDeleted = true;
                }
                catch (Exception ex)
                {
                    // Log or report error, but proceed to delete metadata
                    TempData["Message"] = $"Warning: Metadata deletion pending. Could not delete physical file '{filename}'. Error: {ex.Message}";
                }
            }

            // 2. Delete Metadata
            if (reportToDelete != null)
            {
                ReportArchive.Reports.Remove(reportToDelete);
                TempData["Message"] = $"Report '{filename}' deleted successfully from archive and server.";
            }
            else if (fileDeleted)
            {
                TempData["Message"] = $"Report '{filename}' file deleted successfully, but metadata was not found.";
            }
            else
            {
                TempData["Message"] = $"Error: Report '{filename}' not found for deletion.";
            }

            return RedirectToAction("Reports");
        }

        // =========================================================
        // 🚀 NEW MAINTAIN ACTION AND ASYNC CRUD METHODS
        // =========================================================

        /// <summary>
        /// Loads all Staff, Customers, and Products for the Maintain screen.
        /// </summary>
        public async Task<ActionResult> Maintain()
        {
            // Staff Query (including IDs for Update/Delete)
            var staffQuery = db.staffs.Include(s => s.stores).Include(s => s.staffs2)
                .Select(s => new StaffViewModel
                {
                    staff_id = s.staff_id,
                    first_name = s.first_name,
                    last_name = s.last_name,
                    email = s.email,
                    phone = s.phone,
                    active = s.active,
                    store_id = s.store_id, // For Update
                    manager_id = s.manager_id, // For Update
                    store_name = s.stores.store_name, // For Display
                    manager_name = s.manager_id == null ? "No Manager" : s.staffs2.first_name + " " + s.staffs2.last_name // For Display
                });

            // Customers Query
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

            // Products Query
            var productsQuery = db.products.Select(p => new ProductViewModel
            {
                product_id = p.product_id,
                product_name = p.product_name,
                model_year = p.model_year,
                list_price = p.list_price,
                brand_id = p.brand_id, // For Update
                category_id = p.category_id, // For Update
                brand_name = p.brands.brand_name, // For Display
                category_name = p.categories.category_name, // For Display
                TotalStock = p.stocks.Sum(st => (int?)st.quantity) ?? 0 // Total stock from all stores
            });

            var viewModel = new MaintainViewModel
            {
                StaffsList = await staffQuery.ToListAsync(),
                CustomersList = await customersQuery.ToListAsync(),
                ProductsList = await productsQuery.ToListAsync()
            };

            // Additionally, pass dropdown data for the modals (Used in the View via AJAX)
            ViewBag.Stores = new SelectList(await db.stores.ToListAsync(), "store_id", "store_name");
            // Exclude the staff member being edited from the potential managers list in real scenario, but here we include all staff
            ViewBag.StaffManagers = new SelectList(await db.staffs.OrderBy(s => s.last_name).ToListAsync(), "staff_id", "last_name");
            ViewBag.Brands = new SelectList(await db.brands.ToListAsync(), "brand_id", "brand_name");
            ViewBag.Categories = new SelectList(await db.categories.ToListAsync(), "category_id", "category_name");

            return View(viewModel);
        }

        // ---------------------------------------------------------
        // STAFF CRUD OPERATIONS
        // ---------------------------------------------------------

        // EDIT (Get data for modal)
        [HttpGet]
        public async Task<JsonResult> EditStaff(int id)
        {
            var staff = await db.staffs.FindAsync(id);

            if (staff == null)
            {
                return Json(new { success = false, message = "Staff member not found." }, JsonRequestBehavior.AllowGet);
            }

            // Return the staff model object with FKs for the edit modal
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

        // UPDATE (Post data from modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateStaff(StaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                var staff = await db.staffs.FindAsync(model.staff_id);

                if (staff == null)
                {
                    return Json(new { success = false, message = "Staff member not found." });
                }

                // Update properties from ViewModel to Entity model
                staff.first_name = model.first_name;
                staff.last_name = model.last_name;
                staff.email = model.email;
                staff.phone = model.phone;
                staff.active = model.active;
                staff.store_id = model.store_id;
                staff.manager_id = model.manager_id;

                db.Entry(staff).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();
                    // Return the fully rendered/updated view model data for list refresh
                    var updatedStaff = await db.staffs
                        .Include(s => s.stores).Include(s => s.staffs2)
                        .Where(s => s.staff_id == model.staff_id)
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
                        })
                        .FirstOrDefaultAsync();

                    return Json(new { success = true, message = $"Staff '{model.first_name} {model.last_name}' updated successfully.", data = updatedStaff });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Update failed: {ex.Message}" });
                }
            }

            var errors = ModelState.Where(x => x.Value.Errors.Any()).Select(x => new { x.Key, x.Value.Errors }).ToList();
            return Json(new { success = false, message = "Validation failed.", errors = errors });
        }

        // DELETE (Post data from modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteStaff(int id)
        {
            try
            {
                var staff = await db.staffs.FindAsync(id);
                if (staff == null)
                {
                    return Json(new { success = false, message = "Staff member not found." });
                }

                // CRITICAL: Check for dependent records (e.g., orders assigned to this staff)
                var ordersCount = await db.orders.CountAsync(o => o.staff_id == id);
                if (ordersCount > 0)
                {
                    return Json(new { success = false, message = $"Deletion failed. Staff member has {ordersCount} related orders and cannot be deleted." });
                }

                db.staffs.Remove(staff);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = $"Staff member ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed: {ex.Message}" });
            }
        }

        // ---------------------------------------------------------
        // CUSTOMER CRUD OPERATIONS
        // ---------------------------------------------------------

        // EDIT (Get data for modal)
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

        // UPDATE (Post data from modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateCustomer(CustomerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var customer = await db.customers.FindAsync(model.customer_id);

                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found." });
                }

                // Update properties
                customer.first_name = model.first_name;
                customer.last_name = model.last_name;
                customer.email = model.email;
                customer.phone = model.phone;
                customer.street = model.street;
                customer.city = model.city;
                customer.state = model.state;
                customer.zip_code = model.zip_code;

                db.Entry(customer).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();

                    // Return the updated view model data
                    var updatedCustomer = new CustomerViewModel
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

                    return Json(new { success = true, message = $"Customer '{model.first_name} {model.last_name}' updated successfully.", data = updatedCustomer });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Update failed: {ex.Message}" });
                }
            }

            var errors = ModelState.Where(x => x.Value.Errors.Any()).Select(x => new { x.Key, x.Value.Errors }).ToList();
            return Json(new { success = false, message = "Validation failed.", errors = errors });
        }

        // DELETE (Post data from modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await db.customers.FindAsync(id);
                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found." });
                }

                // CRITICAL: Check for dependent records (e.g., orders placed by this customer)
                var ordersCount = await db.orders.CountAsync(o => o.customer_id == id);
                if (ordersCount > 0)
                {
                    return Json(new { success = false, message = $"Deletion failed. Customer has {ordersCount} related orders and cannot be deleted." });
                }

                db.customers.Remove(customer);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = $"Customer ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed: {ex.Message}" });
            }
        }

        // ---------------------------------------------------------
        // PRODUCT CRUD OPERATIONS
        // ---------------------------------------------------------

        // EDIT (Get data for modal)
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

        // UPDATE (Post data from modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateProduct(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = await db.products.FindAsync(model.product_id);

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // Update properties
                product.product_name = model.product_name;
                product.model_year = model.model_year;
                product.list_price = model.list_price;
                product.brand_id = model.brand_id;
                product.category_id = model.category_id;

                db.Entry(product).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();

                    // Return the updated view model data (with display names/stock)
                    var updatedProduct = await db.products
                        .Include(p => p.brands).Include(p => p.categories).Include(p => p.stocks)
                        .Where(p => p.product_id == model.product_id)
                        .Select(p => new ProductViewModel
                        {
                            product_id = p.product_id,
                            product_name = p.product_name,
                            model_year = p.model_year,
                            list_price = p.list_price,
                            brand_name = p.brands.brand_name,
                            category_name = p.categories.category_name,
                            TotalStock = p.stocks.Sum(st => (int?)st.quantity) ?? 0
                        })
                        .FirstOrDefaultAsync();

                    return Json(new { success = true, message = $"Product '{model.product_name}' updated successfully.", data = updatedProduct });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Update failed: {ex.Message}" });
                }
            }

            var errors = ModelState.Where(x => x.Value.Errors.Any()).Select(x => new { x.Key, x.Value.Errors }).ToList();
            return Json(new { success = false, message = "Validation failed.", errors = errors });
        }

        // DELETE (Post data from modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteProduct(int id)
        {
            try
            {
                var product = await db.products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // CRITICAL: Check for dependent records (e.g., product in order_items or stock)
                var orderItemsCount = await db.order_items.CountAsync(oi => oi.product_id == id);
                if (orderItemsCount > 0)
                {
                    return Json(new { success = false, message = $"Deletion failed. Product is included in {orderItemsCount} orders and cannot be deleted." });
                }

                // Delete related stock records first if you want to allow deletion
                var stocks = await db.stocks.Where(s => s.product_id == id).ToListAsync();
                db.stocks.RemoveRange(stocks);
                await db.SaveChangesAsync(); // Save changes for stocks before deleting the product itself

                db.products.Remove(product);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = $"Product ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed: {ex.Message}" });
            }
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