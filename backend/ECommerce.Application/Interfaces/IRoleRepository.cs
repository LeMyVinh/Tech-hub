namespace ECommerce.Application;

public interface IRoleRepository
{
    Task<int> GetRoleIdByNameAsync(string name);
}