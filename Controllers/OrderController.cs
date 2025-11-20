using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using WEBVANDAP.Models;
using WEBVANDAP.ViewModels;
using Microsoft.AspNet.Identity;
using System.Web.Security;

public class OrderController : Controller
{
    private readonly ShopPCEntities2 _context = new ShopPCEntities2();

    // Helper: Lấy Cart
    private Cart GetUserCart(string userId)
    {
        return _context.Carts
                       .Include(c => c.CartItems)
                       .Include(c => c.CartItems.Select(ci => ci.Product))
                       .FirstOrDefault(c => c.UserId == userId);
    }

    // Helper: Lấy User kèm Address
    private AspNetUser GetUserWithAddresses(string userId)
    {
        return _context.AspNetUsers
                       .Include(u => u.Addresses)
                       .FirstOrDefault(u => u.Id == userId);
    }

    // ----------------------------- CHECKOUT (GET) -----------------------------
    [Authorize]
    public ActionResult Checkout()
    {
        string userId = Session["UserId"] as string;

        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("DangNhap", "Account");

        var cart = GetUserCart(userId);

        if (cart == null || !cart.CartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm.";
            return RedirectToAction("Index", "Cart");
        }

        var user = GetUserWithAddresses(userId);

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
            ShippingPhone = user.PhoneNumber
        };

        ViewBag.CartItems = cart.CartItems.ToList();
        ViewBag.Addresses = user.Addresses.ToList();

        return View(viewModel);
    }

    // ----------------------------- CHECKOUT (POST) -----------------------------
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult PlaceOrder(CheckoutViewModel model)
    {
        string userId = Session["UserId"] as string;

        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("DangNhap", "Account");

        var cart = GetUserCart(userId);

        if (cart == null || !cart.CartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng trống. Không thể đặt hàng.";
            return RedirectToAction("Index", "Cart");
        }

        model.CartTotal = cart.CartItems.Sum(ci => (ci.Quantity ?? 0) * ci.UnitPrice);
        model.ShippingFee = model.CartTotal >= 40000 ? 0 : 30000;

        decimal finalTotal = model.CartTotal + model.ShippingFee;

        // Kiểm tra tồn kho
        bool hasStockError = false;

        foreach (var ci in cart.CartItems)
        {
            int reqQty = ci.Quantity ?? 0;

            if (ci.Product == null || ci.Product.Stock < reqQty)
            {
                ModelState.AddModelError("", $"Sản phẩm '{ci.Product?.Name}' chỉ còn {ci.Product?.Stock ?? 0} sản phẩm.");
                hasStockError = true;
            }
        }

        if (hasStockError || !ModelState.IsValid)
        {
            ViewBag.CartItems = cart.CartItems.ToList();
            ViewBag.Addresses = GetUserWithAddresses(userId).Addresses.ToList();
            return View("Checkout", model);
        }

        // Xử lý Address mới
        int shippingAddressId;
        if (model.SelectedAddressId.HasValue && model.SelectedAddressId.Value > 0)
        {
            shippingAddressId = model.SelectedAddressId.Value;
        }
        else
        {
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

        // Tạo Order
        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.Now,
            OrderCode = "ORD" + DateTime.Now.Ticks.ToString().Substring(10),
            TotalAmount = finalTotal,
            Status = "Pending",
            PaymentMethod = model.PaymentMethod ?? "COD",
            ShippingAddressId = shippingAddressId,
        };

        _context.Orders.Add(order);
        _context.SaveChanges();

        // OrderItems + Trừ tồn kho
        foreach (var ci in cart.CartItems.ToList())
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = ci.ProductId,
                Quantity = ci.Quantity ?? 0,
                UnitPrice = ci.UnitPrice
            };

            _context.OrderItems.Add(orderItem);

            if (ci.Product != null)
                ci.Product.Stock -= orderItem.Quantity;
        }

        _context.CartItems.RemoveRange(cart.CartItems);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Đặt hàng thành công!";
        return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
    }

    // ----------------------------- ORDER CONFIRMATION -----------------------------
    [Authorize]
    public ActionResult OrderConfirmation(int? orderId)
    {
        string userId = Session["UserId"] as string;

        if (!orderId.HasValue)
            return RedirectToAction("Index", "Order");

        var order = _context.Orders
                            .Include(o => o.OrderItems.Select(oi => oi.Product))
                            .Include(o => o.OrderItems.Select(oi => oi.Product.ProductImages))
                            .Include(o => o.Address)
                            .Include(o => o.AspNetUser)
                            .FirstOrDefault(o => o.Id == orderId.Value);

        if (order == null)
            return HttpNotFound();

        return View(order);
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _context.Dispose();

        base.Dispose(disposing);
    }

    [Authorize(Roles = "Admin")]
    public ActionResult Index()
    {
        var orders = _context.Orders
            .Include(o => o.AspNetUser)
            .Include(o => o.Address)
            .OrderByDescending(o => o.OrderDate)
            .ToList();

        return View(orders);
    }
    [Authorize]
    public ActionResult Edit(int id)
    {
        var order = _context.Orders
            .Include(o => o.AspNetUser)
            .Include(o => o.Address)
            .FirstOrDefault(o => o.Id == id);

        if (order == null)
            return HttpNotFound();

        return View(order);
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(int id, string Status)
    {
        var order = _context.Orders.Find(id);

        if (order == null)
            return HttpNotFound();

        order.Status = Status;
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
        return RedirectToAction("Index");
    }
}
