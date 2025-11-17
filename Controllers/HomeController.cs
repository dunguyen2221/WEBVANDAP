using System;
using System.Linq;
using System.Web.Mvc;
using WEBVANDAP.Models;
using System.Data.Entity;
using System.Collections.Generic; // Cần thiết cho List
//bautau
namespace WEBVANDAP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // Phương thức AJAX trả về Brand theo Category (Cho Sidebar)
        // GET: Home/GetBrandsByCategory
        public JsonResult GetBrandsByCategory(int categoryId)
        {
            var brands = _context.Brands
                                 .Where(b => b.CategoryId == categoryId)
                                 .Select(b => new
                                 {
                                     Id = b.Id,
                                     Name = b.Name
                                 })
                                 .ToList();
            // JsonRequestBehavior.AllowGet là bắt buộc cho GET requests
            return Json(brands, JsonRequestBehavior.AllowGet);
        }

        // --------------------------------------------------------
        // READ: Home/Index (Trang chủ có Sắp xếp, Lọc & Phân trang)
        // --------------------------------------------------------
        public ActionResult Index(string sortBy = "popular", int page = 1, int? filterCategory = null, int? filterBrand = null)
        {
            // Định nghĩa số lượng sản phẩm trên mỗi trang
            int pageSize = 4;

            // Bắt đầu truy vấn (Include ProductImages để hiển thị ảnh trên trang chủ)
            var products = _context.Products
                                   .Include(p => p.ProductImages)
                                   .AsQueryable();

            // 1. Logic Lọc (Filtering)
            if (filterCategory.HasValue && filterCategory.Value > 0)
            {
                products = products.Where(p => p.CategoryId == filterCategory.Value);
            }
            if (filterBrand.HasValue && filterBrand.Value > 0)
            {
                products = products.Where(p => p.BrandId == filterBrand.Value);
            }
            // TODO: Thêm logic tìm kiếm (Search) nếu cần

            // 2. Logic Sắp xếp (Sorting)
            switch (sortBy)
            {
                case "price-asc":
                    // Giá: thấp -> cao
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price-desc":
                    // Giá: cao -> thấp
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default:
                    // Mặc định: Phổ biến / Mới nhất
                    products = products.OrderByDescending(p => p.Id);
                    break;
            }

            // 3. Tính toán Phân trang
            int totalProducts = products.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // Đảm bảo số trang hợp lệ
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            // Sử dụng Skip và Take để lấy sản phẩm của trang hiện tại
            var pagedProducts = products.Skip((page - 1) * pageSize).Take(pageSize);

            // 4. Truyền dữ liệu Phân trang, Sắp xếp, và Lọc đến View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentCategory = filterCategory;

            // Tải tất cả Category và Brand để hiển thị Sidebar/Dropdown
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();

            // 5. Trả về View
            return View(pagedProducts.ToList());
        }

        // --------------------------------------------------------
        // BỔ SUNG: Trang Thông tin (Cần tạo Views tương ứng)
        // --------------------------------------------------------

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
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