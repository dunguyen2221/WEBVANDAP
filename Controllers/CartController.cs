using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using WEBVANDAP.Models;
//
public class CartController : Controller
{
    private readonly ShopPCEntities2 _context = new ShopPCEntities2();

    // Helper (Đơn giản): Chỉ dùng để kiểm tra sự tồn tại của Cart
    private Cart GetCartHeader(string userId)
    {
        var cart = _context.Carts
                        .Include(c => c.CartItems) // Chỉ cần tải Cấp 1
                        .SingleOrDefault(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = System.DateTime.Now,
                CartItems = new List<CartItem>()
            };
            _context.Carts.Add(cart);
            _context.SaveChanges();
        }
        return cart;
    }

    // GET: Cart/Index (Tải đầy đủ dữ liệu cho View)
    public ActionResult Index()
    {
        string userId = Session["UserId"] as string;

        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Vui lòng đăng nhập để xem giỏ hàng.";
            return RedirectToAction("DangNhap", "Account");
        }

        var userCart = GetCartHeader(userId); // Lấy CartId

        // FIX: Tải đầy đủ Product và ProductImages cho View (Tách biệt khỏi Helper)
        var cartItemsWithData = _context.CartItems
                                           .Where(ci => ci.CartId == userCart.Id)
                                           .Include(ci => ci.Product)
                                           .Include(ci => ci.Product.ProductImages)
                                           .ToList();

        return View(cartItemsWithData); // Trả về danh sách CartItem đã tải đủ
    }

    // POST: Cart/AddToCart (Dùng Helper đơn giản)
    [HttpPost]
    public ActionResult AddToCart(int productId, int quantity = 1)
    {
        string userId = Session["UserId"] as string;
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng.";
            return RedirectToAction("DangNhap", "Account");
        }

        var userCart = GetCartHeader(userId); // Dùng Helper đơn giản
        var product = _context.Products.Find(productId);

        if (product == null)
        {
            TempData["Error"] = "Sản phẩm không tồn tại.";
            return RedirectToAction("Index");
        }

        var existingItem = _context.CartItems.FirstOrDefault(ci => ci.CartId == userCart.Id && ci.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.Quantity = existingItem.Quantity.GetValueOrDefault(0) + quantity;
        }
        else
        {
            var newItem = new CartItem
            {
                ProductId = productId,
                CartId = userCart.Id,
                Quantity = quantity,
                UnitPrice = product.Price
            };
            _context.CartItems.Add(newItem);
        }

        _context.SaveChanges();
        TempData["SuccessMessage"] = "Sản phẩm đã được thêm vào giỏ hàng!";
        return RedirectToAction("Index");
    }

    // POST: Cart/UpdateQuantity
    [HttpPost]
    public ActionResult UpdateQuantity(int productId, int quantity)
    {
        string userId = Session["UserId"] as string;
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("DangNhap", "Account");

        var userCart = GetCartHeader(userId);
        var itemToUpdate = _context.CartItems.FirstOrDefault(ci => ci.CartId == userCart.Id && ci.ProductId == productId);

        if (itemToUpdate != null)
        {
            if (quantity > 0)
            {
                itemToUpdate.Quantity = quantity;
            }
            else
            {
                _context.CartItems.Remove(itemToUpdate);
            }
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }

    // POST: Cart/RemoveFromCart
    [HttpPost]
    public ActionResult RemoveFromCart(int productId)
    {
        string userId = Session["UserId"] as string;
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("DangNhap", "Account");

        var userCart = GetCartHeader(userId);
        var itemToRemove = _context.CartItems.FirstOrDefault(ci => ci.CartId == userCart.Id && ci.ProductId == productId);

        if (itemToRemove != null)
        {
            _context.CartItems.Remove(itemToRemove);
            _context.SaveChanges();
        }

        return RedirectToAction("Index");
    }

    // POST: Cart/RemoveAll
    [HttpPost]
    public ActionResult RemoveAll()
    {
        string userId = Session["UserId"] as string;
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("DangNhap", "Account");

        var userCart = GetCartHeader(userId);

        if (userCart != null && userCart.CartItems.Any())
        {
            _context.CartItems.RemoveRange(userCart.CartItems);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Đã xóa tất cả sản phẩm khỏi giỏ hàng.";
        }

        return RedirectToAction("Index");
    }

    // GET: Cart/Checkout (Action này sẽ chạy thành công)
    public ActionResult Checkout()
    {
        string userId = Session["UserId"] as string;
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("DangNhap", "Account");

        var userCart = GetCartHeader(userId); // Dùng Helper đơn giản

        if (!userCart.CartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng của bạn đang trống!";
            return RedirectToAction("Index");
        }
        // Chuyển hướng sang OrderController để xử lý
        return RedirectToAction("Checkout", "Order");
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