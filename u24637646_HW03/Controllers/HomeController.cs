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

// ====================================================================
// CRITICAL: REQUIRED STATIC CLASSES AND MODELS FOR ARCHIVING FUNCTIONALITY
// These must be defined outside the controller, typically in the Models/ViewModels namespace.
// We are including them here for a complete, single-file fix.
// ====================================================================

namespace u24637646_HW03.Controllers
{
    public class HomeController : Controller
    {
        // NOTE: Ensure your BikeStoresEntities context is correctly set up
        private BikeStoresEntities db = new BikeStoresEntities();

        // Using the folder path provided in your request.
        private const string ArchiveFolder = "~/ArchivedReports/";

        // Helper function to load reports from the physical directory
        private List<ArchivedReport> LoadArchivedReports(string serverPath)
        {
            var archivedList = new List<ArchivedReport>();

            if (Directory.Exists(serverPath))
            {
                // Get all PDF files in the directory
                var pdfFiles = Directory.EnumerateFiles(serverPath, "*.pdf");

                foreach (var filePath in pdfFiles)
                {
                    string filename = Path.GetFileName(filePath);
                    DateTime lastModified = System.IO.File.GetLastWriteTime(filePath);

                    // Try to find existing metadata (to keep user-defined description)
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

        // --- REPORTS ACTION ---
        // NOTE: The duplicate Reports action has been removed.
        public async Task<ActionResult> Reports()
        {
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

        // --- ARCHIVE ACTIONS ---

        /// <summary>
        /// Saves the PDF chart report generated by pdfmake (passed as Base64) to the server.
        /// NOTE: The duplicate SaveChartReport action has been removed.
        /// </summary>
        [HttpPost]
        [ValidateInput(false)] // REQUIRED to accept HTML content from TinyMCE
        public ActionResult SaveChartReport(ReportSubmissionModel model)
        {
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