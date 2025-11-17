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
    public class ProductController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // Phương thức nội bộ để lưu file ảnh và cập nhật DB (giữ nguyên)
        private void SaveProductImages(int productId, HttpPostedFileBase[] images)
        {
            if (images == null || images.All(f => f == null || f.ContentLength == 0)) return;

            string folderPath = Server.MapPath("~/Content/ProductImages/");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            foreach (var imageFile in images.Where(f => f != null && f.ContentLength > 0))
            {
                string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                string extension = Path.GetExtension(imageFile.FileName);
                fileName = fileName + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                string uploadPath = Path.Combine(folderPath, fileName);
                imageFile.SaveAs(uploadPath);

                var productImage = new ProductImage
                {
                    ProductId = productId,
                    Url = "~/Content/ProductImages/" + fileName
                };
                _context.ProductImages.Add(productImage);
            }
            _context.SaveChanges();
        }

        // Phương thức nội bộ để xóa file ảnh vật lý trên server (giữ nguyên)
        private void DeleteImageFile(string url)
        {
            try
            {
                string physicalPath = Server.MapPath(url);
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xóa file: " + ex.Message);
            }
        }


        // --------------------------------------------------------
        // CHỨC NĂNG CÔNG CỘNG (Giữ nguyên)
        // --------------------------------------------------------
        public ActionResult Index()
        {
            var products = _context.Products
                                   .Include(p => p.Category)
                                   .Include(p => p.Brand)
                                   .ToList();
            return View(products);
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Product product = _context.Products
                                     .Include(p => p.ProductImages)
                                     .FirstOrDefault(p => p.Id == id);

            if (product == null) return HttpNotFound();
            return View(product);
        }

        // --------------------------------------------------------
        // CHỨC NĂNG QUẢN LÝ (Admin)
        // --------------------------------------------------------

        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "Name");
            return View(new Product());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product product, HttpPostedFileBase[] images)
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "Name", product.BrandId);

            if (string.IsNullOrWhiteSpace(product.Slug) && !string.IsNullOrWhiteSpace(product.Name))
            {
                product.Slug = product.Name.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString("N").Substring(0, 5);
            }
            product.CreatedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Products.Add(product);
                    _context.SaveChanges();
                    int newProductId = product.Id;

                    this.SaveProductImages(newProductId, images);

                    TempData["SuccessMessage"] = "Tạo sản phẩm mới thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Đã xảy ra lỗi hệ thống: " + ex.Message;
                    return View(product);
                }
            }
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Product product = _context.Products
                                     .Include(p => p.ProductImages)
                                     .FirstOrDefault(p => p.Id == id);
            if (product == null) return HttpNotFound();

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "Name", product.BrandId);

            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product, HttpPostedFileBase[] newImages)
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "Name", product.BrandId);

            if (ModelState.IsValid)
            {
                try
                {
                    var originalValues = _context.Products
                        .AsNoTracking()
                        .Where(p => p.Id == product.Id)
                        .Select(p => new { p.Slug, p.CreatedAt })
                        .FirstOrDefault();

                    if (originalValues == null)
                    {
                        TempData["Error"] = "Không tìm thấy sản phẩm cần chỉnh sửa.";
                        return HttpNotFound();
                    }

                    product.Slug = originalValues.Slug;
                    product.CreatedAt = originalValues.CreatedAt;

                    _context.Entry(product).State = EntityState.Modified;

                    this.SaveProductImages(product.Id, newImages);

                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Đã xảy ra lỗi hệ thống: " + ex.Message;
                    return View(product);
                }
            }

            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Product product = _context.Products
                                     .Include(p => p.ProductImages)
                                     .FirstOrDefault(p => p.Id == id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = _context.Products
                                     .Include(p => p.ProductImages)
                                     .FirstOrDefault(p => p.Id == id);

            if (product != null)
            {
                // Xóa tất cả các file ảnh vật lý trên server và trong DB
                foreach (var image in product.ProductImages.ToList())
                {
                    this.DeleteImageFile(image.Url);
                    _context.ProductImages.Remove(image);
                }

                // Xóa các OrderItem, CartItem liên quan 
                _context.OrderItems.RemoveRange(_context.OrderItems.Where(oi => oi.ProductId == id));
                _context.CartItems.RemoveRange(_context.CartItems.Where(ci => ci.ProductId == id));

                // Xóa sản phẩm chính
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            TempData["SuccessMessage"] = "Đã xóa sản phẩm thành công!";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public JsonResult DeleteImage(int imageId)
        {
            var image = _context.ProductImages.Find(imageId);
            if (image == null) return Json(new { success = false, message = "Không tìm thấy ảnh" });

            this.DeleteImageFile(image.Url);
            _context.ProductImages.Remove(image);
            _context.SaveChanges();

            return Json(new { success = true, productId = image.ProductId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}