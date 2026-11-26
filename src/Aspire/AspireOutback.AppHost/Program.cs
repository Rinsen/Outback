var builder = DistributedApplication.CreateBuilder(args);

// SEC-018: no secret defaults in source. The values below are provided per
// developer, e.g. via the AppHost's user-secrets (UserSecretsId is set in the
// csproj):
//   dotnet user-secrets set "Parameters:sql-password" "<strong-password>"
//   dotnet user-secrets set "Parameters:login-debug-password" "<strong-password>"
//   dotnet user-secrets set "Parameters:login-runtime-password" "<strong-password>"
//   dotnet user-secrets set "Parameters:invitation-code" "<strong-code>"
// or set in the Aspire dashboard. The values previously committed to source are
// treated as compromised and must not be reused.
//
// SEC-020: the invitation code gates anonymous self-registration and must be a
// strong random value of at least 128 bits (the app fails closed below 32
// characters). Generate one with, e.g.:
//   openssl rand -hex 16
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var debugLoginPassword = builder.AddParameter("login-debug-password", secret: true);
var runtimeLoginPassword = builder.AddParameter("login-runtime-password", secret: true);
var invitationCode = builder.AddParameter("invitation-code", secret: true);

var sqlServer = builder.AddSqlServer("outbackSqlServer", sqlPassword)
    .WithDataVolume();

var outbackDb = sqlServer.AddDatabase("Outback");

var installation = builder.AddDotnetProject("outbackinstallation", "../../OutbackApp/Rinsen.Outback.App.Installation/Rinsen.Outback.App.Installation.csproj")
    .WithReference(sqlServer)
    .WaitFor(sqlServer)
    .WithEnvironment("Command", "Install")
    .WithEnvironment("DatabaseName", "Outback")
    .WithEnvironment("Schema", "dbo")
    .WithEnvironment("ConnectionStringName", "outbackSqlServer")
    .WithEnvironment("Login__Debug__Password", debugLoginPassword)
    .WithEnvironment("Login__Runtime__Password", runtimeLoginPassword);

var outbackApp = builder.AddDotnetProject("outbackapp", "../../OutbackApp/Rinsen.Outback.App/Rinsen.Outback.App.csproj")
    .WithReference(outbackDb)
    .WaitFor(outbackDb)
    .WaitFor(installation)
    .WithExternalHttpEndpoints()
    .WithEnvironment("Rinsen__InvitationCode", invitationCode);

// Plain HTML/JS/Bootstrap admin app (src/Admin). No npm dependencies, so no
// WithNpm: Aspire just runs `npm run start`, which starts tools/static-server.mjs.
// WithReference injects the backend URL as OUTBACKAPP; the static server's proxy
// reads it, so the browser only ever talks to one origin (same as production).
builder.AddJavaScriptApp("admin", "../../Admin", runScriptName: "start")
    .WithReference(outbackApp)
    .WaitFor(outbackApp)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
