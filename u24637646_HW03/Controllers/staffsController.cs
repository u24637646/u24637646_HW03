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

        // Display (Index) action using the StaffViewModel
        public async Task<ActionResult> Index()
        {
            // 1. Building the Async LINQ query
            var staffQuery = db.staffs.Include(s => s.stores).Include(s => s.staffs2).
                            // 2. Converting to the ViewModel
                            Select(s => new ViewModels.StaffViewModel
                            {
                                staff_id = s.staff_id,
                                first_name = s.first_name,
                                last_name = s.last_name,
                                email = s.email,
                                phone = s.phone,
                                active = s.active,
                                // 3. Replacing foreign keys with mapped values
                                store_name = s.stores.store_name,
                                manager_name = s.manager_id == null ? "No Manager" : s.staffs2.first_name + " " + s.staffs2.last_name
                            });
            // 4. Executing the query and getting the results
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

            // Query for a single staff member and project into the ViewModel
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

        // GET: staffs/Create
        public ActionResult Create()
        {
            ViewBag.manager_id = new SelectList(db.staffs, "staff_id", "first_name");
            ViewBag.store_id = new SelectList(db.stores, "store_id", "store_name");
            return View();
        }

        // POST: staffs/Create
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

            // If validation fails, repopulate ViewBags and return the view.
            ViewBag.manager_id = new SelectList(db.staffs, "staff_id", "first_name", staffs.manager_id);
            ViewBag.store_id = new SelectList(db.stores, "store_id", "store_name", staffs.store_id);
            return View(staffs);
        }

        // Display (Edit) action
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
            // Repopulate ViewBags
            ViewBag.manager_id = new SelectList(db.staffs, "staff_id", "first_name", staffs.manager_id);
            ViewBag.store_id = new SelectList(db.stores, "store_id", "store_name", staffs.store_id);
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
            // Repopulate ViewBags
            ViewBag.manager_id = new SelectList(db.staffs, "staff_id", "first_name", staffs.manager_id);
            ViewBag.store_id = new SelectList(db.stores, "store_id", "store_name", staffs.store_id);
            return View(staffs);
        }

        // Display (Delete) action
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Query for a single staff member and project into the ViewModel for display
            var staffVM = await db.staffs
                .Where(s => s.staff_id == id)
                .Include(s => s.stores)
                .Include(s => s.staffs2) // Manager relationship
                .Select(s => new StaffViewModel // Projecting to the ViewModel
                {
                    staff_id = s.staff_id,
                    first_name = s.first_name,
                    last_name = s.last_name,
                    email = s.email,
                    phone = s.phone,
                    active = s.active,
                    // Projection of foreign keys:
                    store_name = s.stores.store_name,
                    manager_name = s.manager_id == null ? "No Manager" : s.staffs2.first_name + " " + s.staffs2.last_name
                })
                .FirstOrDefaultAsync();

            if (staffVM == null)
            {
                return HttpNotFound();
            }
            // Pass the projected View Model to the Delete View
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
