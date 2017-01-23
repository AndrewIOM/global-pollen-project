using Microsoft.AspNetCore.Mvc;
using GlobalPollenProject.App;
using System;
using System.Linq;
using GlobalPollenProject.WebUI.ViewModels;

namespace GlobalPollenProject.WebUI.Controllers
{
    public class TaxonomyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Import(ImportTaxonViewModel result)
        {
            TaxonomyAppService.import(result.LatinName);
            return Ok();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
