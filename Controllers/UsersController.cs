using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
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

    [HttpGet("by-login/{login}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<User>> GetUserByLogin(string login)
    {
        try
        {
            User? user = _userService.GetUserByLogin(login);
            if (user==null)
            {
                _logger.LogError("Non-existent login");
                return NotFound($"There is no user with this login: {login}");
            }
            return Ok(new ResponseUser
            {
                Name = user.Name,
                Gender = user.Gender,
                Birthday = user.Birthday,
                IsActive = user.RevokedOn == null ? true : false
            });
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving user by login {login} ");
            return StatusCode(500, $"Failed to find user by login: {login}");
        }
    }

    [HttpPost("get-by-credentials")] // Post a request so that the information is passed in the body of the request
    [Authorize]
    public async Task<ActionResult<ResponseUser>> GetUserByCredentials([FromBody] LoginViewModel model)
    {
        try
        {
            // 1. User authorization
            var user = await _userService.AuthenticateUserAsync(model);
            if (user == null)
            {
                _logger.LogWarning($"Invalid credentials for login: {model.Login}");
                return Unauthorized(new { Message = "Invalid login or password" });
            }

            // 2. User activity checking
            if (user.RevokedOn != null)
            {
                _logger.LogWarning($"Attempt to access revoked account: {model.Login}");
                return Forbid("Account is deactivated");
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during credentials check for login: {model.Login}");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("older-than")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsersOlderThan([FromQuery] int age)
    {
        try
        {
            var cutoffDate = DateTime.Today.AddYears(-age);
            var users = await _userService.GetUsersBornBeforeAsync(cutoffDate);

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching users older than {age}");
            return StatusCode(500);
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

            var duplicate = _userService.GetUserByLogin(user.Login);

            if (duplicate!=null)
            {
                return Conflict($"A user with login '{user.Login}' already exists {duplicate}");
            }


            user.CreatedBy = currentUser.Name;
            user.CreatedOn = DateTime.UtcNow;
            user.Id = Guid.NewGuid();

            _userService.AddUserAsync(user);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating user. User: {user}");
            return StatusCode(500, "Failed to create user");
        }
    }


 
    [HttpPost("init-default-admin")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateDefaultAdmin()
    {
        try
        {
            var existingAdmin = _userService.GetUserByLogin(_defaultAdmin.Login);
            if (existingAdmin != null)
            {
                return Conflict("Admin user already exists");
            }

            _userService.AddUserAsync(_defaultAdmin);

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

    [HttpDelete("soft-delete/{login}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SoftUserDelete(string login)
    {
        try
        {
            var currentAdmin = await _accountService.GetCurrentUserAsync(User);

            if (currentAdmin.Login.Equals(login, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Admin {currentAdmin.Login} attempted to self-delete");
                return BadRequest(new { Message = "Admin cannot self-delete" });
            }

            var deletingUser = _userService.GetUserByLogin(login);
            if (deletingUser == null)
            {
                _logger.LogWarning($"Attempt to delete non-existent user: {login}");
                return NotFound(new { Message = $"User with login '{login}' not found" });
            }

            if (deletingUser.RevokedOn != null)
            {
                _logger.LogWarning($"Attempt to soft-delete already deleted user: {login}");
                return Conflict(new { Message = $"User {login} is already deleted" });
            }

            var result = await _userService.SoftDeleteAsync(login, currentAdmin.Name);

            _logger.LogInformation($"User {login} soft-deleted by {currentAdmin.Name}");
            return Ok(new
            {
                Message = $"User {login} was soft-deleted",
                result.RevokedOn,
                result.RevokedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during soft-delete of user {login}");
            return StatusCode(500, new { Message = "Internal server error during soft-delete" });
        }
    }

    [HttpDelete("full-delete/{login}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> FullUserDelete(string login)
    {
        try
        {
            var currentAdmin = await _accountService.GetCurrentUserAsync(User);

            if (currentAdmin.Login.Equals(login, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Admin {currentAdmin.Login} attempted to self-delete");
                return BadRequest(new { Message = "Admin cannot self-delete" });
            }


            var userExists = _userService.GetUserByLogin(login);
            if (userExists==null)
            {
                _logger.LogWarning($"Attempt to delete non-existent user: {login}");
                return NotFound(new { Message = $"User with login '{login}' not found" });
            }

            await _userService.FullDeleteAsync(login);

            _logger.LogInformation($"User {login} permanently deleted by {currentAdmin.Name}");
            return Ok(new { Message = $"User {login} was permanently deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during full-delete of user {login}");
            return StatusCode(500, new { Message = "Internal server error during deletion" });
        }
    }
}