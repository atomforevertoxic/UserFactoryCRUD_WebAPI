using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserFactory.Data;
using UserFactory.Models;

namespace UserFactory.Controllers
{
    //[ApiController]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly WebDbContext _context;

        public UsersController(WebDbContext context)
        {
            _context = context;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        [HttpGet("{guid}")]
        public async Task<IActionResult> GetUserByGuid(Guid guid)
        {
            var user = await _context.Users.FindAsync(guid);
            if (user==null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create([FromForm] User user)
        {
            return View();
        }



    }
}
