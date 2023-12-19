using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Microsoft.AspNetCore.Routing
{
    internal static class ComponentsEndpointRouteBuilderExtensions
    {
        // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
        public static IEndpointConventionBuilder MapAdditionalEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var accountGroup = endpoints.MapGroup("/Account");

            accountGroup.MapPost("/Logout", async (HttpContext context)  =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return TypedResults.LocalRedirect("/");
            });

            var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();
            var adminGroup = accountGroup.MapGroup("/Admin").RequireAuthorization();

            var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();

            return accountGroup;
        }
    }
}
