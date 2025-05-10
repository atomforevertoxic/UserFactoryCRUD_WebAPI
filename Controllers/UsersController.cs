using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using UserFactory.Data;
using UserFactory.Models;
using UserFactory.Services;

namespace UserFactory.Controllers
{
    //[ApiController]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet("list")]
        public IActionResult GetUsers()
        {
            IList<User> users = _userService.GetUsers();

            if (users == null || users.Count == 0)
            {
                return NotFound("No users found");
            }
            return Ok(users);
        }

        [HttpGet("{guid}")]
        public async Task<IActionResult> GetUserByGuid(Guid guid)
        {
            var user = await _userService.GetUserByGuid(guid);
            if (user==null)
            {
                return NotFound("No user found");
            }
            return Ok(user);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] User user)
        {
            string result = await _userService.AddUserAsync(user);

            return RedirectToAction("Index", "Home");
        }



    }
}
