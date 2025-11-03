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
    public class staffsController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // Helper method to populate dropdown lists for forms
        private void PopulateDropdowns(staffs staff = null)
        {
            // Create full name list for manager dropdown
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

            ViewBag.store_id = new SelectList(
                db.stores.ToList(),
                "store_id",
                "store_name",
                staff?.store_id
            );
        }

        // GET: staffs/Index - Display list of all staff members
        public async Task<ActionResult> Index()
        {
            var staffQuery = db.staffs
                .Include(s => s.stores)
                .Include(s => s.staffs2)
                .Select(s => new StaffViewModel
                {
                    staff_id = s.staff_id,
                    first_name = s.first_name,
                    last_name = s.last_name,
                    email = s.email,
                    phone = (s.phone == null || s.phone == "") ? "-" : s.phone,
                    active = s.active,
                    store_name = s.stores.store_name,
                    manager_name = s.manager_id == null ? "No Manager" : s.staffs2.first_name + " " + s.staffs2.last_name
                });

            var staffList = await staffQuery.OrderBy(s => s.staff_id).ToListAsync();
            return View(staffList);
        }

        // ******************** AJAX MODAL ACTIONS (ADDED) ********************

        // Load create form in modal (AJAX GET)
        [HttpGet]
        public ActionResult CreatePartial()
        {
            PopulateDropdowns();
            return PartialView("_CreatePartial", new staffs());
        }

        // Handle staff creation via AJAX (AJAX POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreatePartial([Bind(Include = "first_name,last_name,email,phone,active,store_id,manager_id")] staffs staff)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (staff.active == 0) staff.active = 0;
                    db.staffs.Add(staff);
                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Staff member {staff.first_name} {staff.last_name} created successfully!";

                    // Success: Return JSON indicating redirection is needed
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
                }
                catch (Exception ex)
                {
                    Response.StatusCode = 200;
                    return Json(new { success = false, message = "Unable to create staff member. Database error: " + ex.Message });
                }
            }

            // Failure: Return the partial view content with validation errors
            PopulateDropdowns(staff);
            Response.StatusCode = 200;
            return PartialView("_CreatePartial", staff);
        }

        // ******************** Standard (Non-AJAX) Actions ********************

        // GET: staffs/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var staffVM = await db.staffs
                .Where(s => s.staff_id == id)
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
                    manager_name = s.manager_id == null ? "No Manager" : s.staffs2.first_name + " " + s.staffs2.last_name
                }).FirstOrDefaultAsync();

            if (staffVM == null) return HttpNotFound();
            return View(staffVM);
        }

        // GET: staffs/Create
        public ActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: staffs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "first_name,last_name,email,phone,active,store_id,manager_id")] staffs staffs)
        {
            if (ModelState.IsValid)
            {
                if (staffs.active == 0) staffs.active = 0;

                db.staffs.Add(staffs);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            PopulateDropdowns(staffs);
            return View(staffs);
        }

        // GET: staffs/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            staffs staffs = await db.staffs.FindAsync(id);
            if (staffs == null) return HttpNotFound();

            PopulateDropdowns(staffs);
            return View(staffs);
        }

        // POST: staffs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staffs staffs)
        {
            if (ModelState.IsValid)
            {
                if (staffs.active == 0) staffs.active = 0;

                db.Entry(staffs).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            PopulateDropdowns(staffs);
            return View(staffs);
        }

        // GET: staffs/Delete/5 
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var staffVM = await db.staffs
                .Where(s => s.staff_id == id)
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
                    manager_name = s.manager_id == null ? "No Manager" : s.staffs2.first_name + " " + s.staffs2.last_name
                }).FirstOrDefaultAsync();

            if (staffVM == null) return HttpNotFound();
            return View(staffVM);
        }

        // POST: staffs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StandardDeleteConfirmed(int id)
        {
            staffs staffs = await db.staffs.FindAsync(id);
            if (staffs == null) return HttpNotFound();

            try
            {
                db.staffs.Remove(staffs);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Deletion failed. This staff member may have related data (e.g., orders, sub-staff) that must be deleted first. Details: {ex.Message}";
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