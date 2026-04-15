using System.Text;
using LMS.API.Middleware;
using LMS.Application.Features.Auth.Interfaces;
using LMS.Application.Features.Auth.Services;
using LMS.Infrastructure.Persistense.DbContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FluentValidation;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Auth.Validators;
using LMS.Infrastructure.Services.Email;
using LMS.Infrastructure.Services.Redis;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Application.Features.Leaves.Services;
using LMS.API.Filters;
using LMS.Application.Features.Employees.Interfaces;
using LMS.Application.Features.Organization.Department.Interfaces;
using LMS.Application.Features.Organization.Department.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://flowoff.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddValidatorsFromAssembly(typeof(RegisterCompanyValidator).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Leave Management System API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });

    c.OperationFilter<TenantHeaderFilter>();
});

builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<ILeaveConfigService, LeaveConfigService>();

var defaultConnectionString = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(defaultConnectionString))
{
    throw new InvalidOperationException("CRITICAL: Database connection string 'ConnectionStrings__Default' is missing from environment variables.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(defaultConnectionString);
});
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<IEmployeeService, LMS.Application.Features.Employees.Services.EmployeeService>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILmsAuthorizationService, LMS.Application.Features.Auth.Services.Authorization.AuthorizationService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "a_very_long_secret_key_that_is_at_least_32_chars_long";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["AuthToken"];

                if (string.IsNullOrEmpty(context.Token))
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                try
                {
                    var cache = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
                    var token = context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;

                    if (token != null && await cache.GetAsync<string>($"blacklisted_{token.RawData}") != null)
                    {
                        context.Fail("Token has been revoked.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"REDIS_ERROR: {ex.Message}");
                }
            }
        };
    });


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (args.Contains("db-migrate"))
{
    Console.WriteLine("Applying database migrations...");
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        await DbSeeder.SeedPermissionsAsync(db);
    }
    Console.WriteLine("Database updated and seeded successfully!");
    return;
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseMiddleware<UserPermissionMiddleware>();
app.UseAuthorization();

app.MapControllers();


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapMethods("/api/wakeup", new[] { "GET", "HEAD" }, () => Results.Ok("API is awake!")).AllowAnonymous();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class TenantHeaderFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<Microsoft.OpenApi.Models.OpenApiParameter>();

        operation.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter
        {
            Name = "X-Tenant-Subdomain",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Required = false,
            Schema = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
            Description = "Subdomain of the tenant (needed for localhost testing)"
        });
    }
}
