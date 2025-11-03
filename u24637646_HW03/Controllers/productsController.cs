using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using u24637646_HW03.Models;
using u24637646_HW03.ViewModels;
using System.Collections.Generic;

namespace u24637646_HW03.Controllers
{
    public class productsController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // Helper to populate Brand and Category SelectLists for Create/Edit views
        private void PopulateDropdowns(object selectedBrand = null, object selectedCategory = null)
        {
            // Brands
            var brandsQuery = db.brands
                .OrderBy(b => b.brand_name)
                .Select(b => new SelectListItem
                {
                    Value = b.brand_id.ToString(),
                    Text = b.brand_name
                });
            ViewBag.brand_id = new SelectList(brandsQuery, "Value", "Text", selectedBrand);

            // Categories
            var categoriesQuery = db.categories
                .OrderBy(c => c.category_name)
                .Select(c => new SelectListItem
                {
                    Value = c.category_id.ToString(),
                    Text = c.category_name
                });
            ViewBag.category_id = new SelectList(categoriesQuery, "Value", "Text", selectedCategory);
        }

        // GET: products
        public async Task<ActionResult> Index()
        {
            // Projection to ProductViewModel, including Brand and Category names
            var productQuery = db.products
                .Include(p => p.brands)
                .Include(p => p.categories)
                .Select(p => new ProductViewModel
                {
                    product_id = p.product_id,
                    product_name = p.product_name,
                    brand_name = p.brands.brand_name,
                    category_name = p.categories.category_name,
                    model_year = p.model_year,
                    list_price = p.list_price,
                    ListIndex = 0 // Placeholder
                });

            var productList = await productQuery.OrderBy(p => p.product_id).ToListAsync();

            // Assign list indices based on position in the final list
            for (int i = 0; i < productList.Count; i++)
            {
                productList[i].ListIndex = i + 1;
            }

            return View(productList);
        }

        // GET: products/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Fetch and map to ViewModel
            var productVM = await db.products
                .Where(p => p.product_id == id)
                .Include(p => p.brands)
                .Include(p => p.categories)
                .Select(p => new ProductViewModel
                {
                    product_id = p.product_id,
                    product_name = p.product_name,
                    brand_name = p.brands.brand_name,
                    category_name = p.categories.category_name,
                    model_year = p.model_year,
                    list_price = p.list_price
                }).FirstOrDefaultAsync();

            if (productVM == null) return HttpNotFound();
            return View(productVM);
        }

        // GET: products/Create
        public ActionResult Create()
        {
            PopulateDropdowns();
            // Return an empty products model for the view
            return View(new products());
        }

        // POST: products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "product_name,brand_id,category_id,model_year,list_price")] products product)
        {
            if (ModelState.IsValid)
            {
                db.products.Add(product);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            // If validation fails, repopulate dropdowns and return to view
            PopulateDropdowns(product.brand_id, product.category_id);
            return View(product);
        }

        // POST: products/CreatePartial - For Modal/AJAX Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreatePartial([Bind(Include = "product_name,brand_id,category_id,model_year,list_price")] products product)
        {
            if (ModelState.IsValid)
            {
                db.products.Add(product);
                await db.SaveChangesAsync();
                // Return success signal (e.g., an empty JSON object or success message)
                return Json(new { success = true, productId = product.product_id });
            }

            // If validation fails, return the partial view with model errors
            PopulateDropdowns(product.brand_id, product.category_id);
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return PartialView("_CreatePartial", product);
        }


        // GET: products/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            products product = await db.products.FindAsync(id);
            if (product == null) return HttpNotFound();

            PopulateDropdowns(product.brand_id, product.category_id);
            return View(product);
        }

        // POST: products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "product_id,product_name,brand_id,category_id,model_year,list_price")] products product)
        {
            if (ModelState.IsValid)
            {
                db.Entry(product).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            // If validation fails, repopulate dropdowns and return to view
            PopulateDropdowns(product.brand_id, product.category_id);
            return View(product);
        }

        // POST: products/EditPartialPost - For Modal/AJAX Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditPartialPost([Bind(Include = "product_id,product_name,brand_id,category_id,model_year,list_price")] products product)
        {
            if (ModelState.IsValid)
            {
                db.Entry(product).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return Json(new { success = true });
            }

            // If validation fails, return the partial view with model errors
            PopulateDropdowns(product.brand_id, product.category_id);
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return PartialView("_EditPartial", product);
        }

        // GET: products/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Fetch and map to ViewModel for display in Delete confirmation
            var productVM = await db.products
                .Where(p => p.product_id == id)
                .Include(p => p.brands)
                .Include(p => p.categories)
                .Select(p => new ProductViewModel
                {
                    product_id = p.product_id,
                    product_name = p.product_name,
                    brand_name = p.brands.brand_name,
                    category_name = p.categories.category_name,
                    model_year = p.model_year,
                    list_price = p.list_price
                }).FirstOrDefaultAsync();

            if (productVM == null) return HttpNotFound();
            return View(productVM);
        }

        // POST: products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StandardDeleteConfirmed(int id)
        {
            try
            {
                products product = await db.products.FindAsync(id);
                if (product == null) return HttpNotFound();

                db.products.Remove(product);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // If deletion fails due to foreign key constraints, reload the Delete page with an error.
                TempData["ErrorMessage"] = $"Deletion failed. This product may have related data (e.g., stock or order items) that must be deleted first. Details: {ex.Message}";
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
