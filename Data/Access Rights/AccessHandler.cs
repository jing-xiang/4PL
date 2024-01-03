using _4PL.Data.Access_Rights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace _4PL.Data.Access_Rights
{
    public class AccessHandler : AuthorizationHandler<AccessRequirement>
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AccessHandler(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }
    
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AccessRequirement requirement)
        {
            string? email = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (await CheckAccessRightsAsync(email, requirement.AccessRight))
            {
                context.Succeed(requirement);
            }
        }

        private async Task<bool> CheckAccessRightsAsync(string email, string accessRight)
        {
            try
            {
                string? apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl");
                var response = await _httpClient.GetFromJsonAsync<bool>($"{apiBaseUrl}/api/Snowflake/CheckAccess{email}&Right={accessRight}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }
    }
}
