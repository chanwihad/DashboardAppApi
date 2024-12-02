using CrudApi.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt; 
using System.Text; 
using Microsoft.IdentityModel.Tokens;

namespace CrudApi.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user, Role role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, role.Name),
                new Claim("client_id", user.Id.ToString())  ,
                new Claim("CanCreate", role.CanCreate.ToString()),
                new Claim("CanView", role.CanView.ToString()),
                new Claim("CanUpdate", role.CanUpdate.ToString()),
                new Claim("CanDelete", role.CanDelete.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}