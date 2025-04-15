using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("outbackSqlServer")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

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

builder.AddProject<Projects.Rinsen_Outback_App>("outbackapp")
    .WithReference(outbackDb)
    .WaitFor(outbackDb)
    .WaitFor(installation)
    .WithExternalHttpEndpoints()
    .WithEnvironment("Rinsen__InvitationCode", "1234")
    .WithUrl("https://localhost:5001");

builder.Build().Run();
