using Microsoft.AspNetCore.Mvc;
using GlobalPollenProject.App;
using System;
using System.Linq;

namespace GlobalPollenProject.WebUI.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Register()
        {
            UserAppService.register(Guid.NewGuid(), "Mr", "Test", "User");
            return Ok();
        }

    }
}
