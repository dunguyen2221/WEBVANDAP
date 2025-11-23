// File: Controllers/OrderController.cs (FINAL VERSION - FULL FEATURES)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using WEBVANDAP.Models;
using WEBVANDAP.ViewModels;
using Microsoft.AspNet.Identity;
using System.Web.Security; // Cần cho FormsAuthentication

public class OrderController : Controller
{
    private readonly ShopPCEntities2 _context = new ShopPCEntities2();

    // ==========================================================
    // HELPERS (HÀM HỖ TRỢ)
    // ==========================================================

    // Helper: Lấy Cart và CartItems từ DB (ĐÃ FIX INCLUDE)
    // TRONG OrderController.cs

    private Cart GetUserCart(string userId)
    {
        // THÊM DÒNG INCLUDE NÀY:
        return _context.Carts
                       .Include(c => c.CartItems)
                       .Include(c => c.CartItems.Select(ci => ci.Product)) // <--- BẮT BUỘC CÓ
                       .FirstOrDefault(c => c.UserId == userId);
    }

    private AspNetUser GetUserWithAddresses(string userId)
    {
        return _context.AspNetUsers
                       .Include(u => u.Addresses)
                       .FirstOrDefault(u => u.Id == userId);
    }

    // ==========================================================
    // CHỨC NĂNG KHÁCH HÀNG (CLIENT SIDE)
    // ==========================================================

    // --------------------------------------------------------
    // 1. GET: Order/Checkout (Hiển thị form thanh toán)
    // --------------------------------------------------------
    [Authorize]
    public ActionResult Checkout()
    {
        // FIX: SỬ DỤNG Session["UserId"] ĐỂ ĐỒNG BỘ
        string userId = Session["UserId"] as string;

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("DangNhap", "Account");
        }

        var cart = GetUserCart(userId);

        if (cart == null || !cart.CartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm.";
            return RedirectToAction("Index", "Cart");
        }

        var user = GetUserWithAddresses(userId);

