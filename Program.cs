using _4PL.Components;
using _4PL.Components.Account;
using _4PL.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Syncfusion.Blazor;
using Microsoft.AspNetCore.Authorization;
using _4PL.Data.Access_Rights;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddControllersWithViews();

// Add cookie authentication service
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/Forbidden/";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MaintenanceRead", policy =>
    {
        policy.Requirements.Add(new AccessRequirement("MAINTENANCE_READ"));
    });

    options.AddPolicy("RateCardRead", policy =>
    {
        policy.Requirements.Add(new AccessRequirement("RATECARD_READ"));
    });

    options.AddPolicy("ShipmentRead", policy =>
    {
        policy.Requirements.Add(new AccessRequirement("SHIPMENT_READ"));
    });

    options.AddPolicy("ShipmentAutoRatingRead", policy =>
    {
        policy.Requirements.Add(new AccessRequirement("SHIPMENT_AUTORATING_READ"));
    });

    options.AddPolicy("DataAnalysisRead", policy =>
    {
        policy.Requirements.Add(new AccessRequirement("DATA_ANALYSIS_READ"));
    });
});

builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddSyncfusionBlazor();
builder.Services.AddSingleton<SnowflakeDbContext>();
builder.Services.AddSingleton<AccessRightsDbContext>();
builder.Services.AddSingleton<IAuthorizationHandler, AccessHandler>();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.WebHost.UseStaticWebAssets();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Strict,
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultControllerRoute();
app.MapAdditionalEndpoints();

//https://stackoverflow.com/questions/76933203/not-able-to-get-to-api-controller-in-blazor-server
app.MapControllers();

app.Run();

       

