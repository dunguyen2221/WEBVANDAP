using Microsoft.AspNet.Identity; // Cần cho User.Identity.GetUserId() và Identity Manager
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using WEBVANDAP.Models;

namespace WEBVANDAP.Controllers
{
    // Bắt buộc: Chỉ người dùng đã đăng nhập mới có thể truy cập
    [Authorize]
    public class UserController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // Lưu ý: Nếu bạn dùng ASP.NET Identity, bạn cần khởi tạo UserManager và RoleManager
        // var UserManager = new UserManager<...>(...);

        // Phương thức hỗ trợ để lấy ID người dùng hiện tại
        private AspNetUser GetCurrentUserProfile()
        {
            // FIX: SỬ DỤNG Session["UserId"] ĐỂ ĐỒNG BỘ
            string userId = Session["UserId"] as string;

            if (string.IsNullOrEmpty(userId))
            {
                return null; // Controller (Action Index) sẽ xử lý chuyển hướng
            }

            // Tìm đối tượng AspNetUser
            return _context.AspNetUsers
                           .Include(u => u.Addresses)
                           .FirstOrDefault(u => u.Id == userId);
        }

        // --------------------------------------------------------
        // KHÁCH HÀNG: User/Index (Trang Hồ sơ chính)
        // --------------------------------------------------------
        public ActionResult Index()
        {
            var userProfile = GetCurrentUserProfile();
            if (userProfile == null)
            {
                return HttpNotFound();
            }
            return View(userProfile);
        }

        // --------------------------------------------------------
        // KHÁCH HÀNG: EDIT (GET/POST) Hồ sơ cá nhân
        // --------------------------------------------------------
        public ActionResult Edit()
        {
            var userProfile = GetCurrentUserProfile();
            if (userProfile == null)
            {
                return HttpNotFound();
            }
            return View(userProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AspNetUser model)
        {
            if (ModelState.IsValid)
            {
                // Tìm đối tượng hiện tại từ DB để tránh ghi đè các trường bảo mật
                var userInDb = _context.AspNetUsers.Find(model.Id);

                if (userInDb != null)
                {
                    // CHỈ cập nhật các trường an toàn
                    userInDb.Email = model.Email;
                    userInDb.PhoneNumber = model.PhoneNumber;

                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                    return RedirectToAction("Index");
                }
            }
            return View(model);
        }

        // --------------------------------------------------------
        // ADMIN ONLY: User/ManageUsers (Quản lý Tất cả người dùng)
        // --------------------------------------------------------

        [Authorize(Roles = "Admin")]
        public ActionResult ManageUsers()
        {
            // Lấy danh sách TẤT CẢ người dùng
            var allUsers = _context.AspNetUsers.ToList();
            return View(allUsers);
        }

        [Authorize(Roles = "Admin")]
        // GET: User/AdminEdit/userId (Hiển thị form chỉnh sửa cho Admin)
        public ActionResult AdminEdit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var userProfile = _context.AspNetUsers.Find(id);

            if (userProfile == null)
            {
                return HttpNotFound();
            }

            // Gửi danh sách Role (quyền) để Admin có thể gán quyền mới
            // Nếu bạn dùng Identity Manager, bạn cần phải load vai trò hiện tại của user vào View
            ViewBag.Roles = new SelectList(_context.AspNetRoles, "Id", "Name");

            return View(userProfile);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminEdit(AspNetUser model, string selectedRoleId)
        {
            if (ModelState.IsValid)
            {
                var userInDb = _context.AspNetUsers
                    .Include(u => u.AspNetRoles)
                    .SingleOrDefault(u => u.Id == model.Id);

                if (userInDb != null)
                {
                    // Cập nhật thông tin cơ bản nếu muốn (Email, PhoneNumber)
                    userInDb.Email = model.Email;
                    userInDb.PhoneNumber = model.PhoneNumber;

                    // =========================
                    // CẬP NHẬT ROLE
                    // =========================
                    // Xóa hết role cũ
                    userInDb.AspNetRoles.Clear();

                    // Gán role mới
                    if (!string.IsNullOrEmpty(selectedRoleId))
                    {
                        var role = _context.AspNetRoles.Find(selectedRoleId);
                        if (role != null)
                        {
                            userInDb.AspNetRoles.Add(role);
                        }
                    }

                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                    return RedirectToAction("ManageUsers");
                }
            }

            // Nếu lỗi, load lại dropdown
            ViewBag.Roles = new SelectList(_context.AspNetRoles, "Id", "Name", selectedRoleId);
            return View(model);
        }


        // --------------------------------------------------------
        // KHÁCH HÀNG: User/OrderHistory (Xem lịch sử đơn hàng)
        // --------------------------------------------------------
        // TRONG UserController.cs
        // --------------------------------------------------------
        // KHÁCH HÀNG: User/OrderHistory (Xem lịch sử đơn hàng)
        // --------------------------------------------------------
        public ActionResult OrderHistory()
        {
            // FIX: SỬ DỤNG Session["UserId"] ĐỂ ĐỒNG BỘ
            string userId = Session["UserId"] as string;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("DangNhap", "Account");
            }

            // Tải các đơn hàng của người dùng hiện tại
            var orders = _context.Orders
                                 .Include(o => o.AspNetUser) // Tải kèm thông tin User (nếu cần)
                                 .Include(o => o.Address) // Tải kèm Địa chỉ
                                 .Where(o => o.UserId == userId) // Dùng userId từ Session
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToList();

            return View(orders);
        }

        // ... Có thể thêm ManageAddress, ChangePassword, v.v. ...

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
        public ActionResult ManageAddress()
        {
            return RedirectToAction("Index", "Address");
        }
        // GET: User/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: User/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string userId = Session["UserId"] as string;

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("DangNhap", "Account");

            var user = _context.AspNetUsers.Find(userId);
            if (user == null)
                return HttpNotFound();

            // =========================
            // 1. KIỂM TRA MẬT KHẨU CŨ
            // =========================
            bool isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(model.OldPassword, user.PasswordHash);

            if (!isOldPasswordCorrect)
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không chính xác.");
                return View(model);
            }

            // =========================
            // 2. HASH MẬT KHẨU MỚI BẰNG BCRYPT
            // =========================
            string newHashed = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.PasswordHash = newHashed;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index");
        }
        [Authorize(Roles = "Admin")]
        public ActionResult LockUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = _context.AspNetUsers.Find(id);
            if (user == null)
                return HttpNotFound();

            user.IsLocked = true;   // KHÓA VĨNH VIỄN
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đã khóa tài khoản!";
            return RedirectToAction("ManageUsers"); // reload lại View từ DB
        }

        [Authorize(Roles = "Admin")]
        public ActionResult UnlockUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = _context.AspNetUsers.Find(id);
            if (user == null)
                return HttpNotFound();

            user.IsLocked = false;   // MỞ KHÓA
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đã mở khóa tài khoản!";
            return RedirectToAction("ManageUsers");
        }



    }
}