using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WEBVANDAP.Models;

namespace WEBVANDAP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // Lưu file ảnh và cập nhật DB
        private void SaveProductImages(int productId, HttpPostedFileBase[] images)
        {
            if (images == null || images.All(f => f == null || f.ContentLength == 0)) return;

            string folderPath = Server.MapPath("~/Content/ProductImages/");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            foreach (var imageFile in images.Where(f => f != null && f.ContentLength > 0))
            {
                string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                string extension = Path.GetExtension(imageFile.FileName);
                fileName = fileName + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                string uploadPath = Path.Combine(folderPath, fileName);
                imageFile.SaveAs(uploadPath);

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    Url = "~/Content/ProductImages/" + fileName
                });
            }
            _context.SaveChanges();
        }

        // Xóa file ảnh vật lý
        private void DeleteImageFile(string url)
        {
            try
            {
                string physicalPath = Server.MapPath(url);
                if (System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xóa file: " + ex.Message);
            }
        }

        // -----------------------
        // Danh sách sản phẩm
        // -----------------------
        public ActionResult Index()
        {
            var products = _context.Products
                                   .Include(p => p.Category)
                                   .Include(p => p.Brand)
                                   .ToList();
            return View(products);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = _context.Products
                                  .Include(p => p.ProductImages)
                                  .Include(p => p.Category)
                                  .Include(p => p.Brand)
                                  .FirstOrDefault(p => p.Id == id);

            if (product == null) return HttpNotFound();

            return View("Details", product); // View riêng cho admin: DetailsAdmin.cshtml
        }

        [AllowAnonymous]
        public ActionResult DetailUser(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = _context.Products.Include(p => p.ProductImages)
                                           .FirstOrDefault(p => p.Id == id);
            if (product == null) return HttpNotFound();

            return View("DetailsUser", product);
        }



        // -----------------------
        // Create Product
        // -----------------------
        public ActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.BrandId = new SelectList(new List<Brand>(), "Id", "Name"); // ban đầu trống
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product product, HttpPostedFileBase[] images)
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands.Where(b => b.CategoryId == product.CategoryId), "Id", "Name", product.BrandId);

            if (string.IsNullOrWhiteSpace(product.Slug) && !string.IsNullOrWhiteSpace(product.Name))
            {
                product.Slug = product.Name.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString("N").Substring(0, 5);
            }
            product.CreatedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                _context.SaveChanges();
                SaveProductImages(product.Id, images);

                TempData["SuccessMessage"] = "Tạo sản phẩm mới thành công!";
                return RedirectToAction("Index");
            }
            return View(product);
        }

        // -----------------------
        // Edit Product
        // -----------------------
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = _context.Products.Include(p => p.ProductImages).FirstOrDefault(p => p.Id == id);
            if (product == null) return HttpNotFound();

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands.Where(b => b.CategoryId == product.CategoryId), "Id", "Name", product.BrandId);

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product, HttpPostedFileBase[] newImages)
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands.Where(b => b.CategoryId == product.CategoryId), "Id", "Name", product.BrandId);

            if (ModelState.IsValid)
            {
                var originalValues = _context.Products.AsNoTracking()
                                       .Where(p => p.Id == product.Id)
                                       .Select(p => new { p.Slug, p.CreatedAt })
                                       .FirstOrDefault();
                if (originalValues == null) return HttpNotFound();

                product.Slug = originalValues.Slug;
                product.CreatedAt = originalValues.CreatedAt;

                _context.Entry(product).State = EntityState.Modified;
                SaveProductImages(product.Id, newImages);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            return View(product);
        }

        // -----------------------
        // Delete Product
        // -----------------------
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = _context.Products.Include(p => p.ProductImages).FirstOrDefault(p => p.Id == id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var product = _context.Products.Include(p => p.ProductImages).FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                foreach (var image in product.ProductImages.ToList())
                {
                    DeleteImageFile(image.Url);
                    _context.ProductImages.Remove(image);
                }
                _context.OrderItems.RemoveRange(_context.OrderItems.Where(oi => oi.ProductId == id));
                _context.CartItems.RemoveRange(_context.CartItems.Where(ci => ci.ProductId == id));
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            TempData["SuccessMessage"] = "Đã xóa sản phẩm thành công!";
            return RedirectToAction("Index");
        }

        // -----------------------
        // AJAX lấy Brand theo Category
        // -----------------------
        [HttpPost]
        public JsonResult GetBrandsByCategory(int categoryId)
        {
            var brands = _context.Brands
                                 .Where(b => b.CategoryId == categoryId)
                                 .Select(b => new { b.Id, b.Name })
                                 .ToList();
            return Json(brands);
        }

        [HttpPost]
        public JsonResult DeleteImage(int imageId)
        {
            var image = _context.ProductImages.Find(imageId);
            if (image == null) return Json(new { success = false });

            DeleteImageFile(image.Url);
            _context.ProductImages.Remove(image);
            _context.SaveChanges();

            return Json(new { success = true, productId = image.ProductId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
