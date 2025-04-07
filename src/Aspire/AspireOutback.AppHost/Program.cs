using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("outback")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.Rinsen_Outback_App_Installation>("outbackinstallation")
    .WithReference(sqlServer)
    .WaitFor(sqlServer)
    .WithEnvironment("Command", "Install")
    .WithEnvironment("DatabaseName", "Outback")
    .WithEnvironment("Schema", "dbo")
    .WithEnvironment("Login__Debug__Password", "nfgsjknFSDgdf5436545fghscnhfgmDFSAFdfsj4534DFG")
    .WithEnvironment("Login__Runtime__Password", "fds4235sfdgDFGfgde4523sdfgSDdcsfsfdg32")
    .WithExternalHttpEndpoints();

builder.Build().Run();
