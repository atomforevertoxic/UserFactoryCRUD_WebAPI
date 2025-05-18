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

    public UsersController(UserService userService, AccountService accountService, ILogger<UsersController> logger, IOptions<User> admin)
    {
        _userService = userService;
        _accountService = accountService;
        _logger = logger;
        _defaultAdmin = admin.Value;
    }

    [HttpGet("full-list")]
    [Authorize]
    public async Task<ActionResult<IList<User>>> GetAll()
    {
        try
        {
            return Ok(await _userService.GetUsersAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, "Internal server error");
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

            await _userService.AddUserAsync(_defaultAdmin);

            return CreatedAtAction(
                nameof(Create),
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
            return StatusCode(500, $"Failed to initialize admin user");
        }
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = await _accountService.GetCurrentUserAsync(User);
            if (!currentUser.Admin && request.Admin)
            {
                _logger.LogError($"User {currentUser.Login} has no right to create admins");
                return Conflict("You have no right to create admin user!");
            }

            if (_userService.LoginExistsAsync(request.Login))
            {
                return Conflict($"A user with login '{request.Login}' already exists");
            }

            User newUser = new User(request);

            newUser.Admin = request.Admin;
            newUser.CreatedBy = currentUser.Name;
            

            await _userService.AddUserAsync(newUser);

            _logger.LogInformation($"User {newUser.Login} created successfully");
            return CreatedAtAction(nameof(Create), new {Message = "Success user creation" }, new
            {
                newUser.Id,
                newUser.Login,
                newUser.Name,
                newUser.Admin
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating user. User: {request}");
            return StatusCode(500, $"Failed to create user  {ex}");
        }
    }


    [HttpPatch("{login}/update-profile")]
    [Authorize]
    public async Task<IActionResult> UpdateUserProfile( string login, [FromBody] UpdateProfileRequest request)
    {
        try
        {
            var currentUser = await _accountService.GetCurrentUserAsync(User);

            var targetUser = _userService.GetUserByLogin(login);

            var validationError = ValidateUserAccess(currentUser, targetUser);
            if (validationError != null) return validationError;

            if (targetUser==null)
            {
                _logger.LogError("Non-existent login");
                return NotFound($"There is no user with this login: {login}");
            }

            var updatedUser = await _userService.UpdateUserProfileAsync( targetUser, request, currentUser.Login);

            return Ok(new UserProfileResponse
                    (
                        name: updatedUser.Name,
                        gender: updatedUser.Gender,
                        birthday: updatedUser.Birthday,
                        isActive: updatedUser.RevokedOn == null
                    ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating profile for login");
            return StatusCode(500, new { Message = "Profile update failed" });
        }
    }

    private IActionResult ValidateUserAccess(User currentUser, User targetUser)
    {
        if (targetUser.RevokedOn != null)
        {
            return BadRequest(new { Message = "Modifying user account is deactivated" });
        }
        else if (!currentUser.Login.Equals(targetUser.Login, StringComparison.OrdinalIgnoreCase) && !currentUser.Admin)
        {
            return Forbid();
        }

        return null;
    }

    [HttpPatch("{login}/update-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword( string login, [FromBody] ChangePasswordRequest request)
    {
        try
        {
            var currentUser = await _accountService.GetCurrentUserAsync(User);
            var targetUser = _userService.GetUserByLogin(login);

            var validationError = ValidateUserAccess(currentUser, targetUser);
            if (validationError != null) return validationError;

            var updatedUser = await _userService.ChangePasswordAsync(targetUser, request.NewPassword, currentUser.Login);

            return Ok(new { 
                Message = "Password changed successfully",
                ModifiedOn = updatedUser.ModifiedOn,
                ModifiedBy = updatedUser.ModifiedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error changing password for {login}");
            return StatusCode(500, new { Message = "Password change failed" });
        }
    }

    [HttpPatch("{login}/update-login")]
    [Authorize]
    public async Task<IActionResult> ChangeLogin( string login, [FromBody] ChangeLoginRequest request)
    {
        try
        {
            var currentUser = await _accountService.GetCurrentUserAsync(User);
            var targetUser = _userService.GetUserByLogin(login);

            var validationError = ValidateUserAccess(currentUser, targetUser);
            if (validationError != null) return validationError;

            
            if (_userService.LoginExistsAsync(request.NewLogin))
                return Conflict(new { Message = $"Login '{request.NewLogin}' already taken" });

            var updatedUser = await _userService.ChangeLoginAsync(targetUser, request.NewLogin, currentUser.Login);

            return Ok(new
            {
                Message = "Login changed successfully",
                OldLogin = login,
                NewLogin = updatedUser.Login,
                ModifiedOn = updatedUser.ModifiedOn,
                ModifiedBy = updatedUser.ModifiedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error changing login for {login}");
            return StatusCode(500, new { Message = "Login change failed" });
        }
    }

    [HttpGet("active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<User>>> GetActive()
    {
        try
        {
            var activeUsers = await _userService.GetActiveUsers();
            var response = activeUsers.Select(u => new UserProfileResponse(
                name: u.Name,
                gender: u.Gender,
                birthday: u.Birthday,
                isActive: u.RevokedOn == null
            ));
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{login}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<User>> GetUserByLogin(string login)
    {
        try
        {
            User? user = _userService.GetUserByLogin(login);
            if (user == null)
            {
                _logger.LogError("Non-existent login");
                return NotFound($"There is no user with this login: {login}");
            }
            
            return Ok(new UserProfileResponse
            (
                name: user.Name,
                gender: user.Gender,
                birthday: user.Birthday,
                isActive: user.RevokedOn == null
            ));

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving user by login {login} ");
            return StatusCode(500, $"Failed to find user by login: {login}");
        }
    }

    [HttpPost("by-credentials")] // Post a request so that the information is passed in the body of the request
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> GetUserByCredentials([FromBody] LoginViewModel model)
    {
        try
        {
            
            var user = await _userService.AuthenticateUserAsync(model);
            if (user == null)
            {
                _logger.LogWarning($"Invalid credentials for login: {model.Login}");
                return Unauthorized(new { Message = "Invalid login or password" });
            }

            
            if (user.RevokedOn != null)
            {
                _logger.LogWarning($"Attempt to access revoked account: {model.Login}");
                return Forbid("Account is deactivated");
            }

            return Ok(new UserProfileResponse
                    (
                        name: user.Name,
                        gender: user.Gender,
                        birthday: user.Birthday,
                        isActive: user.RevokedOn == null
                    ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during credentials check for login: {model.Login}");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("older-than/{age}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsersOlderThan(int age)
    {
        try
        {
            var cutoffDate = DateTime.Today.AddYears(-age);
            var olderUsers = await _userService.GetUsersBornBeforeAsync(cutoffDate);

            var response = olderUsers.Select(u => new UserProfileResponse(
                name: u.Name,
                gender: u.Gender,
                birthday: u.Birthday,
                isActive: u.RevokedOn == null
            ));

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching users older than {age}");
            return StatusCode(500);
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
                return BadRequest(new { Message = "Admin cannot delete himself" });
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
                return BadRequest(new { Message = "Admin cannot delete himself" });
            }


            var userExists = _userService.GetUserByLogin(login);
            if (userExists == null)
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


    [HttpPatch("{login}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestoreUser(string login)
    {
        try
        {
            
            var currentAdmin = await _accountService.GetCurrentUserAsync(User);

            
            var userToRestore = _userService.GetUserByLogin(login);
            if (userToRestore == null)
            {
                _logger.LogWarning($"Restore attempt for non-existent user: {login}");
                return NotFound(new { Message = $"User '{login}' not found" });
            }

            if (userToRestore.RevokedOn == null)
            {
                _logger.LogWarning($"Restore attempt for active user: {login}");
                return BadRequest(new { Message = $"User '{login}' is not soft-deleted" });
            }

            
            var restoredUser = await _userService.RestoreUserAsync(login, currentAdmin.Name);

            _logger.LogInformation($"User {login} restored by {currentAdmin.Login}");



            return Ok(new
            {
                Message = $"User '{login}' successfully restored",
                User = new UserProfileResponse
                    (
                        name: restoredUser.Name,
                        gender: restoredUser.Gender,
                        birthday: restoredUser.Birthday,
                        isActive: restoredUser.RevokedOn == null
                    ),
                RestoredBy = currentAdmin.Login,
                RestoredAt = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error restoring user {login}");
            return StatusCode(500, new { Message = "Internal server error during restoration" });
        }
    }
}