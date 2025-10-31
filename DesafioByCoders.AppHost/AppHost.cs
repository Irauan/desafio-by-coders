using Projects;

var builder = DistributedApplication.CreateBuilder(args);

const string dataDir = "data/postgres";

Directory.CreateDirectory(Path.GetFullPath(dataDir));

var postgres = builder.AddPostgres("desafiobycoders-db")
                      .WithBindMount(dataDir, "/var/lib/postgresql/data");

var desafioByCodersDb = postgres.AddDatabase("desafiobycoders");

builder.AddProject<DesafioByCoders_Api>("desafiobycoders-api")
       .WaitFor(desafioByCodersDb)
       .WithReference(desafioByCodersDb);


builder.Build().Run();
