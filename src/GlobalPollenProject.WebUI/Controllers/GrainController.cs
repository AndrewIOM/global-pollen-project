using Microsoft.AspNetCore.Mvc;
using GlobalPollenProject.App;
using System;
using System.Linq;

namespace GlobalPollenProject.WebUI.Controllers
{
    public class GrainController : Controller
    {
        public IActionResult Index()
        {
            var model = GrainService.listUnknownGrains().ToList();
            return View(model);
        }

        public IActionResult Identify(Guid id)
        {
            var model = GrainService.listUnknownGrains().FirstOrDefault(m => m.Id == id);
            if (model == null) return BadRequest();
            return View(model);
        }

        [HttpPost]
        public IActionResult Identify(Guid id, int taxonId = 1)
        {
            GrainService.identifyUnknownGrain(id, taxonId);
            return RedirectToAction("Identify");
        }

    }
}
