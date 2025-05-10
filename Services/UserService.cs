using Microsoft.EntityFrameworkCore;
using UserFactory.Data;
using UserFactory.Models;

namespace UserFactory.Services
{
    public class UserService
    {
        private readonly WebDbContext _context;

        public UserService(WebDbContext context)
        {
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

        public async Task<User?> GetUserByGuidAsync(Guid guid)
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
    }
}
