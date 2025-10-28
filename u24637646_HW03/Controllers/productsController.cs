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
using u24637646_HW03.ViewModels; // <-- ADDED: Include the ViewModels namespace

namespace u24637646_HW03.Controllers
{
    public class productsController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // GET: products
        public async Task<ActionResult> Index()
        {
            // 1. Building the Async LINQ query
            var productQuery = db.products
                // 2. Converting to the ViewModel with projected and calculated fields
                .Select(p => new ProductViewModel
                {
                    product_id = p.product_id,
                    product_name = p.product_name,
                    model_year = p.model_year,
                    list_price = p.list_price,

                    // Mapped properties that replace the foreign keys using simple navigation
                    brand_name = p.brands.brand_name,
                    category_name = p.categories.category_name,

                    // 3. Calculated property: Total Stock (using a robust subquery for summation)
                    // The .Sum(s => (int?)s.quantity) ?? 0 handles null stock safely.
                    TotalStock = p.stocks.Sum(s => (int?)s.quantity) ?? 0
                });

            // 4. Executing the query and getting the results
            var productList = await productQuery.ToListAsync();
            return View(productList); // Returns List<ProductViewModel>
        }

        //Display (Details) action
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            // Query for a single product and project into the ViewModel
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

                    // Calculate TotalStock for this specific product using a subquery
                    TotalStock = p.stocks.Sum(s => (int?)s.quantity) ?? 0
                })
                .FirstOrDefaultAsync();

            if (productVM == null)
            {
                return HttpNotFound();
            }
            return View(productVM);
        }

        // GET: products/Create
        public ActionResult Create()
        {
            ViewBag.brand_id = new SelectList(db.brands, "brand_id", "brand_name");
            ViewBag.category_id = new SelectList(db.categories, "category_id", "category_name");
            return View();
        }

        // POST: products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "product_id,product_name,brand_id,category_id,model_year,list_price")] products products)
        {
            if (ModelState.IsValid)
            {
                db.products.Add(products);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.brand_id = new SelectList(db.brands, "brand_id", "brand_name", products.brand_id);
            ViewBag.category_id = new SelectList(db.categories, "category_id", "category_name", products.category_id);
            return View(products);
        }

        // GET: products/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            products products = await db.products.FindAsync(id);
            if (products == null)
            {
                return HttpNotFound();
            }
            ViewBag.brand_id = new SelectList(db.brands, "brand_id", "brand_name", products.brand_id);
            ViewBag.category_id = new SelectList(db.categories, "category_id", "category_name", products.category_id);
            return View(products);
        }

        // POST: products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "product_id,product_name,brand_id,category_id,model_year,list_price")] products products)
        {
            if (ModelState.IsValid)
            {
                db.Entry(products).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.brand_id = new SelectList(db.brands, "brand_id", "brand_name", products.brand_id);
            ViewBag.category_id = new SelectList(db.categories, "category_id", "category_name", products.category_id);
            return View(products);
        }

        //Display (Delete) action
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Query for a single product and project into the ViewModel for display
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

                    // Calculate TotalStock for this specific product using a subquery
                    TotalStock = p.stocks.Sum(s => (int?)s.quantity) ?? 0
                })
                .FirstOrDefaultAsync();

            if (productVM == null)
            {
                return HttpNotFound();
            }
            return View(productVM);
        }

        //Display (DeleteConfirmed) action
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            products products = await db.products.FindAsync(id);
            db.products.Remove(products);
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