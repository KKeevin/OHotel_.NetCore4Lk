using Microsoft.AspNetCore.Mvc;
using OHotel.NETCoreMVC.Models;
using System.Diagnostics;

namespace OHotel.NETCoreMVC.Controllers.Sys
{
    [Area("Sys")]
    public class SystemController : Controller
    {
        public IActionResult Class()
        {
            return View();

        }
        public IActionResult Item()
        {
            return View();
        }
        public IActionResult Staff()
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
