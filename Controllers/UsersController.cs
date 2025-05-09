using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserFactory.Data;

namespace UserFactory.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly WebDbContext _context;

        public UsersController(WebDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserByGuid(Guid guid)
        {
            var user = await _context.Users.FindAsync(guid);
            if (user==null)
            {
                return NotFound();
            }
            return Ok(user);
        }
    }
}
