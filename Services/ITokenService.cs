using CrudApi.Models;

namespace CrudApi.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user, Role role);
    }

}