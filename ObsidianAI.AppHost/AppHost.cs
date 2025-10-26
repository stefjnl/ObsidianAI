var builder = DistributedApplication.CreateBuilder(args);

// Add consolidated Web project to orchestration
var web = builder.AddProject<Projects.ObsidianAI_Web>("web");

builder.Build().Run();
