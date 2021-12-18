using Rinsen.Outback;
using Rinsen.Outback.Accessors;
using SampleServer.InMemoryAccessors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
AddServices(builder.Services);

builder.Services.AddRinsenOutback();
builder.Services.AddControllersWithViews()
    .AddRinsenOutbackControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void AddServices(IServiceCollection services)
{
    services.AddSingleton<IAllowedCorsOriginsAccessor, AllowedCorsOriginsAccessor>();
    services.AddSingleton<IClientAccessor, ClientAccessor>();
    services.AddSingleton<IGrantAccessor, GrantAccessor>();
    services.AddSingleton<IScopeAccessor, ScopeAccessor>();
    services.AddSingleton<ITokenSigningAccessor, SigningAccessor>();
    services.AddSingleton<IWellKnownSigningAccessor, SigningAccessor>();
    services.AddSingleton<IUserInfoAccessor, UserInfoAccessor>();
}

public partial class Program { }
