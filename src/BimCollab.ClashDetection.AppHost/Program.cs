var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.BimCollab_ClashDetection_Api>("clash-detection-api");

builder.Build().Run();
