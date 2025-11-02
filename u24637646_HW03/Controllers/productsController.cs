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
    public class productsController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        private void PopulateDropdowns(products product = null)
        {
            ViewBag.brand_id = new SelectList(db.brands, "brand_id", "brand_name", product?.brand_id);
            ViewBag.category_id = new SelectList(db.categories, "category_id", "category_name", product?.category_id);
        }

        // --------------------------------------------------------------------------------
        // ACTION: Index (Modified for ListIndex)
        // --------------------------------------------------------------------------------
        public async Task<ActionResult> Index()
        {
            var productQuery = db.products
                .Select(p => new ProductViewModel
                {
                    product_id = p.product_id,
                    product_name = p.product_name,
                    model_year = p.model_year,
                    list_price = p.list_price,
                    brand_name = p.brands.brand_name,
                    category_name = p.categories.category_name,
                    TotalStock = p.stocks.Sum(s => (int?)s.quantity) ?? 0,
                    ListIndex = 0 // Placeholder
                });

            var productList = await productQuery.OrderBy(p => p.product_id).ToListAsync();

            // ⭐ Assign ListIndex based on position in the final list
            for (int i = 0; i < productList.Count; i++)
            {
                productList[i].ListIndex = i + 1;
            }

            return View(productList);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Edit (GET)
        // --------------------------------------------------------------------------------
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            products products = await db.products.FindAsync(id);
            if (products == null) return HttpNotFound();

            PopulateDropdowns(products);
            return PartialView("_EditPartial", products);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Edit (POST)
        // --------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "product_id,product_name,brand_id,category_id,model_year,list_price")] products products)
        {
            if (ModelState.IsValid)
            {
                db.Entry(products).State = EntityState.Modified;
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Product **{products.product_name}** updated successfully!";

                // ⭐ CORRECTED REDIRECT: Redirect to Home/Index
                return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
            }

            PopulateDropdowns(products);
            Response.StatusCode = 200;
            return PartialView("_EditPartial", products);
        }

        // --------------------------------------------------------------------------------
        // ACTION: AJAX Delete (POST)
        // --------------------------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            products product = await db.products.FindAsync(id);
            if (product == null) return Json(new { success = false, message = "Record not found." });

            string productName = product.product_name;

            try
            {
                db.products.Remove(product);
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Product **{productName}** deleted successfully!";

                // ⭐ CORRECTED REDIRECT: Redirect to Home/Index
                return Json(new { success = true, redirectUrl = Url.Action("Maintain", "Home") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed (Products): {ex.Message}" });
            }
        }

        // --------------------------------------------------------------------------------
        // Standard View Actions (Details, Create, Delete GET)
        // --------------------------------------------------------------------------------
        public async Task<ActionResult> Details(int? id)
        {
            // ... (Standard Details logic using ProductViewModel) ...
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var productVM = await db.products
                .Where(p => p.product_id == id)
                .Select(p => new ProductViewModel
                {
                    product_id = p.product_id,
                    product_name = p.product_name,
                    model_year = p.model_year,
                    list_price = p.list_price,
                    brand_name = p.brands.brand_name,
                    category_name = p.categories.category_name,
                    TotalStock = p.stocks.Sum(s => (int?)s.quantity) ?? 0
                }).FirstOrDefaultAsync();
            if (productVM == null) return HttpNotFound();
            return View(productVM);
        }

        public ActionResult Create() { PopulateDropdowns(); return View(); }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "product_id,product_name,brand_id,category_id,model_year,list_price")] products products)
        {
            if (ModelState.IsValid) { db.products.Add(products); await db.SaveChangesAsync(); return RedirectToAction("Index"); }
            PopulateDropdowns(products); return View(products);
        }

        public async Task<ActionResult> Delete(int? id)
        {
            // ... (Standard Delete GET logic using ProductViewModel) ...
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var productVM = await db.products
                .Where(p => p.product_id == id)
                .Select(p => new ProductViewModel
                {
                    product_id = p.product_id,
                    product_name = p.product_name,
                    model_year = p.model_year,
                    list_price = p.list_price,
                    brand_name = p.brands.brand_name,
                    category_name = p.categories.category_name,
                    TotalStock = p.stocks.Sum(s => (int?)s.quantity) ?? 0
                }).FirstOrDefaultAsync();
            if (productVM == null) return HttpNotFound();
            return View(productVM);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StandardDeleteConfirmed(int id)
        {
            products products = await db.products.FindAsync(id);
            db.products.Remove(products); await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}