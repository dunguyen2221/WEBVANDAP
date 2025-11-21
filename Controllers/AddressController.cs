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
                    return View(new Address { IsDefault = false });
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

                        // fix nullables
                        model.FullName = model.FullName ?? "";
                        model.Phone = model.Phone ?? "";
                        model.Street = model.Street ?? "";
                        model.Ward = model.Ward ?? "";
                        model.District = model.District ?? "";
                        model.City = model.City ?? "";
                        model.IsDefault = model.IsDefault ?? false;

                        // Nếu là default thì bỏ default cũ
                        if (model.IsDefault == true)
                        {
                            var oldDefaults = _context.Addresses
                                                      .Where(a => a.UserId == userId && a.IsDefault == true)
                                                      .ToList();
                            foreach (var addr in oldDefaults)
                                addr.IsDefault = false;
                        }

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
                    string userId = Session["UserId"] as string;
                    if (string.IsNullOrEmpty(userId))
                        return RedirectToAction("DangNhap", "Account");

                    var address = _context.Addresses
                                          .FirstOrDefault(a => a.Id == id && a.UserId == userId);
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
                    string userId = Session["UserId"] as string;
                    if (string.IsNullOrEmpty(userId))
                        return RedirectToAction("DangNhap", "Account");

                    if (ModelState.IsValid)
                    {
                        var addr = _context.Addresses
                                           .FirstOrDefault(a => a.Id == model.Id && a.UserId == userId);
                        if (addr == null)
                            return HttpNotFound();

                        addr.FullName = model.FullName;
                        addr.Phone = model.Phone;
                        addr.Street = model.Street;
                        addr.Ward = model.Ward;
                        addr.District = model.District;
                        addr.City = model.City;

                        // Nếu check default → bỏ các default khác
                        if (model.IsDefault == true)
                        {
                            var oldDefaults = _context.Addresses
                                                      .Where(a => a.UserId == userId && a.IsDefault == true && a.Id != model.Id)
                                                      .ToList();
                            foreach (var old in oldDefaults)
                                old.IsDefault = false;
                        }
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
                    string userId = Session["UserId"] as string;
                    if (string.IsNullOrEmpty(userId))
                        return RedirectToAction("DangNhap", "Account");

                    var addr = _context.Addresses
                                       .FirstOrDefault(a => a.Id == id && a.UserId == userId);
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
                    string userId = Session["UserId"] as string;
                    if (string.IsNullOrEmpty(userId))
                        return RedirectToAction("DangNhap", "Account");

                    var addr = _context.Addresses
                                       .FirstOrDefault(a => a.Id == id && a.UserId == userId);
                    if (addr == null)
                        return HttpNotFound();

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
