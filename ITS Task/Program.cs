using ITS_Task.Business.Interfaces;
using ITS_Task.Business.Services;
using ITS_Task.Business.Settings;
using ITS_Task.DataAccess;
using ITS_Task.DataAccess.Interfaces;
using ITS_Task.DataAccess.Repositories;
using ITS_Task.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using ITS_Task.Middleware;

namespace ITS_Task
{
    public partial class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<JwtSettings>(
                builder.Configuration.GetSection("Jwt"));

            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole>(options => options.User.RequireUniqueEmail = true)
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Events = new JwtBearerEvents { OnTokenValidated = TokenAccountValidator.ValidateAsync };
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromSeconds(30),

                            ValidIssuer =
                                builder.Configuration["Jwt:Issuer"],

                            ValidAudience =
                                builder.Configuration["Jwt:Audience"],

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(
                                        builder.Configuration["Jwt:Key"]!))
                        };
                });

            builder.Services.AddControllers();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ReactPolicy", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description =
                            "JWT Authorization header using the Bearer scheme."
                    });

                options.AddSecurityRequirement(document =>
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(
                            "Bearer",
                            document)] = []
                    });
            });

            builder.Services.AddAuthorization();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration
                        .GetConnectionString("DefaultConnection"), sql => sql.EnableRetryOnFailure()));

            builder.Services.AddScoped<
                IEmployeeRepository,
                EmployeeRepository>();

            builder.Services.AddScoped<
                ILeaveRequestRepository,
                LeaveRequestRepository>();

            builder.Services.AddScoped<
                ILeaveTypeRepository,
                LeaveTypeRepository>();

            builder.Services.AddScoped<
                IEmployeeService,
                EmployeeService>();

            builder.Services.AddScoped<
                ILeaveRequestService,
                LeaveRequestService>();

            builder.Services.AddScoped<
                ILeaveTypeService,
                LeaveTypeService>();

            builder.Services.AddScoped<
                IAuthService,
                AuthService>();


            builder.Services.AddScoped<
    IDashboardService,
    DashboardService>();

            builder.Services.AddHttpContextAccessor();



            builder.Services.AddScoped<
    IAuditLogRepository,
    AuditLogRepository>();

            builder.Services.AddScoped<
                IAuditLogService,
                AuditLogService>();

            builder.Services.AddScoped<ITransactionRunner, TransactionRunner>();
            builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            builder.Services.AddScoped<IDepartmentService, DepartmentService>();
            builder.Services.AddScoped<SeedInitializer>();
            var key = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("Configure a JWT signing key with at least 32 bytes");
            var app = builder.Build();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseCors("ReactPolicy");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();
            app.MapGet("/api/health", async (AppDbContext db) => await db.Database.CanConnectAsync()
                ? Results.Ok(new { status = "ready" })
                : Results.StatusCode(503));
            app.MapFallback("/api/{**path}", () => Results.NotFound());
            app.MapFallbackToFile("index.html");

            if (builder.Configuration.GetValue<bool>("Database:Initialize"))
            {
                if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Automatic database initialization is only allowed in Development");
                using var scope = app.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
            }

            if (builder.Configuration.GetValue<bool>("Seed:Enabled"))
            {
                using var scope = app.Services.CreateScope();
                var includeDemo = builder.Configuration.GetValue<bool>("Seed:IncludeDemo");
                if (includeDemo && !app.Environment.IsDevelopment()) throw new InvalidOperationException("Demo data is only allowed in Development");
                await scope.ServiceProvider.GetRequiredService<SeedInitializer>().InitializeAsync(includeDemo);
            }

            app.Run();
        }
    }
}