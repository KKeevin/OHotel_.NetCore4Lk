using Microsoft.AspNetCore.Mvc;
using OHotel.NETCoreMVC.Models;
using System.Diagnostics;

namespace OHotel.NETCoreMVC.Controllers.Sys
{
    [Area("Sys")]
    public class WelcomeController : Controller
    {
        public IActionResult Index()
        {
            return View();

        }
        public IActionResult Test()
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
