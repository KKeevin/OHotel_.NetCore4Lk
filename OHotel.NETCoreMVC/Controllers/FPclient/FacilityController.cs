using Microsoft.AspNetCore.Mvc;
using OHotel.NETCoreMVC.Models;
using System.Diagnostics;

namespace OHotel.NETCoreMVC.Controllers.FPclient
{
    [Area("FP-client")]
    public class FacilityController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public FacilityController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
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