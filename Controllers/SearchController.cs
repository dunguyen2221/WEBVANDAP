using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using WEBVANDAP.Models;
using System.Data.Entity; // Cần thiết cho Include (nếu bạn muốn eager load)

namespace WEBVANDAP.Controllers
{
    // Đã cập nhật để sử dụng tên Context của bạn
    public class SearchController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // GET: Search/Index (Hiển thị Form và các tùy chọn lọc)
        public ActionResult Index()
        {
            // Sử dụng các tên thuộc tính Category/Brand phù hợp với Controller của bạn: Id, Name
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name");

            return View();
        }

        // GET: Search/Results (Action xử lý và hiển thị kết quả tìm kiếm)
        // @param keyword: Từ khóa tìm kiếm (tên sản phẩm)
        // @param categoryId: ID Danh mục để lọc (nullable)
        // @param brandId: ID Thương hiệu để lọc (nullable)
        public ActionResult Results(string keyword, int? categoryId, int? brandId)
        {
            // Đảm bảo Product.cs của bạn có các thuộc tính: Name, Description, CategoryId, BrandId

            // 1. Kiểm tra từ khóa rỗng
            if (string.IsNullOrWhiteSpace(keyword))
            {
                // Nếu không có từ khóa, chuyển hướng về trang tìm kiếm
                return RedirectToAction("Index");
            }

            // Bắt đầu truy vấn với tất cả sản phẩm
            IQueryable<Product> query = _context.Products.AsQueryable();

            // 2. Lọc theo Từ khóa (Áp dụng cho Name hoặc Description)
            // Lọc không phân biệt chữ hoa/chữ thường (sử dụng ToLower() trong LINQ to Entities)
            string lowerKeyword = keyword.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(lowerKeyword) || (p.Description != null && p.Description.ToLower().Contains(lowerKeyword)));

            // 3. Lọc theo Danh mục (Nếu có CategoryId)
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                // Sử dụng thuộc tính CategoryId
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // 4. Lọc theo Thương hiệu (Nếu có BrandId)
            if (brandId.HasValue && brandId.Value > 0)
            {
                // Sử dụng thuộc tính BrandId
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            // Eager loading Category và Brand để tránh lỗi N+1 trong View
            query = query.Include(p => p.Category).Include(p => p.Brand);

            // Thực thi truy vấn và chuyển kết quả thành List
            List<Product> searchResults = query.ToList();

            // Lấy tên Category và Brand để hiển thị trên View
            ViewBag.SearchKeyword = keyword;
            ViewBag.CategoryName = categoryId.HasValue ? _context.Categories.FirstOrDefault(c => c.Id == categoryId.Value)?.Name : "Tất cả Danh mục";
            ViewBag.BrandName = brandId.HasValue ? _context.Brands.FirstOrDefault(b => b.Id == brandId.Value)?.Name : "Tất cả Thương hiệu";

            // Truyền List<Product> vào View
            return View(searchResults);
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