        // FIX: Xử lý User null an toàn
        if (user == null)
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("DangNhap", "Account");
        }

        decimal cartTotal = cart.CartItems.Sum(ci => (ci.Quantity ?? 0) * ci.UnitPrice);
        decimal shippingFee = cartTotal >= 40000 ? 0 : 30000;

        var viewModel = new CheckoutViewModel
        {
            CartTotal = cartTotal,
            ShippingFee = shippingFee,
            // FinalTotal tự động tính trong ViewModel
            ShippingFullName = user.UserName,
            ShippingPhone = user.PhoneNumber,
        };

        ViewBag.CartItems = cart.CartItems.ToList();
        ViewBag.Addresses = user.Addresses.ToList();

        return View(viewModel);
    }


    // --------------------------------------------------------
    // 2. POST: Order/PlaceOrder (Xử lý đặt hàng & Trừ tồn kho)
    // --------------------------------------------------------
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult PlaceOrder(CheckoutViewModel model)
    {
        string userId = Session["UserId"] as string;

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("DangNhap", "Account");
        }

        var cart = GetUserCart(userId);

        if (cart == null || !cart.CartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng trống. Không thể đặt hàng.";
            return RedirectToAction("Index", "Cart");
        }

        // Tái tính toán tổng tiền (Server-side validation)
        model.CartTotal = cart.CartItems.Sum(ci => (ci.Quantity ?? 0) * ci.UnitPrice);
        model.ShippingFee = model.CartTotal >= 40000 ? 0 : 30000;
        // FIX: Tính toán vào biến cục bộ, KHÔNG gán vào thuộc tính Read-Only FinalTotal
        decimal calculatedFinalTotal = model.CartTotal + model.ShippingFee;

        // 🛑 BƯỚC 1: KIỂM TRA TỒN KHO TOÀN BỘ (BLOCK GIAO DỊCH NẾU THIẾU)
        bool hasStockError = false;
        foreach (var cartItem in cart.CartItems)
        {
            var productCheck = _context.Products.Find(cartItem.ProductId);
            int requestedQuantity = cartItem.Quantity.GetValueOrDefault(0);

            // Kiểm tra nếu sản phẩm không tồn tại hoặc không đủ hàng (Dùng StockQuantity)
            if (productCheck == null || productCheck.Stock < requestedQuantity)
            {
                ModelState.AddModelError("", $"Sản phẩm '{productCheck?.Name ?? "Không rõ"}' chỉ còn {productCheck?.Stock ?? 0} sản phẩm. Vui lòng cập nhật giỏ hàng.");
                hasStockError = true;
            }
        }

        if (hasStockError || !ModelState.IsValid)
        {
            // Nếu có lỗi, tải lại form Checkout
            ViewBag.CartItems = cart.CartItems.ToList();
            ViewBag.Addresses = GetUserWithAddresses(userId).Addresses.ToList();
            return View("Checkout", model);
        }

        // NẾU HỢP LỆ -> TIẾN HÀNH LƯU
        if (ModelState.IsValid)
        {
            // 1. TÌM HOẶC TẠO ĐỊA CHỈ GIAO HÀNG
            int shippingAddressId;

            if (model.SelectedAddressId.HasValue && model.SelectedAddressId.Value > 0)
            {
                shippingAddressId = model.SelectedAddressId.Value;
            }
            else
            {
                // FIX: Thêm Ward và District để tránh lỗi Khóa ngoại DB
                var newAddress = new Address
                {
                    UserId = userId,
                    FullName = model.ShippingFullName,
                    Phone = model.ShippingPhone,
                    Street = model.ShippingStreet,
                    City = "Hà Nội",
                    District = "Quận Mặc Định", // FIX: Thêm dữ liệu tạm
                    Ward = "Phường Mặc Định",    // FIX: Thêm dữ liệu tạm
                    IsDefault = false
                };
                _context.Addresses.Add(newAddress);
                _context.SaveChanges();
                shippingAddressId = newAddress.Id;
            }

            // 2. TẠO ĐỐI TƯỢNG ORDER
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                OrderCode = "ORD" + DateTime.Now.Ticks.ToString().Substring(10),
                TotalAmount = calculatedFinalTotal, // Sử dụng biến đã tính
                Status = "Pending",
                PaymentMethod = model.PaymentMethod ?? "COD",
                IsPaid = (model.PaymentMethod == "Online") ? (bool?)false : null,
                ShippingAddressId = shippingAddressId,
                Notes = model.Notes,
            };
            _context.Orders.Add(order);
            _context.SaveChanges();

            // 3. TẠO CHI TIẾT ĐƠN HÀNG VÀ TRỪ TỒN KHO
            foreach (var cartItem in cart.CartItems.ToList())
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity.GetValueOrDefault(0), // FIX: Đảm bảo không NULL
                    UnitPrice = cartItem.UnitPrice
                };
                _context.OrderItems.Add(orderItem);

                // TRỪ TỒN KHO THỰC TẾ (Đã an toàn vì đã kiểm tra ở Bước 1)
                var product = _context.Products.Find(cartItem.ProductId);
                if (product != null)
                {
                    product.Stock -= orderItem.Quantity;
                }
            }

            // 4. DỌN DẸP GIỎ HÀNG
            _context.CartItems.RemoveRange(cart.CartItems);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đơn hàng của bạn đã được đặt thành công!";
            // ✅ CHUYỂN HƯỚNG THÀNH CÔNG
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
        }

        // Trường hợp Validation thất bại
        ViewBag.CartItems = cart.CartItems.ToList();
        ViewBag.Addresses = GetUserWithAddresses(userId).Addresses.ToList();
        return View("Checkout", model);
    }

    // --------------------------------------------------------
    // 3. GET: Order/OrderConfirmation (Trang xác nhận đơn hàng)
    // --------------------------------------------------------
    [Authorize]
    public ActionResult OrderConfirmation(int? orderId)
    {
        string userId = Session["UserId"] as string;
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("DangNhap", "Account");

        if (orderId == null)
            return RedirectToAction("OrderHistory", "User");

        var order = _context.Orders
            .Include(o => o.Address)
            .Include(o => o.AspNetUser)
            .Include(o => o.OrderItems)
            .Include(o => o.OrderItems.Select(oi => oi.Product))            // <--- THÊM DÒNG NÀY
            .Include(o => o.OrderItems.Select(oi => oi.Product.ProductImages)) // <--- THÊM DÒNG NÀY
            .FirstOrDefault(o => o.Id == orderId.Value && o.UserId == userId);

        if (order == null)
            return HttpNotFound();

        return View(order);
    }



    // ==========================================================
    // PHẦN QUẢN LÝ ĐƠN HÀNG DÀNH CHO ADMIN (MỚI BỔ SUNG)
    // ==========================================================

    [Authorize(Roles = "Admin")]
    // GET: Order/Index (Danh sách tất cả đơn hàng cho Admin)
    public ActionResult Index()
    {
        var allOrders = _context.Orders
                                .Include(o => o.AspNetUser) // Tải thông tin khách hàng
                                .OrderByDescending(o => o.OrderDate)
                                .ToList();
        return View("Index", allOrders); // Trả về View riêng (cần tạo AdminIndex.cshtml)
    }

    [Authorize(Roles = "Admin")]
    // GET: Order/Details/5 (Xem chi tiết đơn hàng để xử lý)
    public ActionResult AdminDetails(int? id)
    {
        if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

        // Tải đầy đủ thông tin để Admin xem
        var order = _context.Orders
                            .Include(o => o.OrderItems.Select(oi => oi.Product))
                            .Include(o => o.OrderItems.Select(oi => oi.Product.ProductImages))
                            .Include(o => o.Address)
                            .Include(o => o.AspNetUser)
                            .FirstOrDefault(o => o.Id == id);

        if (order == null) return HttpNotFound();

        return View("AdminDetails", order); // Trả về View riêng (cần tạo AdminDetails.cshtml)
    }

    [Authorize(Roles = "Admin")]
    // POST: Order/UpdateStatus (Cập nhật trạng thái đơn hàng)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult UpdateStatus(int id, string status)
    {
        var order = _context.Orders.Find(id);
        if (order != null)
        {
            order.Status = status; // Ví dụ: "Shipped", "Completed", "Cancelled"
            _context.SaveChanges();
            TempData["SuccessMessage"] = $"Đơn hàng #{order.OrderCode} đã được cập nhật thành: {status}";
        }
        // Quay lại trang chi tiết Admin
        return RedirectToAction("AdminDetails", new { id = id });
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