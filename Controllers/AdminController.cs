using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity; // Cần thiết cho các truy vấn nâng cao
using WEBVANDAP.Models;

namespace WEBVANDAP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        public ActionResult Index()
        {
            // 1. THỐNG KÊ CƠ BẢN
            ViewBag.ProductCount = _context.Products.Count();
            ViewBag.UserCount = _context.AspNetUsers.Count();
            ViewBag.OrderCount = _context.Orders.Count();

            // 2. TÍNH TOÁN CÁC CHỈ SỐ KINH DOANH QUAN TRỌNG

            // Tính Tổng Doanh Thu (Total Revenue)
            // Sử dụng cột TotalAmount trong bảng Order
            decimal totalRevenue = _context.Orders.Any() ?
                                   _context.Orders.Sum(o => o.TotalAmount) :
                                   0;
            ViewBag.TotalRevenue = totalRevenue.ToString("N0"); // Giữ nguyên số để định dạng trong View

            // Đếm Đơn hàng Mới (trong 7 ngày qua)
            DateTime lastWeek = DateTime.Now.AddDays(-7);
            ViewBag.NewOrderCount = _context.Orders.Count(o => o.OrderDate >= lastWeek);

            // Sản phẩm sắp hết (Giả định Stock < 10)
            ViewBag.LowStockCount = _context.Products.Count(p => p.Stock < 10);

            // 3. DỮ LIỆU BẢNG (5 Đơn hàng Mới nhất)
            // Cần Include AspNetUser để hiển thị tên khách hàng
            ViewBag.LatestOrders = _context.Orders
                                    .Include(o => o.AspNetUser)
                                    .OrderByDescending(o => o.OrderDate)
                                    .Take(5)
                                    .ToList();

            ViewBag.Message = "Chào mừng Admin đến Dashboard!";
            return View();
        }
    }
}
