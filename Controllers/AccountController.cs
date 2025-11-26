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

    // ============================== ĐĂNG KÝ ==============================
    public ActionResult DangKy()
    {
        return View();
    }

    [HttpPost]
    public ActionResult DangKy(string username, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
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

        // HASH PASSWORD BẰNG BCRYPT
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new AspNetUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            Email = email,
            PasswordHash = hashedPassword,   // ← LƯU BCRYPT HASH
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            AccessFailedCount = 0
        };

        _context.AspNetUsers.Add(user);
        _context.SaveChanges();

        // Lưu Session
        Session["UserId"] = user.Id;
        Session["Username"] = user.UserName;

        return RedirectToAction("DangNhap");
    }


    // ============================== ĐĂNG NHẬP ==============================
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

        var user = _context.AspNetUsers
            .Include("AspNetRoles")
            .FirstOrDefault(u => u.Email == email || u.UserName == email);

        if (user == null)
        {
            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
            return View();
        }

        // ✅ Kiểm tra tài khoản đã bị khóa chưa
        if (user.IsLocked)
        {
            ViewBag.Error = "Tài khoản này đã bị khóa vĩnh viễn!";
            return View();
        }

        // ✅ Kiểm tra mật khẩu
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
            return View();
        }

        // ✅ Lưu session
        Session["UserId"] = user.Id;
        Session["Username"] = user.UserName;

        bool isAdmin = user.AspNetRoles.Any(r => r.Name == "Admin");

        if (isAdmin)
        {
            string roleData = "Admin";
            var authTicket = new FormsAuthenticationTicket(
                1, user.UserName, DateTime.Now, DateTime.Now.AddMinutes(30), false, roleData,
                FormsAuthentication.FormsCookiePath
            );
            string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
            var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            Response.Cookies.Add(authCookie);

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

    // ============================== ĐĂNG XUẤT ==============================
    public ActionResult DangXuat()
    {
        FormsAuthentication.SignOut();

        var previousUrl = Request.UrlReferrer?.ToString();

        Session.Clear();
        Session.Abandon();

        if (!string.IsNullOrEmpty(previousUrl))
            return Redirect(previousUrl);

        return RedirectToAction("Index", "Home");
    }
}


