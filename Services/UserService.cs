using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        public async Task<User?> GetByGuidAsync(Guid guid)
        {
            if (_context == null) return null;
            else if (_context.Users == null || _context.Users.Count() == 0) return null;

            return await _context.Users.FindAsync(guid);
        }

        public async Task<string> AddUserAsync(User user)
        {
            if (_context != null && _context.Users != null)
            {
                if (await _context.Users.AnyAsync(u => u.Login == user.Login))
                {
                    return "Duplicate";
                }

                try
                {
                    user.Password = _passwordHasher.HashPassword(user, user.Password);

                    await _context.Users.AddAsync(user);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }

                return "Success";
            }

            return "Database access error";
        }

        public async Task<User> AuthenticateUserAsync(LoginViewModel model)
        {
            var user = await GetUserByLoginAsync(model.Login);
            if (user == null)
            {
                return null; 
            }

            if (VerifyPassword(user, user.Password, model.Password))
            {
                return user;
            }

            return null;
        }

        public async Task<User> GetUserByLoginAsync(string login)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        }

        public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
