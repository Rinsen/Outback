var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", "ddsaFDSS54#$%&hglk");

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
    .WithEnvironment("Login__Debug__Password", "nfgsjknFSDgdf5436545fghscnhfgmDFSAFdfsj4534DFG")
    .WithEnvironment("Login__Runtime__Password", "fds4235sfdgDFGfgde4523sdfgSDdcsfsfdg32");

var outbackApp = builder.AddDotnetProject("outbackapp", "../../OutbackApp/Rinsen.Outback.App/Rinsen.Outback.App.csproj")
    .WithReference(outbackDb)
    .WaitFor(outbackDb)
    .WaitFor(installation)
    .WithExternalHttpEndpoints()
    .WithEnvironment("Rinsen__InvitationCode", "1234");

builder.AddJavaScriptApp("angular", "../../outback-admin", runScriptName: "start")
    .WithNpm(installCommand: "ci")
    .WithReference(outbackApp)
    .WaitFor(outbackApp)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
