﻿using Rinsen.Outback;
using Rinsen.Outback.Accessors;
using SampleServer.InMemoryAccessors;

namespace SampleServer
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var signingAccessor = new SigningAccessor();
            services.AddSingleton<IAllowedCorsOriginsAccessor, AllowedCorsOriginsAccessor>();
            services.AddSingleton<IClientAccessor, ClientAccessor>();
            services.AddSingleton<IGrantAccessor, GrantAccessor>();
            services.AddSingleton<IScopeAccessor, ScopeAccessor>();
            services.AddSingleton<ITokenSigningAccessor>(signingAccessor);
            services.AddSingleton<IWellKnownSigningAccessor>(signingAccessor);
            services.AddSingleton<IUserInfoAccessor, UserInfoAccessor>();

            services.AddAuthentication()
                .AddJwtBearer(options =>
                {
                    options.Audience = "";
                    options.Authority = "";
                });

            services.AddRinsenOutback();
            services.AddControllersWithViews()
                .AddRinsenOutbackControllers();
        }

        public void Configure(IApplicationBuilder app, ILogger<Startup> logger)
        {
            // Configure the HTTP request pipeline.
            if (!_env.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(routes =>
            {
                routes.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}");
            });
        }
    }
}
