using _4PL.Data.Access_Rights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Office.Interop.Excel;
using System.Security.Claims;

namespace _4PL.Data.Access_Rights
{
    public class AccessHandler : AuthorizationHandler<AccessRequirement>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AccessHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
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
                var httpClient = _httpClientFactory.CreateClient();
                string? apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl");
                var response = await httpClient.GetFromJsonAsync<bool>($"{apiBaseUrl}/api/Snowflake/Check={email}&Right={accessRight}");
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
