var builder = DistributedApplication.CreateBuilder(args);

// Add API proj to orchestration
var api = builder.AddProject<Projects.ObsidianAI_Api>("api");

builder.Build().Run();
