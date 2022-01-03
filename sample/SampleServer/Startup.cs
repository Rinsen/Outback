using Rinsen.Outback;
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
            services.AddSingleton<IAllowedCorsOriginsAccessor, AllowedCorsOriginsAccessor>();
            services.AddSingleton<IClientAccessor, ClientAccessor>();
            services.AddSingleton<IGrantAccessor, GrantAccessor>();
            services.AddSingleton<IScopeAccessor, ScopeAccessor>();
            services.AddSingleton<ITokenSigningAccessor, SigningAccessor>();
            services.AddSingleton<IWellKnownSigningAccessor, SigningAccessor>();
            services.AddSingleton<IUserInfoAccessor, UserInfoAccessor>();

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

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            ExtensionPointForTestMiddleware(app);

            app.UseAuthorization();

            app.UseEndpoints(routes =>
            {
                routes.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Identity}/{action=Index}");
            });


        }

        protected virtual void ExtensionPointForTestMiddleware(IApplicationBuilder app)
        {
            
        }
    }
}
