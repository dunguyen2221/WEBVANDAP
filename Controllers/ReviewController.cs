using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WEBVANDAP.Models;

namespace WEBVANDAP.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // --------------------------------------------------------
        // 1. ADMIN: Xem danh sách đánh giá (Quản lý)
        // --------------------------------------------------------
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            var reviews = _context.Reviews
                                  .Include(r => r.Product)
                                  .Include(r => r.AspNetUser)
                                  .OrderByDescending(r => r.CreatedAt) // Mới nhất lên đầu
                                  .ToList();
            return View(reviews); // Cần tạo View Admin Index
        }

        // --------------------------------------------------------
        // 2. ADMIN: Xóa đánh giá (Spam/Vi phạm)
        // --------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var review = _context.Reviews.Find(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa đánh giá thành công.";
            }
            return RedirectToAction("Index");
        }

        // --------------------------------------------------------
        // 3. KHÁCH HÀNG: Gửi đánh giá mới (POST từ trang Product Details)
        // --------------------------------------------------------
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int productId, int rating, string content)
        {
            // SỬ DỤNG SESSION ĐỂ ĐỒNG BỘ VỚI HỆ THỐNG CỦA BẠN
            string userId = Session["UserId"] as string;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("DangNhap", "Account");
            }

            // Kiểm tra dữ liệu hợp lệ
            if (rating < 1 || rating > 5) rating = 5; // Mặc định 5 sao nếu sai
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Nội dung đánh giá không được để trống.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            // (Tùy chọn) Kiểm tra xem user đã mua sản phẩm chưa?
            // bool hasPurchased = _context.Orders.Any(o => o.UserId == userId && o.OrderItems.Any(oi => oi.ProductId == productId));
            // if (!hasPurchased) { ... báo lỗi ... }

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = content, // Đảm bảo Model Review có cột này (hoặc Comment)
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm!";

            // Quay lại trang chi tiết sản phẩm
            return RedirectToAction("Details", "Product", new { id = productId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}