using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WEBVANDAP.Models;

namespace WEBVANDAP.Controllers
{
    // Chỉ Admin mới được quản lý Thương hiệu
    [Authorize(Roles = "Admin")]
    public class BrandController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // --------------------------------------------------------
        // READ: Brand/Index (Danh sách Thương hiệu)
        // --------------------------------------------------------
        public ActionResult Index()
        {
            var brands = _context.Brands
                                 .Include(b => b.Category) // ✔ LOAD CATEGORY
                                 .ToList();

            return View(brands);
        }


        // --------------------------------------------------------
        // CREATE (GET): Brand/Create (Hiển thị form)
        // --------------------------------------------------------
        public ActionResult Create()
        {
            ViewBag.Categories = _context.Categories
                                         .Select(c => new { c.Id, c.Name })
                                         .ToList();
            return View();
        }


        // --------------------------------------------------------
        // CREATE (POST): Brand/Create (Lưu dữ liệu)
        // --------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Brand brand)
        {
            if (ModelState.IsValid)
            {
                _context.Brands.Add(brand);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            // Nếu ModelState lỗi thì load lại dropdown
            ViewBag.Categories = _context.Categories
                                         .Select(c => new { c.Id, c.Name })
                                         .ToList();

            return View(brand);
        }

        // --------------------------------------------------------
        // EDIT (GET): Brand/Edit/5 (Hiển thị form chỉnh sửa)
        // --------------------------------------------------------
        public ActionResult Edit(int id)
        {
            var brand = _context.Brands.Find(id);

            // Tạo danh sách danh mục cho dropdown, lấy từ bảng Categories
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", brand.CategoryId);

            return View(brand);
        }

        // --------------------------------------------------------
        // EDIT (POST): Brand/Edit (Lưu dữ liệu chỉnh sửa)
        // --------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Brand brand)
        {
            if (ModelState.IsValid)
            {
                _context.Entry(brand).State = EntityState.Modified;
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            // Không còn PopulateCategoriesDropdown
            return View(brand);
        }

        // --------------------------------------------------------
        // DELETE (GET): Brand/Delete/5 (Trang xác nhận xóa)
        // --------------------------------------------------------
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Brand brand = _context.Brands.FirstOrDefault(b => b.Id == id);
            if (brand == null)
                return HttpNotFound();

            return View(brand);
        }

        // --------------------------------------------------------
        // DELETE (POST): Brand/Delete/5 (Thực hiện xóa)
        // --------------------------------------------------------
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Kiểm tra ràng buộc khóa ngoại (Product -> Brand)
            bool hasProducts = _context.Products.Any(p => p.BrandId == id);

            if (hasProducts)
            {
                // Nếu còn sản phẩm dùng brand này thì không cho xóa
                Brand brand = _context.Brands.Find(id);
                // Có thể set TempData báo lỗi ở đây nếu muốn
                return View("Delete", brand);
            }

            // 1. Tìm brand
            Brand brandToDelete = _context.Brands.Find(id);

            // 2. Nếu null thì quay về Index luôn
            if (brandToDelete == null)
                return RedirectToAction("Index");

            // 3. Xóa
            _context.Brands.Remove(brandToDelete);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // Giải phóng context
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
