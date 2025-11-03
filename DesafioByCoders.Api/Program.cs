using System.Data;
using System.Runtime.CompilerServices;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using DesafioByCoders.Api.Features;
using DesafioByCoders.Api.Features.Stores.List;
using DesafioByCoders.Api.Features.Transactions;
using DesafioByCoders.Api.Handlers;
using Npgsql;
using Scalar.AspNetCore;

[assembly: InternalsVisibleTo("DesafioByCoders.Api.Tests.Units")]
[assembly: InternalsVisibleTo("DesafioByCoders.Api.Tests.Integrations")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace DesafioByCoders.Api;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        // Add services to the container.
        var connectionString = builder.Configuration
                               .GetConnectionString("desafiobycoders");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The settings ConnectionStrings:desafiobycoders is required.");
        }
        
        builder.Services.AddTransactionSlice(connectionString);
        
        builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));
        
        builder.Services.AddApiVersioning(o =>
            {
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
                o.ReportApiVersions = true;
            }
        ).AddApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV"; // v1, v2
                o.SubstituteApiVersionInUrl = true;
            }
        );
        
        builder.Services.Scan(scan => scan.FromAssemblyOf<Program>()
                                          .AddClasses(c => c.AssignableTo(typeof(IHandler<>)), publicOnly: false)
                                          .AsImplementedInterfaces()
                                          .WithScopedLifetime()
                                          .AddClasses(c => c.AssignableTo(typeof(IHandler<,>)), publicOnly: false)
                                          .AsImplementedInterfaces()
                                          .WithScopedLifetime()
        );
        
        builder.Services.Decorate(typeof(IHandler<,>), typeof(LoggingHandlerDecorator<,>));
        
        builder.Services.AddControllers();
        
        builder.Services.AddOpenApi();
        
        builder.Services.AddHostedService<ApplyMigrations>();
        
        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var apiVersionDesc in provider.ApiVersionDescriptions)
            {
                app.MapOpenApi($"/openapi/{apiVersionDesc.GroupName}.json")
                   .WithGroupName(apiVersionDesc.GroupName);
            }

            app.MapScalarApiReference(options =>
                {
                    options.Title = "Desafio ByCoders API Docs";

                    foreach (var apiVersionDesc in provider.ApiVersionDescriptions)
                    {
                        options.AddDocument(apiVersionDesc.GroupName, routePattern: $"/openapi/{apiVersionDesc.GroupName}.json");
                    }
                }
            );
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        // Minimal APIs
        app.MapStoreList();

        app.MapControllers();

        app.Run();
    }
}