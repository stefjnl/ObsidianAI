var builder = DistributedApplication.CreateBuilder(args);

// Add API project to orchestration
var api = builder.AddProject<Projects.ObsidianAI_Api>("api");

// Add Web project to orchestration with reference to API
var web = builder.AddProject<Projects.ObsidianAI_Web>("web")
    .WithReference(api);

builder.Build().Run();
