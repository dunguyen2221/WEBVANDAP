using BCrypt.Net;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WEBVANDAP.Models;

public class AccountController : Controller
{
    private readonly ShopPCEntities2 _context = new ShopPCEntities2();

    // ====== GET: Đăng ký ======
    public ActionResult DangKy()
    {
        return View();
    }

    // ====== POST: Đăng ký ======
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult DangKy(string FullName, string Email, string Password, string ConfirmPassword)
    {
        // Kiểm tra validate cơ bản
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ViewBag.Error = "Vui lòng điền đầy đủ thông tin!";
            return View();
        }

        if (Password != ConfirmPassword)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp!";
            return View();
        }

        if (_context.AspNetUsers.Any(u => u.Email == Email))
        {
            ViewBag.Error = "Email này đã được sử dụng!";
            return View();
        }

        // Hash mật khẩu
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);

        var user = new AspNetUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = FullName,
            Email = Email,
            PasswordHash = hashedPassword,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            AccessFailedCount = 0
        };

        // Lưu vào DB
        _context.AspNetUsers.Add(user);
        _context.SaveChanges();

        // Lưu session
        Session["UserId"] = user.Id;
        Session["Username"] = user.UserName;

        // Chuyển trang đăng nhập
        return RedirectToAction("DangNhap");
    }

    // ====== GET: Đăng nhập ======
    public ActionResult DangNhap(string returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // ====== POST: Đăng nhập ======
    [HttpPost]
    public ActionResult DangNhap(string email, string password, string returnUrl = null)
    {
        email = (email ?? "").Trim();
        password = (password ?? "").Trim();

        var user = _context.AspNetUsers
            .FirstOrDefault(u => u.Email == email || u.UserName == email);

        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // Tải role
            user = _context.AspNetUsers.Include("AspNetRoles")
                .Single(u => u.Id == user.Id);

            bool isAdmin = user.AspNetRoles.Any(r => r.Name == "Admin");

            Session["UserId"] = user.Id;
            Session["Username"] = user.UserName;

            if (isAdmin)
            {
                string roleData = "Admin";
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1, user.UserName, DateTime.Now, DateTime.Now.AddMinutes(30), false, roleData,
                    FormsAuthentication.FormsCookiePath
                );
                string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket));

                return RedirectToAction("Index", "Admin");
            }
            else
            {
                FormsAuthentication.SetAuthCookie(user.UserName, false);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }
        }

        ViewBag.Error = "Sai email hoặc mật khẩu!";
        return View();
    }

    // ====== Đăng xuất ======
    public ActionResult DangXuat()
    {
        FormsAuthentication.SignOut();
        Session.Clear();
        Session.Abandon();

        return RedirectToAction("Index", "Home");
    }
}
