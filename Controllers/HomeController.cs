using System;
using System.Linq;
using System.Web.Mvc;
using WEBVANDAP.Models;
using System.Data.Entity;
using System.Collections.Generic;

namespace WEBVANDAP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // Brand KHÔNG còn CategoryId → Action này phải trả rỗng
        public JsonResult GetBrandsByCategory(int categoryId)
        {
            return Json(new List<object>(), JsonRequestBehavior.AllowGet);
        }

        // --------------------------------------------------------
        // INDEX (Trang chủ)
        // --------------------------------------------------------
        public ActionResult Index(string sortBy = "popular", int page = 1, int? filterCategory = null, int? filterBrand = null)
        {
            int pageSize = 6;

            var products = _context.Products
                                   .Include(p => p.ProductImages)
                                   .AsQueryable();

            // LỌC THEO CATEGORY
            if (filterCategory.HasValue && filterCategory.Value > 0)
            {
                products = products.Where(p => p.CategoryId == filterCategory.Value);
            }

            // LỌC THEO BRAND
            if (filterBrand.HasValue && filterBrand.Value > 0)
            {
                products = products.Where(p => p.BrandId == filterBrand.Value);
            }

            // SẮP XẾP
            switch (sortBy)
            {
                case "price-asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price-desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default:
                    products = products.OrderByDescending(p => p.Id);
                    break;
            }

            // PHÂN TRANG
            int totalProducts = products.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var pagedProducts = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // VIEWBAG TRUYỀN DỮ LIỆU
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentCategory = filterCategory;

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();

            return View(pagedProducts);
        }

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
                _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
