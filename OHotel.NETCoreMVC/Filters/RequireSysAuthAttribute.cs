using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OHotel.NETCoreMVC.Filters;

/// <summary>
/// 後台頁面授權 Filter（可選用）
/// 若後台改用 Cookie 驗證，可套用此 Filter 於 Sys 的 Controller。
/// 目前後台以 JWT 存於 localStorage、由前端 Vue 呼叫 API，故未預設套用。
/// </summary>
public class RequireSysAuthAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            base.OnActionExecuting(context);
            return;
        }
        var path = context.HttpContext.Request.Path.Value ?? "";
        if (path.Contains("/Sys/Login", StringComparison.OrdinalIgnoreCase))
        {
            base.OnActionExecuting(context);
            return;
        }
        if (context.HttpContext.Request.Headers.Accept.Any(h => h?.Contains("application/json") == true))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        context.Result = new RedirectResult("/Sys/Login");
    }
}
