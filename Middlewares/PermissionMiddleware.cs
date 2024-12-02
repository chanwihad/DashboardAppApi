using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;  
using System.Security.Claims;  
using Microsoft.IdentityModel.Tokens;  
using System.Text; 

namespace CrudApi.Middlewares
{
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;

        public PermissionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var permission = endpoint.Metadata.GetMetadata<PermissionAttribute>();
                if (permission != null)
                {
                    // var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                    // if (token == null)
                    // {
                    //     context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    //     await context.Response.WriteAsync("You do not have permission to access this resource.");
                    //     return;
                    // }   
                    // var hasPermission = context.User.Claims
                    //     .Any(c => c.Type == permission.Permission && c.Value == "True");

                    // if (!hasPermission)
                    // {
                    //     context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    //     await context.Response.WriteAsync("You do not have permission to access this resource.");
                    //     return;
                    // }
                }
            }

            await _next(context);
        }
    }
}