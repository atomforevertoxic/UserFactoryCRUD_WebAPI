using Microsoft.AspNetCore.Mvc;
using UserFactory.Models;

namespace UserFactory.Controllers
{
    public class AccountController : Controller
    {

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            return View();
        }

        [HttpPost]
        public IActionResult Logout()
        {
            return View();
        }

    }
}
