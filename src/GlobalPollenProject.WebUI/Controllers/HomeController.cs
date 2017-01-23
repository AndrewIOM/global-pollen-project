using Microsoft.AspNetCore.Mvc;
using GlobalPollenProject.App;
using System;
using System.Linq;

namespace GlobalPollenProject.WebUI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = GrainAppService.listUnknownGrains().ToList();
            return View(model);
        }

        public IActionResult Events()
        {
            var model = GrainAppService.listEvents().ToList();
            return View(model);
        }

        public IActionResult SubmitUnknownGrain()
        {
            var id = Guid.NewGuid();
            GrainAppService.submitUnknownGrain(id, "http://www.acm.im/cheuihiu.jpg");
            return RedirectToAction("Home");
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
