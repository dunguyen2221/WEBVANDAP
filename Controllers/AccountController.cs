using BCrypt.Net;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security; // Thêm thư viện Forms Authentication
using WEBVANDAP.Models;

public class AccountController : Controller
{
    // ĐÃ KHAI BÁO BIẾN _CONTEXT Ở ĐÂY (PHẠM VI LỚP)
    private readonly ShopPCEntities2 _context = new ShopPCEntities2();

    public ActionResult DangKy()
    {
        return View();
    }

    [HttpPost]
    public ActionResult DangKy(string username, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ViewBag.Error = "Vui lòng điền đầy đủ thông tin!";
            return View();
        }

        if (password != confirmPassword)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp!";
            return View();
        }

        if (_context.AspNetUsers.Any(u => u.Email == email))
        {
            ViewBag.Error = "Email này đã được sử dụng!";
            return View();
        }

        // HASH PASSWORD BẰNG BCrypt
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new AspNetUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            Email = email,
            PasswordHash = hashedPassword, // ← HASH RỒI MỚI LƯU
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            AccessFailedCount = 0
        };

        _context.AspNetUsers.Add(user);
        _context.SaveChanges();

        Session["UserId"] = user.Id;
        Session["Username"] = user.UserName;

        return RedirectToAction("DangNhap");
    }

    public ActionResult DangNhap(string returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public ActionResult DangNhap(string email, string password, string returnUrl = null)
    {
        email = (email ?? "").Trim();
        password = (password ?? "").Trim();

        // 1. Tìm User
        var user = _context.AspNetUsers
            .FirstOrDefault(u => u.Email == email || u.UserName == email);

        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // Tải lại User và BUỘC EF tải mối quan hệ AspNetRoles
            user = _context.AspNetUsers.Include("AspNetRoles")
                .Single(u => u.Id == user.Id);

            // 2. Kiểm tra vai trò Admin 
            bool isAdmin = user.AspNetRoles.Any(r => r.Name == "Admin");

            // -----------------------------------------------------------------
            // LOGIC CẤP VÉ XÁC THỰC VÀ CHUYỂN HƯỚNG ĐÃ SỬA
            // -----------------------------------------------------------------

            // Cập nhật Session
            Session["UserId"] = user.Id;
            Session["Username"] = user.UserName;

            if (isAdmin)
            {
                // CẤP VÉ XÁC THỰC CÓ ROLE (ADMIN)
                string roleData = "Admin";
                FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(
                    1, user.UserName, DateTime.Now, DateTime.Now.AddMinutes(30), false, roleData, FormsAuthentication.FormsCookiePath
                );
                string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                Response.Cookies.Add(authCookie);

                // Chuyển Admin đến Dashboard
                return RedirectToAction("Index", "Admin");
            }
            else // Người dùng thường
            {
                // CẤP VÉ XÁC THỰC CƠ BẢN
                System.Web.Security.FormsAuthentication.SetAuthCookie(user.UserName, false);

                // Quay về trang yêu cầu hoặc trang chủ
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
        }

        ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    public ActionResult DangXuat()
    {
        // QUAN TRỌNG: GỠ BỎ XÁC THỰC COOKIE
        System.Web.Security.FormsAuthentication.SignOut();

        // Lấy URL của trang trước khi người dùng nhấn "Đăng xuất"
        var previousUrl = Request.UrlReferrer?.ToString();

        // Xóa thông tin đăng nhập trong Session
        Session.Clear();
        Session.Abandon();

        // Nếu có trang trước thì quay lại trang đó
        if (!string.IsNullOrEmpty(previousUrl))
        {
            return Redirect(previousUrl);
        }

        // Nếu không có thì đưa về trang chủ
        return RedirectToAction("Index", "Home");
    }
}
