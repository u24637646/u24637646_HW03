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

        // Update staff member
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

                // Update properties
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

                    // Return updated data for UI refresh
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

                    // ADDED REDIRECT URL
                    return Json(new { success = true, message = $"Staff '{model.first_name} {model.last_name}' updated successfully.", data = updatedStaff, redirectUrl = Url.Action("Maintain", "Home") });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Update failed: {ex.Message}" });
                }
            }

            var errors = ModelState.Where(x => x.Value.Errors.Any()).Select(x => new { x.Key, x.Value.Errors }).ToList();
            return Json(new { success = false, message = "Validation failed.", errors = errors });
        }

        // Delete staff member
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

                // Check for dependent records
                var ordersCount = await db.orders.CountAsync(o => o.staff_id == id);
                if (ordersCount > 0)
                {
                    return Json(new { success = false, message = $"Cannot delete. This staff member has {ordersCount} related orders." });
                }

                db.staffs.Remove(staff);
                await db.SaveChangesAsync();

                // ADDED REDIRECT URL
                return Json(new { success = true, message = $"Staff member ID {id} deleted successfully.", redirectUrl = Url.Action("Maintain", "Home") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed: {ex.Message}" });
            }
        }

        // CUSTOMER CRUD OPERATIONS

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

                    // ADDED REDIRECT URL
                    return Json(new { success = true, message = $"Customer '{model.first_name} {model.last_name}' updated successfully.", data = updatedCustomer, redirectUrl = Url.Action("Maintain", "Home") });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Update failed: {ex.Message}" });
                }
            }

            var errors = ModelState.Where(x => x.Value.Errors.Any()).Select(x => new { x.Key, x.Value.Errors }).ToList();
            return Json(new { success = false, message = "Validation failed.", errors = errors });
        }

        // Delete customer
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

                // Check for dependent orders
                var ordersCount = await db.orders.CountAsync(o => o.customer_id == id);
                if (ordersCount > 0)
                {
                    return Json(new { success = false, message = $"Cannot delete. Customer has {ordersCount} related orders." });
                }

                db.customers.Remove(customer);
                await db.SaveChangesAsync();

                // ADDED REDIRECT URL
                return Json(new { success = true, message = $"Customer ID {id} deleted successfully.", redirectUrl = Url.Action("Maintain", "Home") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed: {ex.Message}" });
            }
        }

        // PRODUCT CRUD OPERATIONS

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

                    // Return updated data for UI refresh
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

                    // ADDED REDIRECT URL
                    return Json(new { success = true, message = $"Product '{model.product_name}' updated successfully.", data = updatedProduct, redirectUrl = Url.Action("Maintain", "Home") });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Update failed: {ex.Message}" });
                }
            }

            var errors = ModelState.Where(x => x.Value.Errors.Any()).Select(x => new { x.Key, x.Value.Errors }).ToList();
            return Json(new { success = false, message = "Validation failed.", errors = errors });
        }

        // NEWLY ADDED: Delete product
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

                // Check for dependent records (order_items)
                var orderItemsCount = await db.order_items.CountAsync(oi => oi.product_id == id);
                if (orderItemsCount > 0)
                {
                    return Json(new { success = false, message = $"Cannot delete. This product has {orderItemsCount} related order items." });
                }

                // Handle related 'stocks' records (assuming they must be deleted first)
                var stocks = await db.stocks.Where(s => s.product_id == id).ToListAsync();
                if (stocks.Any())
                {
                    db.stocks.RemoveRange(stocks);
                }

                db.products.Remove(product);
                await db.SaveChangesAsync();

                // ADDED REDIRECT URL
                return Json(new { success = true, message = $"Product ID {id} deleted successfully.", redirectUrl = Url.Action("Maintain", "Home") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed: {ex.Message}" });
            }
        }
    }
}