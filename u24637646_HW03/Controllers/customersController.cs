using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using u24637646_HW03.Models;
using u24637646_HW03.ViewModels;

namespace u24637646_HW03.Controllers
{
    public class customersController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();



        // Display (Index) action using CustomerViewModel
        public async Task<ActionResult> Index()
        {
            // 1. Building the Async LINQ query
            var customerQuery = db.customers.
                            // 2. Converting to the ViewModel
                            Select(c => new ViewModels.CustomerViewModel
                            {
                                customer_id = c.customer_id,
                                first_name = c.first_name,
                                last_name = c.last_name,
                                //3. Handling null or empty phone numbers
                                phone = (c.phone == null || c.phone == "") ? "-" : c.phone,
                                email = c.email,
                                street = c.street,
                                city = c.city,
                                state = c.state.Length > 2 ? c.state.Substring(0, 2).ToUpper() : c.state,
                                zip_code = c.zip_code
                            });
            //4. Executing the query and getting the results
            var customerList = await customerQuery.ToListAsync();
            return View(customerList);

        }

        //Display (Details) action
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            // Query for a single customer and project into the ViewModel
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

            if (customerVM == null)
            {
                return HttpNotFound();
            }
            return View(customerVM);
        }

        // GET: customers/CreatePartial
        [HttpGet]
        public ActionResult CreatePartial()
        {
            // Pass a new, empty model to the partial view
            return PartialView("_CreatePartial", new customers());
        }

        // POST: customers/CreatePartial (The Partial View form is submitted to this action)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Note: You must bind to the EF Model 'customers', not the ViewModel
        public async Task<ActionResult> CreatePartial(customers customer)
        {
            if (ModelState.IsValid)
            {
                // 1. Save to Database
                db.customers.Add(customer);
                await db.SaveChangesAsync();

                // 2. Set Success Message for the next page load
                TempData["SuccessMessage"] = $"Customer **{customer.first_name} {customer.last_name}** created successfully!";

                // 3. **Return JSON on Success**
                // The JavaScript expects `{ success: true, redirectUrl: ... }`
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
            }

            // If validation fails (ModelState is NOT valid)
            // 4. **Return the Partial View HTML with Validation Errors**
            Response.StatusCode = 200; // Important: Forces jQuery to process the response body
            return PartialView("_CreatePartial", customer);
        }


        // GET: customers/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "customer_id,first_name,last_name,phone,email,street,city,state,zip_code")] customers customers)
        {
            if (ModelState.IsValid)
            {
                db.customers.Add(customers);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(customers);
        }

        //Display (Edit) action
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            customers customers = await db.customers.FindAsync(id);
            if (customers == null)
            {
                return HttpNotFound();
            }
            return View(customers);
        }

        // POST: customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "customer_id,first_name,last_name,phone,email,street,city,state,zip_code")] customers customers)
        {
            if (ModelState.IsValid)
            {
                db.Entry(customers).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(customers);
        }

        //Display (Delete) action
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Query for a single customer and project into the ViewModel
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

            if(customerVM == null)
            {
                return HttpNotFound();
            }
            return View(customerVM);
        }

        //Display (DeleteConfirmed) action
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            customers customers = await db.customers.FindAsync(id);
            db.customers.Remove(customers);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
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
