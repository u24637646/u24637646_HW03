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

        // --------------------------------------------------------------------------------
        // ACTION: Index (Modified for ListIndex)
        // --------------------------------------------------------------------------------
        public async Task<ActionResult> Index()
        {
            var customerQuery = db.customers.
                                     Select(c => new ViewModels.CustomerViewModel
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
                                         ListIndex = 0 // Placeholder
                                     });

            var customerList = await customerQuery.OrderBy(c => c.customer_id).ToListAsync();

            // ⭐ Assign ListIndex based on position in the final list
            for (int i = 0; i < customerList.Count; i++)
            {
                customerList[i].ListIndex = i + 1;
            }

            return View(customerList);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Create (GET/POST)
        // --------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult CreatePartial() { return PartialView("_CreatePartial", new customers()); }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreatePartial(customers customer)
        {
            if (ModelState.IsValid)
            {
                db.customers.Add(customer); await db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Customer **{customer.first_name} {customer.last_name}** created successfully!";

                // ⭐ CORRECTED REDIRECT: Redirect to Home/Index
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
            }
            Response.StatusCode = 200;
            return PartialView("_CreatePartial", customer);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Edit (GET)
        // --------------------------------------------------------------------------------
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            customers customers = await db.customers.FindAsync(id);
            if (customers == null) return HttpNotFound();

            return PartialView("_EditPartial", customers);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Edit (POST)
        // --------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "customer_id,first_name,last_name,phone,email,street,city,state,zip_code")] customers customers)
        {
            if (ModelState.IsValid)
            {
                db.Entry(customers).State = EntityState.Modified;
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Customer **{customers.first_name} {customers.last_name}** updated successfully!";

                // ⭐ CORRECTED REDIRECT: Redirect to Home/Index
                return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
            }

            Response.StatusCode = 200;
            return PartialView("_EditPartial", customers);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Delete (POST)
        // --------------------------------------------------------------------------------
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

                // ⭐ CORRECTED REDIRECT: Redirect to Home/Index
                return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed (Customers): {ex.Message}" });
            }
        }

        // --------------------------------------------------------------------------------
        // Standard View Actions (Details, Create, Delete GET)
        // --------------------------------------------------------------------------------
        public async Task<ActionResult> Details(int? id)
        {
            // ... (Standard Details logic using CustomerViewModel) ...
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
            if (customerVM == null) return HttpNotFound(); return View(customerVM);
        }

        public ActionResult Create() { return View(); }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "customer_id,first_name,last_name,phone,email,street,city,state,zip_code")] customers customers)
        {
            if (ModelState.IsValid) { db.customers.Add(customers); await db.SaveChangesAsync(); return RedirectToAction("Index"); }
            return View(customers);
        }

        public async Task<ActionResult> Delete(int? id)
        {
            // ... (Standard Delete GET logic using CustomerViewModel) ...
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
            if (customerVM == null) return HttpNotFound(); return View(customerVM);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StandardDeleteConfirmed(int id)
        {
            customers customers = await db.customers.FindAsync(id);
            db.customers.Remove(customers); await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}