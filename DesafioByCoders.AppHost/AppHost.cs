using Projects;
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

const string dataDir = "data/postgres";

Directory.CreateDirectory(Path.GetFullPath(dataDir));

var postgres = builder.AddPostgres("desafiobycoders-db")
                      .WithBindMount(dataDir, "/var/lib/postgresql/data");

var desafioByCodersDb = postgres.AddDatabase("desafiobycoders");

var api = builder.AddProject<DesafioByCoders_Api>("desafiobycoders-api")
                 .WaitFor(desafioByCodersDb)
                 .WithReference(desafioByCodersDb);

var ui = builder.AddNpmApp("desafiobycoders-ui-web", "../DesafioByCoders.Ui.Web")
                .WithReference(api)
                .WaitFor(api);

builder.Build()
       .Run();