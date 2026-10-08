using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RestaurantBooking.Application;
using RestaurantBooking.Infrastructure;
using RestaurantBooking.Infrastructure.Auth;
using RestaurantBooking.Infrastructure.Persistence;
using RestaurantBooking.Web.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 characters long. Set it using: " +
        "dotnet user-secrets set \"Jwt:Key\" \"<long-random-string>\" --project src/RestaurantBooking.Web");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; 
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                ctx.Token = ctx.Request.Cookies[AuthCookie.Name];
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
                ctx.Response.Redirect($"/Account/Login?returnUrl={returnUrl}");
                return Task.CompletedTask;
            },
            OnForbidden = ctx =>
            {
                ctx.Response.Redirect("/Account/AccessDenied");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(o =>
    o.AddPolicy("AdminOnly", p => p.RequireRole(nameof(RestaurantBooking.Domain.Entities.UserRole.Admin))));

builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/Reservations");
    o.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
