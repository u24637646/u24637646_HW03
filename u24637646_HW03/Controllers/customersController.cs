using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using u24637646_HW03.Models;
using u24637646_HW03.ViewModels;

namespace u24637646_HW03.Controllers
{
    public class customersController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // Display list of all customers
        public async Task<ActionResult> Index()
        {
            var customerQuery = db.customers
                .Select(c => new ViewModels.CustomerViewModel
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
                    ListIndex = 0 // Placeholder, will be assigned below
                });

            var customerList = await customerQuery.OrderBy(c => c.customer_id).ToListAsync();

            // Assign list indices based on position in the final list
            for (int i = 0; i < customerList.Count; i++)
            {
                customerList[i].ListIndex = i + 1;
            }

            return View(customerList);
        }

        // Load create form in modal
        [HttpGet]
        public ActionResult CreatePartial()
        {
            return PartialView("_CreatePartial", new customers());
        }

        // Handle customer creation via AJAX (Added try-catch for error handling)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreatePartial(customers customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.customers.Add(customer);
                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Customer **{customer.first_name} {customer.last_name}** created successfully!";

                    return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
                }
                catch (Exception ex)
                {
                    // Return failure JSON response with error details
                    Response.StatusCode = 200;
                    return Json(new { success = false, message = "Unable to create customer. Database error: " + ex.Message });
                }
            }

            // If validation fails, return the form with errors
            Response.StatusCode = 200;
            return PartialView("_CreatePartial", customer);
        }

        // Action for a full-page GET request to edit a customer
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            customers customers = await db.customers.FindAsync(id);
            if (customers == null) return HttpNotFound();

            return View(customers);
        }

        // Action for a full-page POST request to save an edited customer (Standard Edit - Added try-catch)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "customer_id,first_name,last_name,phone,email,street,city,state,zip_code")] customers customers)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(customers).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index"); // Redirect to the Index page on success
                }
                catch (Exception ex)
                {
                    // Add a model error to display on the view via ValidationSummary
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator. Details: " + ex.Message);
                }
            }
            // If ModelState is invalid or catch block hit, return the view with errors
            return View(customers);
        }

        // Load edit form in modal
        public async Task<ActionResult> EditPartial(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            customers customers = await db.customers.FindAsync(id);
            if (customers == null) return HttpNotFound();

            return PartialView("_EditPartial", customers);
        }

        // Handle customer update via AJAX (Renamed to EditPartialPost and added try-catch)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("EditPartialPost")] // Using a distinct name to avoid conflict with standard Edit POST
        public async Task<ActionResult> EditPartialPost([Bind(Include = "customer_id,first_name,last_name,phone,email,street,city,state,zip_code")] customers customers)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(customers).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Customer **{customers.first_name} {customers.last_name}** updated successfully!";

                    return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
                }
                catch (Exception ex)
                {
                    // Return a failure JSON response with the error
                    Response.StatusCode = 200;
                    return Json(new { success = false, message = "Unable to save changes. Database error: " + ex.Message });
                }
            }

            // If validation fails, return the form with errors
            Response.StatusCode = 200;
            return PartialView("_EditPartial", customers);
        }

        // Handle customer deletion via AJAX (Enhanced error handling)
        [HttpPost]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            customers customer = await db.customers.FindAsync(id);
            if (customer == null) return Json(new { success = false, message = "Record not found." });

            string customerName = $"{customer.first_name} {customer.last_name}";

            try
            {
                db.customers.Remove(customer);
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Customer **{customerName}** deleted successfully!";

                return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
            }
            catch (Exception ex)
            {
                // This handles Foreign Key constraint issues
                return Json(new { success = false, message = $"Deletion failed. This customer may have related data (e.g., orders) that must be deleted first. Details: {ex.Message}" });
            }
        }

        // Display detailed customer information
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var customerVM = await db.customers
                .Where(c => c.customer_id == id)
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
                    zip_code = c.zip_code
                }).FirstOrDefaultAsync();

            if (customerVM == null) return HttpNotFound();
            return View(customerVM);
        }

        // Standard create form (non-AJAX)
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "customer_id,first_name,last_name,phone,email,street,city,state,zip_code")] customers customers)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.customers.Add(customers);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Add a model error to display on the view via ValidationSummary
                    ModelState.AddModelError("", "Unable to create customer. Try again, and if the problem persists, see your system administrator. Details: " + ex.Message);
                }
            }
            return View(customers);
        }

        // Standard delete confirmation page (non-AJAX)
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var customerVM = await db.customers
                .Where(c => c.customer_id == id)
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
                    zip_code = c.zip_code
                }).FirstOrDefaultAsync();

            if (customerVM == null) return HttpNotFound();
            return View(customerVM);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StandardDeleteConfirmed(int id)
        {
            try
            {
                customers customers = await db.customers.FindAsync(id);
                if (customers == null) return HttpNotFound();

                db.customers.Remove(customers);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // If deletion fails, reload the Delete page with an error.
                TempData["ErrorMessage"] = $"Deletion failed. This customer may have related data (e.g., orders) that must be deleted first. Details: {ex.Message}";
                return RedirectToAction("Delete", new { id = id });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}