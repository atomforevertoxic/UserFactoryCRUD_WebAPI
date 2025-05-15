using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using UserFactory.Models;
using UserFactory.Services;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AccountService _accountService;
    private readonly User _defaultAdmin;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserService userService, AccountService accountService,ILogger<UsersController> logger, IOptions<User> admin)
    {
        _userService = userService;
        _accountService = accountService;
        _logger = logger;
        _defaultAdmin = admin.Value;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        try
        {
            return Ok(await _userService.GetUsersAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAll error");
            return StatusCode(500);
        }
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<User>> GetById(Guid id)
    {
        try
        {
            var user = await _userService.GetByGuidAsync(id);
            return user != null ? Ok(user) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user with GUID: {id}");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<User>>> GetActive()
    {
        try
        {
            return Ok(await _userService.GetActiveUsers());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] User user)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {   
            var currentUser = await _accountService.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized("Current user not found");
            }

            user.CreatedBy = currentUser.Name;
            user.CreatedOn = DateTime.UtcNow;
            user.Id = Guid.NewGuid();

            await _userService.AddUserAsync(user);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user. User: {@User}", user);
            return StatusCode(500, "Failed to create user");
        }
    }


 
    [HttpPost("init-default-admin")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateDefaultAdmin()
    {
        try
        {
            var existingAdmin = await _userService.GetUserByLoginAsync(_defaultAdmin.Login);
            if (existingAdmin != null)
            {
                return Conflict("Admin user already exists");
            }

            await _userService.AddUserAsync(_defaultAdmin);

            return CreatedAtAction(
                nameof(GetById),
                new { id = _defaultAdmin.Id },
                new
                {
                    _defaultAdmin.Id,
                    _defaultAdmin.Login,
                    _defaultAdmin.Name,
                    _defaultAdmin.Admin
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin initialization failed");
            return StatusCode(500, "Failed to initialize admin user");
        }
    }
}