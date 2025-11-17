using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WEBVANDAP.Models;
using System.Collections.Generic;

namespace WEBVANDAP.Controllers
{
    // Bảo vệ toàn bộ Controller: Chỉ Admin mới có thể quản lý Thương hiệu
    [Authorize(Roles = "Admin")]
    public class BrandController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // Phương thức hỗ trợ cho Dropdownlist
        private void PopulateCategoriesDropdown(object selectedCategory = null)
        {
            var categoriesQuery = _context.Categories.OrderBy(c => c.Name);
            // Tạo SelectList cho ViewBag
            ViewBag.CategoryId = new SelectList(categoriesQuery, "Id", "Name", selectedCategory);
        }

        // --------------------------------------------------------
        // READ: Brand/Index (Danh sách Thương hiệu)
        // --------------------------------------------------------
        public ActionResult Index()
        {
            var brands = _context.Brands.ToList();
            return View(brands);
        }

        // --------------------------------------------------------
        // CREATE (GET): Brand/Create (Hiển thị form)
        // --------------------------------------------------------
        public ActionResult Create()
        {
            PopulateCategoriesDropdown();
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

            // Nếu validation thất bại, tải lại danh sách Category và trả về View
            PopulateCategoriesDropdown(brand.CategoryId);
            return View(brand);
        }

        // --------------------------------------------------------
        // EDIT (GET): Brand/Edit/5 (Hiển thị form chỉnh sửa)
        // --------------------------------------------------------
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Brand brand = _context.Brands.Find(id);
            if (brand == null)
            {
                return HttpNotFound();
            }
            // Tải lại dropdown với Category đang được chọn
            PopulateCategoriesDropdown(brand.CategoryId);
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

            // Nếu validation thất bại, tải lại danh sách Category và trả về View
            PopulateCategoriesDropdown(brand.CategoryId);
            return View(brand);
        }

        // --------------------------------------------------------
        // DELETE (GET): Brand/Delete/5 (Trang xác nhận xóa)
        // --------------------------------------------------------
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Brand brand = _context.Brands.FirstOrDefault(b => b.Id == id);
            if (brand == null)
            {
                return HttpNotFound();
            }
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
                // ... (Logic chặn xóa và trả về View) ...
                Brand brand = _context.Brands.Find(id); // Vẫn phải Find để trả về View
                                                        // ... (Logic trả về View Delete)
                return View("Delete", brand);
            }

            // 1. TÌM KIẾM ĐỐI TƯỢNG
            Brand brandToDelete = _context.Brands.Find(id);

            // 2. BỔ SUNG KIỂM TRA NULL (QUAN TRỌNG)
            if (brandToDelete == null)
            {
                // Nếu không tìm thấy (ví dụ: đã xóa), chuyển hướng về Index thay vì gây lỗi 500
                return RedirectToAction("Index");
            }

            // 3. THỰC HIỆN XÓA
            _context.Brands.Remove(brandToDelete);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}