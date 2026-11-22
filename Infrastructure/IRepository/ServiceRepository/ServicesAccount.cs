using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public sealed class ServicesAccount : IAccountQueryService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly FreeBookDbContext _context;

        public ServicesAccount(RoleManager<IdentityRole> roleManager, FreeBookDbContext context)
        {
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            var roles = await _roleManager.Roles.OrderBy(x => x.Name).ToListAsync();
            return roles.Select(CrudMapping.ToDto).ToList();
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            var users = await _context.VwUsers.OrderBy(x => x.Role).ToListAsync();
            return users.Select(CrudMapping.ToDto).ToList();
        }
    }
}
