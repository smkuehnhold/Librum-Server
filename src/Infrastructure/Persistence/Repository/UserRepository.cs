using Application.Common.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repository;

public class UserRepository : IUserRepository
{
    private readonly DataContext _context;

    
    public UserRepository(DataContext context)
    {
        _context = context;
    }
    
    
    public async Task<User> GetAsync(string email, bool trackChanges)
    {
        return trackChanges
            ? await _context.Users.SingleOrDefaultAsync(user => user.Email == email)
            : await _context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Email == email);
    }

    public async Task DeleteAsync(User user)
    {
        await LoadRelationShipsAsync(user);
        _context.Users.Remove(user);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task LoadRelationShipsAsync(User user)
    {
        await _context.Entry(user).Collection(p => p.Books).LoadAsync();
    }
}