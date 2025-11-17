using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WEBVANDAP.Models;

namespace WEBVANDAP.Controllers
{
    // Bảo vệ toàn bộ Controller: Chỉ Admin mới có thể quản lý Danh mục
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // --------------------------------------------------------
        // READ: Category/Index (Danh sách Danh mục)
        // --------------------------------------------------------
        public ActionResult Index()
        {
            // Trả về danh sách tất cả các Category
            return View(_context.Categories.ToList());
        }

        // --------------------------------------------------------
        // CREATE (GET): Category/Create (Hiển thị form)
        // --------------------------------------------------------
        public ActionResult Create()
        {
            return View();
        }

        // --------------------------------------------------------
        // CREATE (POST): Category/Create (Lưu dữ liệu)
        // --------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            // Nếu Model không hợp lệ, trả về form cùng với dữ liệu đã nhập
            return View(category);
        }

        // --------------------------------------------------------
        // EDIT (GET): Category/Edit/5 (Hiển thị form chỉnh sửa)
        // --------------------------------------------------------
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = _context.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // --------------------------------------------------------
        // EDIT (POST): Category/Edit (Lưu dữ liệu chỉnh sửa)
        // --------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                // Đánh dấu đối tượng là đã bị chỉnh sửa
                _context.Entry(category).State = EntityState.Modified;
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(category);
        }
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = _context.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // --------------------------------------------------------
        // DELETE (GET): Category/Delete/5 (Trang xác nhận xóa)
        // --------------------------------------------------------
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = _context.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // --------------------------------------------------------
        // DELETE (POST): Category/Delete/5 (Thực hiện xóa)
        // --------------------------------------------------------
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Kiểm tra xem có sản phẩm nào đang sử dụng CategoryId này không
            bool hasProducts = _context.Products.Any(p => p.CategoryId == id);

            if (hasProducts)
            {
                // Thêm thông báo lỗi vào ModelState để hiển thị trên View Delete.cshtml
                ModelState.AddModelError(string.Empty, "Không thể xóa danh mục này vì vẫn còn sản phẩm thuộc về nó.");

                // Cần tải lại Category để trả về View Delete.cshtml
                Category category = _context.Categories.Find(id);
                return View("Delete", category);
            }

            // Nếu không có sản phẩm liên quan, thực hiện xóa
            Category categoryToDelete = _context.Categories.Find(id);
            _context.Categories.Remove(categoryToDelete);
            _context.SaveChanges();
            return RedirectToAction("Index");
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