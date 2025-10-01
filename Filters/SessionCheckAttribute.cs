using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IPS_TH.Filters
{
    public class SessionCheckAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                var sessionId = context.HttpContext.Session.Id;
                var userData = context.HttpContext.Session.GetString("UserData");

                // ยกเว้นหน้า Login และ Register
                var isAuthController = context.RouteData.Values["controller"]?.ToString()?.ToLower() == "authen";
                var isLoginAction = context.RouteData.Values["action"]?.ToString()?.ToLower() == "login";
                var isAutoLoginAction = context.RouteData.Values["action"]?.ToString()?.ToLower() == "autologin";
                var isLogoutAction = context.RouteData.Values["action"]?.ToString()?.ToLower() == "logout";
                var isAccessDeniedAction = context.RouteData.Values["action"]?.ToString()?.ToLower() == "accessdenied";
                var isRegisterAction = context.RouteData.Values["action"]?.ToString()?.ToLower() == "registers";

                if (!isAuthController || (!isLoginAction && !isRegisterAction && !isAutoLoginAction && !isLogoutAction && !isAccessDeniedAction))
                {
                    if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userData))
                    {
                        context.Result = new RedirectToActionResult("AutoLogin", "Authen", null);
                        return;
                    }
                }
            }

            base.OnActionExecuting(context);
        }
    }
} 