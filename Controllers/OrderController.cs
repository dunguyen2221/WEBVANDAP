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

    // Helper: Lấy Cart (Đã Fix Include)
    private Cart GetUserCart(string userId)
    {
        // FIX: Thêm Include Product để tải dữ liệu Sản phẩm
        return _context.Carts
                       .Include(c => c.CartItems)
                       .Include(c => c.CartItems.Select(ci => ci.Product))
                       .FirstOrDefault(c => c.UserId == userId);
    }

    // Helper: Lấy User (Giữ nguyên)
    private AspNetUser GetUserWithAddresses(string userId)
    {
        return _context.AspNetUsers
                       .Include(u => u.Addresses)
                       .FirstOrDefault(u => u.Id == userId);
    }

    // --------------------------------------------------------
    // 1. GET: Order/Checkout (Sử dụng Session["UserId"])
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
            ShippingFullName = user.UserName,
            ShippingPhone = user.PhoneNumber,
        };

        ViewBag.CartItems = cart.CartItems.ToList();
        ViewBag.Addresses = user.Addresses.ToList();

        return View(viewModel);
    }


    // --------------------------------------------------------
    // 2. POST: Order/PlaceOrder (Sử dụng Session["UserId"])
    // --------------------------------------------------------
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult PlaceOrder(CheckoutViewModel model)
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
            TempData["Error"] = "Giỏ hàng trống. Không thể đặt hàng.";
            return RedirectToAction("Index", "Cart");
        }

        // Tái tính toán tổng tiền
        model.CartTotal = cart.CartItems.Sum(ci => (ci.Quantity ?? 0) * ci.UnitPrice);
        model.ShippingFee = model.CartTotal >= 40000 ? 0 : 30000;
        decimal calculatedFinalTotal = model.CartTotal + model.ShippingFee;

        // KIỂM TRA TỒN KHO AN TOÀN
        bool hasStockError = false;
        foreach (var cartItem in cart.CartItems)
        {
            // cartItem.Product đã được tải ở GetUserCart (FIX 1)
            int requestedQuantity = cartItem.Quantity.GetValueOrDefault(0);

            if (cartItem.Product == null || cartItem.Product.Stock < requestedQuantity)
            {
                ModelState.AddModelError("", $"Sản phẩm '{cartItem.Product?.Name ?? "Không rõ"}' chỉ còn {cartItem.Product?.Stock ?? 0} sản phẩm.");
                hasStockError = true;
            }
        }

        if (hasStockError || !ModelState.IsValid)
        {
            // Nếu có lỗi, tải lại form
            ViewBag.CartItems = cart.CartItems.ToList();
            ViewBag.Addresses = GetUserWithAddresses(userId).Addresses.ToList();
            return View("Checkout", model);
        }

        if (ModelState.IsValid)
        {
            // 1. TÌM HOẶC TẠO ĐỊA CHỈ (FIX LỖI KHÓA NGOẠI)
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
                    District = "Quận Mặc Định",
                    Ward = "Phường Mặc Định",
                    IsDefault = false
                };
                _context.Addresses.Add(newAddress);
                _context.SaveChanges();
                shippingAddressId = newAddress.Id;
            }

            // 2. TẠO ĐƠN HÀNG (FIX LỖI THIẾU NOTES)
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                OrderCode = "ORD" + DateTime.Now.Ticks.ToString().Substring(10),
                TotalAmount = calculatedFinalTotal,
                Status = "Pending",
                PaymentMethod = model.PaymentMethod ?? "COD",
                ShippingAddressId = shippingAddressId,
                Notes = model.Notes
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
                    Quantity = cartItem.Quantity.GetValueOrDefault(0),
                    UnitPrice = cartItem.UnitPrice
                };
                _context.OrderItems.Add(orderItem);

                // TRỪ TỒN KHO (Đã kiểm tra ở trên)
                if (cartItem.Product != null)
                {
                    cartItem.Product.StockQuantity -= orderItem.Quantity;
                }
            }

            // 4. DỌN DẸP GIỎ HÀNG
            _context.CartItems.RemoveRange(cart.CartItems);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đơn hàng của bạn đã được đặt thành công!";
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
        }

        // Lỗi Form Validation
        ViewBag.CartItems = cart.CartItems.ToList();
        ViewBag.Addresses = GetUserWithAddresses(userId).Addresses.ToList();
        return View("Checkout", model);
    }

    // --------------------------------------------------------
    // 3. GET: Order/OrderConfirmation (Sử dụng Session["UserId"])
    // --------------------------------------------------------
    [Authorize]
    public ActionResult OrderConfirmation(int? orderId)
    {
        // FIX: SỬ DỤNG Session["UserId"] ĐỂ ĐỒNG BỘ
        string userId = Session["UserId"] as string;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("DangNhap", "Account");
        }

        if (orderId == null)
        {
            return RedirectToAction("OrderHistory", "User");
        }

        var order = _context.Orders
                            .Include(o => o.OrderItems.Select(oi => oi.Product))
                            .Include(o => o.OrderItems.Select(oi => oi.Product.ProductImages))
                            .Include(o => o.Address)
                            .FirstOrDefault(o => o.Id == orderId.Value && o.UserId == userId); // FIX: Dùng userId từ Session

        if (order == null)
        {
            return HttpNotFound();
        }
        return View(order);
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