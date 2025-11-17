using System;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security; // Cần thiết

namespace WEBVANDAP
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // HÀM QUAN TRỌNG: ĐỌC VAI TRÒ TỪ COOKIE SAU KHI XÁC THỰC
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];

            if (authCookie != null)
            {
                // 1. Giải mã vé xác thực (Ticket)
                FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);

                // 2. Lấy Role Data đã được nhúng trong AccountController
                string roles = authTicket.UserData;

                // 3. Tách Role Data thành mảng (ví dụ: "Admin" -> ["Admin"])
                string[] roleArray = roles.Split(',');

                // 4. Tạo đối tượng User với Vai trò mới (GenericPrincipal)
                HttpContext.Current.User = new GenericPrincipal(
                    new FormsIdentity(authTicket),
                    roleArray
                );
            }
        }
    }
}
