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

public class Startup
{
    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        _env = env;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        var connectionString = Configuration.GetConnectionString("Outback");

        if (string.IsNullOrEmpty(connectionString))
            throw new Exception("No connection string provided");
        
        ConfigureOpenApi(services);

        services.AddRinsenIdentity(options => options.ConnectionString = connectionString);
        services.AddRinsenOutback();

        var gelfhost = Configuration["Rinsen:GelfHost"];

        if (!string.IsNullOrEmpty(gelfhost))
        {
            services.AddRinsenGelf(options =>
            {
                options.GelfServiceHostNameOrAddress = gelfhost;
                options.GelfServicePort = 12202;
                options.ApplicationName = "Outback";
            });
        }

        AddAuthentication(services, connectionString);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminsOnly", policy => policy.RequireClaim(RinsenClaimTypes.Administrator, "true"));
            options.AddRequiredScopePolicy("CreateNode", "outback.createnode");
        });

        if (_env.IsRunningInContainer())
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownProxies.Add(IPAddress.Parse("192.169.1.32"));
            });
        }

        services.AddDbContext<OutbackDbContext>(options =>
        options.UseSqlServer(connectionString));

        services.AddDataProtection()
            .PersistKeysToDbContext<DataProtectionKeyDbContext>();

        services.AddDbContext<DataProtectionKeyDbContext>(options =>
        options.UseSqlServer(connectionString));

        var builder = services.AddMvc(o =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            o.Filters.Add(new AuthorizeFilter(policy));

        })
        .AddRinsenOutbackControllers();

#if DEBUG
        if (_env.IsDevelopment())
        {
            builder.AddRazorRuntimeCompilation();
        }
#endif
    }
    public void Configure(IApplicationBuilder app, ILogger<Startup> logger)
    {
        if (_env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseForwardedHeaders();
            app.UseExceptionHandler("/Error");
        }

        if (!_env.IsRunningInContainer())
        {
            app.UseHttpsRedirection();
        }

        if (_env.IsDevelopment())
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
        
        app.UseEndpoints(routes =>
        {
            // Serves /openapi/{documentName}.json (e.g., v1, operation, session, openid)
            routes.MapOpenApi();
            routes.MapControllerRoute(
                name: "default",
                pattern: "{controller=Identity}/{action=Index}");
        });

        logger.LogInformation("Starting");
    }

    private void AddAuthentication(IServiceCollection services, string connectionString)
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
                options.Authority = Configuration["Rinsen:Outback"];
                options.Audience = Configuration["Rinsen:ClientId"];
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


