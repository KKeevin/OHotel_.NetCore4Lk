using Microsoft.AspNetCore.Mvc;
using OHotel.NETCoreMVC.Models;
using System.Diagnostics;

namespace OHotel.NETCoreMVC.Controllers.Sys
{
    [Area("Sys")]
    public class WebsiteController : Controller
    {
        public IActionResult Info()
        {
            return View();
        }
        public IActionResult News()
        {
            return View();
        }
        public IActionResult Staff()
        {
            return View();
        }
        public IActionResult Notice()
        {
            return View();
        }
        public IActionResult BookingRules()
        {
            return View();
        }
        public IActionResult Images()
        {
            return View();
        }
        public IActionResult Photos()
        {
            return View();
        }
        public IActionResult Facilities()
        {
            return View();
        }
        public IActionResult Restaurant()
        {
            return View();
        }
        public IActionResult Room()
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