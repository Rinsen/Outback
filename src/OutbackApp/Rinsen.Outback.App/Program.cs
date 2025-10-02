using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.Configurations;
using Rinsen.IdentityProvider.Outback.Entities;

namespace Rinsen.Outback.App;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var env = builder.Environment;
        var configuration = builder.Configuration;

        // Configure services (previously in ConfigureServices)
        var connectionString = configuration.GetConnectionString("Outback");
        if (string.IsNullOrEmpty(connectionString))
            throw new Exception("No connection string provided");
            
        ConfigureOpenApi(builder.Services);

        builder.Services.AddRinsenIdentity(options => options.ConnectionString = connectionString);
        builder.Services.AddRinsenOutback();

        var gelfhost = configuration["Rinsen:GelfHost"];
        if (!string.IsNullOrEmpty(gelfhost))
        {
            builder.Services.AddRinsenGelf(options =>
            {
                options.GelfServiceHostNameOrAddress = gelfhost;
                options.GelfServicePort = 12202;
                options.ApplicationName = "Outback";
            });
        }

        // Configure logging
        builder.Logging.ClearProviders();
        if (env.IsDevelopment())
        {
            builder.Logging.AddConsole();
        }
        else
        {
            builder.Logging.AddRinsenGelfLogger();
        }

        AddAuthentication(builder.Services, connectionString, configuration);

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminsOnly", policy => policy.RequireClaim(RinsenClaimTypes.Administrator, "true"));
            options.AddRequiredScopePolicy("CreateNode", "outback.createnode");
        });

        if (env.IsRunningInContainer())
        {
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownProxies.Add(IPAddress.Parse("192.169.1.32"));
            });
        }

        builder.Services.AddDbContext<OutbackDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddDataProtection()
            .PersistKeysToDbContext<DataProtectionKeyDbContext>();

        builder.Services.AddDbContext<DataProtectionKeyDbContext>(options =>
            options.UseSqlServer(connectionString));

        var mvcBuilder = builder.Services.AddMvc(o =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            o.Filters.Add(new AuthorizeFilter(policy));
        })
        .AddRinsenOutbackControllers();

#if DEBUG
        if (env.IsDevelopment())
        {
            mvcBuilder.AddRazorRuntimeCompilation();
        }
#endif

        // Optional - Add Aspire service defaults if you want to use it
        builder.AddServiceDefaults();

        // Build the application
        var app = builder.Build();

        // Configure middleware (previously in Configure)
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseForwardedHeaders();
            app.UseExceptionHandler("/Error");
        }

        if (!env.IsRunningInContainer())
        {
            app.UseHttpsRedirection();
        }

        if (env.IsDevelopment())
        {
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/openapi/outback.json", "Outback");
                c.SwaggerEndpoint("/openapi/operation.json", "Operation");
                c.SwaggerEndpoint("/openapi/session.json", "Session");
                c.SwaggerEndpoint("/openapi/oauth.json", "OpenId Connect and OAuth");
            });
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseStaticFiles();
        
        // Set up endpoints - this replaces app.UseEndpoints
        app.MapOpenApi();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Identity}/{action=Index}");

        // Optional - Map default endpoints from Aspire
        // app.MapDefaultEndpoints();

        app.Logger.LogInformation("Starting");
        
        app.Run();
    }

    private static void AddAuthentication(IServiceCollection services, string connectionString, IConfiguration configuration)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        var httpContextAccessor = new HttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.SessionStore = new SqlTicketStore(new SessionStorage(connectionString), httpContextAccessor);
                options.LoginPath = "/Identity/Login";
                options.LogoutPath = "/Identity/LogOut";
                options.AccessDeniedPath = "/Identity/AccessDenied";
            })
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Rinsen:Outback"];
                options.Audience = configuration["Rinsen:ClientId"];
            });
    }

    private static void ConfigureOpenApi(IServiceCollection services)
    {
        services.AddOpenApi("outback", (options) =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken)
                             =>
            {
                document.Info.Title = "Outback";
                document.Info.Version = "v1";
                document.Info.Description = "APIs for managing the outback client and scope configurations";
                return Task.CompletedTask;
            });

            options.ShouldInclude = (description) =>
            {
                var relativePath = description.RelativePath ?? string.Empty;
                if (relativePath.Contains("outback/api", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            };
        });

        services.AddOpenApi("oauth", (options) =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken)
                             =>
            {
                document.Info.Title = "OpenId Connect and OAuth APIs";
                document.Info.Version = "v1";
                document.Info.Description = "Outback OpenId Connect and OAuth APIs";
                return Task.CompletedTask;
            });

            options.ShouldInclude = (description) =>
            {
                var relativePath = description.RelativePath ?? string.Empty;

                if (relativePath.Contains(".well-known", StringComparison.OrdinalIgnoreCase)
                    || relativePath.Contains("oauth", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            };
        });

        services.AddOpenApi("session", (options) =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken)
                             =>
            {
                document.Info.Title = "Session information";
                document.Info.Version = "v1";
                document.Info.Description = "Information about the user session";
                return Task.CompletedTask;
            });

            options.ShouldInclude = (description) =>
            {
                var relativePath = description.RelativePath ?? string.Empty;

                if (relativePath.Equals("api/session", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            };
        });

        services.AddOpenApi("operation", (options) =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken)
                             =>
            {
                document.Info.Title = "Operational API";
                document.Info.Version = "v1";
                document.Info.Description = "APIs for running operational tasks on this Outback installation";
                return Task.CompletedTask;
            });

            options.ShouldInclude = (description) =>
            {
                var relativePath = description.RelativePath ?? string.Empty;

                if (relativePath.Contains("api/admin", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            };
        });
    }
}
