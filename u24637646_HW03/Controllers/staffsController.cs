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
    public class staffsController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // --------------------------------------------------------------------------------
        // HELPER METHOD: To fix the ViewData exception on POST failure
        // --------------------------------------------------------------------------------
        private void PopulateDropdowns(staffs staff = null)
        {
            // Managers: Displaying only the first name for simplicity, can be changed.
            // Using a query to get staff members who could be managers
            var managers = db.staffs.Select(s => new
            {
                s.staff_id,
                full_name = s.first_name + " " + s.last_name
            }).ToList();

            ViewBag.manager_id = new SelectList(
                managers,
                "staff_id",
                "full_name",
                staff?.manager_id
            );

            // Stores
            ViewBag.store_id = new SelectList(
                db.stores.ToList(),
                "store_id",
                "store_name",
                staff?.store_id
            );
        }

        // --------------------------------------------------------------------------------
        // AJAX Create (GET)
        // --------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult CreatePartial()
        {
            // Call helper to populate dropdowns
            PopulateDropdowns();

            // Return the partial view (the form)
            return PartialView("_CreatePartial", new staffs());
        }

        // --------------------------------------------------------------------------------
        // AJAX Create (POST)
        // --------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreatePartial([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staffs staffs)
        {
            if (ModelState.IsValid)
            {
                db.staffs.Add(staffs);
                await db.SaveChangesAsync();

                // Set TempData for the Home/Index view to display the success message
                TempData["SuccessMessage"] = $"Staff member **{staffs.first_name} {staffs.last_name}** created successfully!";

                // ⭐ CRITICAL FIX: Return JSON on SUCCESS for AJAX clients
                return Json(new
                {
                    success = true,
                    // Tell the client where to redirect to update the main dashboard
                    redirectUrl = Url.Action("Index", "Home")
                });
            }

            // ⭐ CRITICAL FIX: IF VALIDATION FAILS, RE-LOAD DROPDOWN DATA and return the partial view
            PopulateDropdowns(staffs);

            // Return the PartialView with the model and validation errors
            return PartialView("_CreatePartial", staffs);
        }

        // --------------------------------------------------------------------------------
        // Remaining Actions (Index, Details, Create, Edit, Delete, Dispose) are unchanged.
        // --------------------------------------------------------------------------------

        // Display (Index) action using the StaffViewModel
        public async Task<ActionResult> Index()
        {
            var staffQuery = db.staffs.Include(s => s.stores).Include(s => s.staffs2).
                            Select(s => new ViewModels.StaffViewModel
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
            var staffList = await staffQuery.ToListAsync();
            return View(staffList);
        }

        // Display (Details) action
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var staffVM = await db.staffs
                .Where(s => s.staff_id == id)
                .Include(s => s.stores)
                .Include(s => s.staffs2)
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

            if (staffVM == null)
            {
                return HttpNotFound();
            }
            return View(staffVM);
        }

        // GET: staffs/Create (Standard full-page Create)
        public ActionResult Create()
        {
            PopulateDropdowns(); // Using the helper for the full page view too
            return View();
        }

        // Set (Create) action (Standard full-page POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staffs staffs)
        {
            if (ModelState.IsValid)
            {
                db.staffs.Add(staffs);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            PopulateDropdowns(staffs); // Using the helper
            return View(staffs);
        }

        // Set (Edit) action
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            staffs staffs = await db.staffs.FindAsync(id);
            if (staffs == null)
            {
                return HttpNotFound();
            }
            PopulateDropdowns(staffs); // Using the helper
            return View(staffs);
        }

        // POST: staffs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staffs staffs)
        {
            if (ModelState.IsValid)
            {
                db.Entry(staffs).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            PopulateDropdowns(staffs); // Using the helper
            return View(staffs);
        }

        // Display (Delete) action
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var staffVM = await db.staffs
                .Where(s => s.staff_id == id)
                .Include(s => s.stores)
                .Include(s => s.staffs2)
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

            if (staffVM == null)
            {
                return HttpNotFound();
            }
            return View(staffVM);
        }

        // Display (DeleteConfirmed) action
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            staffs staffs = await db.staffs.FindAsync(id);
            db.staffs.Remove(staffs);
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