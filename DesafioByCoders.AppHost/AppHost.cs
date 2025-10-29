var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DesafioByCoders_Api>("desafiobycoders-api");

builder.Build().Run();
