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

        private void PopulateDropdowns(staffs staff = null)
        {
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

        // --------------------------------------------------------------------------------
        // ACTION: Index (Modified for ListIndex)
        // --------------------------------------------------------------------------------
        public async Task<ActionResult> Index()
        {
            var staffQuery = db.staffs.Include(s => s.stores).Include(s => s.staffs2)
                                     .Select(s => new ViewModels.StaffViewModel
                                     {
                                         staff_id = s.staff_id,
                                         first_name = s.first_name,
                                         last_name = s.last_name,
                                         email = s.email,
                                         phone = s.phone,
                                         active = s.active,
                                         store_name = s.stores.store_name,
                                         manager_name = s.manager_id == null ? "No Manager" : s.staffs2.first_name + " " + s.staffs2.last_name,
                                         ListIndex = 0 // Placeholder
                                     });

            var staffList = await staffQuery.OrderBy(s => s.staff_id).ToListAsync();

            // ⭐ Assign ListIndex based on position in the final list
            for (int i = 0; i < staffList.Count; i++)
            {
                staffList[i].ListIndex = i + 1;
            }

            return View(staffList);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Create (GET)
        // --------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult CreatePartial()
        {
            PopulateDropdowns();
            return PartialView("_CreatePartial", new staffs());
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Create (POST)
        // --------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreatePartial([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staffs staffs)
        {
            if (ModelState.IsValid)
            {
                db.staffs.Add(staffs);
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Staff member **{staffs.first_name} {staffs.last_name}** created successfully!";

                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
            }

            PopulateDropdowns(staffs);
            Response.StatusCode = 200;
            return PartialView("_CreatePartial", staffs);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Edit (GET)
        // --------------------------------------------------------------------------------
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            staffs staffs = await db.staffs.FindAsync(id);
            if (staffs == null) return HttpNotFound();

            PopulateDropdowns(staffs);
            return PartialView("_EditPartial", staffs);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Edit (POST)
        // --------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staffs staffs)
        {
            if (ModelState.IsValid)
            {
                db.Entry(staffs).State = EntityState.Modified;
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Staff member **{staffs.first_name} {staffs.last_name}** updated successfully!";

                // ⭐ CORRECTED REDIRECT: Redirect to Home/Index
                return Json(new { success = true, redirectUrl = Url.Action("Home", "Maintain") });
            }

            PopulateDropdowns(staffs);
            Response.StatusCode = 200;
            return PartialView("_EditPartial", staffs);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Delete (POST)
        // --------------------------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            staffs staff = await db.staffs.FindAsync(id);
            if (staff == null) return Json(new { success = false, message = "Record not found." });

            string staffName = $"{staff.first_name} {staff.last_name}";

            try
            {
                db.staffs.Remove(staff);
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Staff member **{staffName}** deleted successfully!";

                // ⭐ CORRECTED REDIRECT: Redirect to Home/Index
                return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
            }
            catch (Exception ex)
            {
                // Note: The message will be displayed by the AJAX handler, not TempData
                return Json(new { success = false, message = $"Deletion failed (Staffs): {ex.Message}" });
            }
        }

        // --------------------------------------------------------------------------------
        // Standard View Actions (Details, Create, Delete GET)
        // --------------------------------------------------------------------------------
        public async Task<ActionResult> Details(int? id)
        {
            // ... (Standard Details logic using StaffViewModel) ...
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

        public ActionResult Create() { PopulateDropdowns(); return View(); }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staffs staffs)
        {
            if (ModelState.IsValid) { db.staffs.Add(staffs); await db.SaveChangesAsync(); return RedirectToAction("Index"); }
            PopulateDropdowns(staffs); return View(staffs);
        }

        public async Task<ActionResult> Delete(int? id)
        {
            // ... (Standard Delete GET logic using StaffViewModel) ...
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StandardDeleteConfirmed(int id)
        {
            staffs staffs = await db.staffs.FindAsync(id);
            db.staffs.Remove(staffs); await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}