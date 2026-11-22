using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    /// <summary>
    /// Read-only account queries. Identity and VwUser entities are mapped to DTOs
    /// here so views never bind to persistence types.
    /// </summary>
    public interface IAccountQueryService
    {
        Task<List<RoleDto>> GetRolesAsync();
        Task<List<UserDto>> GetUsersAsync();
    }
}
