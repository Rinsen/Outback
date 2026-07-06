var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", "ddsaFDSS54#$%&hglk");

var sqlServer = builder.AddSqlServer("outbackSqlServer", sqlPassword)
    .WithDataVolume();

var outbackDb = sqlServer.AddDatabase("Outback");

var installation = builder.AddProject<Projects.Rinsen_Outback_App_Installation>("outbackinstallation")
    .WithReference(sqlServer)
    .WaitFor(sqlServer)
    .WithEnvironment("Command", "Install")
    .WithEnvironment("DatabaseName", "Outback")
    .WithEnvironment("Schema", "dbo")
    .WithEnvironment("ConnectionStringName", "outbackSqlServer")
    .WithEnvironment("Login__Debug__Password", "nfgsjknFSDgdf5436545fghscnhfgmDFSAFdfsj4534DFG")
    .WithEnvironment("Login__Runtime__Password", "fds4235sfdgDFGfgde4523sdfgSDdcsfsfdg32");

var outbackApp = builder.AddProject<Projects.Rinsen_Outback_App>("outbackapp")
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
