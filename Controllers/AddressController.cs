using System.Linq;
using System.Web.Mvc;
using WEBVANDAP.Models;
using System.Data.Entity;

namespace WEBVANDAP.Controllers
{
    [Authorize]
    public class AddressController : Controller
    {
        private readonly ShopPCEntities2 _context = new ShopPCEntities2();

        // ================================
        // INDEX – Danh sách địa chỉ
        // ================================
        public ActionResult Index()
        {
            string userId = Session["UserId"] as string;

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("DangNhap", "Account");

            var addresses = _context.Addresses
                                    .Where(a => a.UserId == userId)
                                    .ToList();

            return View(addresses);
        }

        // ================================
        // CREATE – GET
        // ================================
        public ActionResult Create()
        {
            return View(new Address());
        }

        // ================================
        // CREATE – POST
        // ================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Address model)
        {
            string userId = Session["UserId"] as string;

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("DangNhap", "Account");

            if (ModelState.IsValid)
            {
                model.UserId = userId;

                // FIX: Set giá trị mặc định cho các trường không được null
                model.FullName = model.FullName ?? "";
                model.Phone = model.Phone ?? "";
                model.Street = model.Street ?? "";
                model.Ward = model.Ward ?? "";
                model.District = model.District ?? "";
                model.City = model.City ?? "";
                model.IsDefault = model.IsDefault ?? false;

                _context.Addresses.Add(model);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Thêm địa chỉ thành công!";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // ================================
        // EDIT – GET
        // ================================
        public ActionResult Edit(int id)
        {
            var address = _context.Addresses.Find(id);

            if (address == null)
                return HttpNotFound();

            return View(address);
        }

        // ================================
        // EDIT – POST
        // ================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Address model)
        {
            if (ModelState.IsValid)
            {
                var addr = _context.Addresses.Find(model.Id);
                if (addr == null)
                    return HttpNotFound();

                // FIX: Update đúng field thật
                addr.FullName = model.FullName;
                addr.Phone = model.Phone;
                addr.Street = model.Street;
                addr.Ward = model.Ward;
                addr.District = model.District;
                addr.City = model.City;
                addr.IsDefault = model.IsDefault;

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật địa chỉ thành công!";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // ================================
        // DELETE – GET
        // ================================
        public ActionResult Delete(int id)
        {
            var addr = _context.Addresses.Find(id);

            if (addr == null)
                return HttpNotFound();

            return View(addr);
        }

        // ================================
        // DELETE – POST
        // ================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var addr = _context.Addresses.Find(id);

            _context.Addresses.Remove(addr);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa địa chỉ thành công!";
            return RedirectToAction("Index");
        }

        // ================================
        // Dispose
        // ================================
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
