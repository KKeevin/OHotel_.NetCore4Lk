using Microsoft.AspNetCore.Mvc;
using OHotel.NETCoreMVC.Models;
using System.Diagnostics;

namespace OHotel.NETCoreMVC.Controllers.Sys
{
    [Area("Sys")]
    public class SaddogController : Controller
    {
        // 以下註解如果後續要增加Action方便複製，請勿刪除
        //public IActionResult YourAction()
        //{
        //    return View();
        //}
        public IActionResult Some()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
