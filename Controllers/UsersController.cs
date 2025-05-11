using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using UserFactory.Data;
using UserFactory.Models;
using UserFactory.Services;
using Microsoft.Extensions.Options;

namespace UserFactory.Controllers
{
    //[ApiController]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly UserService _userService;

        public UsersController(UserService userService, IOptions<User> admin)
        {
            _userService = userService;
            if (admin!=null)
            {
                Create(admin.Value);
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUsers()
        {
            IList<User> users = await _userService.GetUsersAsync();

            if (users == null || users.Count == 0)
            {
                return NotFound("No users found");
            }
            return Ok(users);
        }

        [HttpGet("{guid}")]
        public async Task<IActionResult> GetUserByGuid(Guid guid)
        {
            var user = await _userService.GetUserByGuidAsync(guid);
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

            if (!ModelState.IsValid) 
            {
                return View();
            }

            string result = await _userService.AddUserAsync(user);

            return RedirectToAction("Index", "Home");
        }



    }
}
