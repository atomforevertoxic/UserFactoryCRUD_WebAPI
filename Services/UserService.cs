using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using UserFactory.Data;
using UserFactory.Models;

namespace UserFactory.Services
{
    public class UserService
    {
        private readonly WebDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(WebDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _passwordHasher = passwordHasher;
            _context = context;
        }

        public async Task<User> UpdateUserProfileAsync( User modifyingUser, UpdateProfileRequest request, string modifiedBy)
        {
            modifyingUser.Name = request.Name?? modifyingUser.Name;
            modifyingUser.Gender = request.Gender ?? modifyingUser.Gender;
            modifyingUser.Birthday = request.Birthday ?? modifyingUser.Birthday;
            modifyingUser.ModifiedBy = modifiedBy;
            modifyingUser.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return modifyingUser;
        }

        public async Task<User?> ChangePasswordAsync(User modifyingUser, string newPassword, string modifiedBy)
        {
            modifyingUser.Password = _passwordHasher.HashPassword(modifyingUser, newPassword);
            modifyingUser.ModifiedBy = modifiedBy;
            modifyingUser.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return modifyingUser;
        }

        public async Task<User> ChangeLoginAsync( User modifyingUser, string newLogin, string modifiedBy)
        {
            modifyingUser.Login = newLogin;
            modifyingUser.ModifiedBy = modifiedBy;
            modifyingUser.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return modifyingUser;
        }

        public bool LoginExistsAsync(string login)
        {
            return _context.Users.Any(u => u.Login == login);
        }

        public async Task<IList<User>> GetUsersAsync()
        {
            if (_context.Users != null)
            {
                return await _context.Users.ToListAsync();
            }
            return new List<User>();
        }

        public async Task<IList<User>> GetActiveUsers()
        {
            if (_context.Users!=null)
            {
                return await _context.Users
                    .Where(u => u.RevokedOn == null)
                    .OrderBy(u => u.CreatedOn)
                    .ToListAsync();
            }
            return new List<User>();
        }

        public async Task AddUserAsync(User user)
        {
            user.Password = _passwordHasher.HashPassword(user, user.Password);

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User> AuthenticateUserAsync(LoginViewModel model)
        {
            var user = GetUserByLogin(model.Login);
            if (user == null)
            {
                return null; 
            }

            if (VerifyPassword(user, model.Password))
            {
                return user;
            }

            return null;
        }

        public User? GetUserByLogin(string login)
        {
            return _context.Users.FirstOrDefault(u => u.Login == login);
        }

        public bool VerifyPassword(User user, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, providedPassword);
            return result == PasswordVerificationResult.Success;
        }

        public async Task<List<User>> GetUsersBornBeforeAsync(DateTime cutoffDate)
        {
            return await _context.Users
                .Where(u => u.Birthday != null && u.Birthday <= cutoffDate)
                .ToListAsync();
        }


        public async Task<User> SoftDeleteAsync(string login, string revokedBy)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == login && u.RevokedOn == null)
                ?? throw new InvalidOperationException($"Active user with login '{login}' not found");

            user.RevokedOn = DateTime.UtcNow;
            user.RevokedBy = revokedBy;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task FullDeleteAsync(string login)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == login)
                ?? throw new InvalidOperationException($"User with login '{login}' not found");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User> RestoreUserAsync(string login, string restoredBy)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == login && u.RevokedOn != null)
                ?? throw new InvalidOperationException($"Deleted user '{login}' not found");

            user.RevokedOn = null;
            user.RevokedBy = null;
            user.ModifiedBy = restoredBy;
            user.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return user;
        }
    }
}
