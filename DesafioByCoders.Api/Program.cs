using System.Data;
using System.Runtime.CompilerServices;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using DesafioByCoders.Api.Features;
using DesafioByCoders.Api.Features.Stores.List;
using DesafioByCoders.Api.Features.Transactions;
using DesafioByCoders.Api.Handlers;
using DesafioByCoders.Api.Middleware;
using Npgsql;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

[assembly: InternalsVisibleTo("DesafioByCoders.Api.Tests.Units")]
[assembly: InternalsVisibleTo("DesafioByCoders.Api.Tests.Integrations")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace DesafioByCoders.Api;

internal class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog for structured logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        try
        {
            Log.Information("Starting DesafioByCoders API application");
            
            var builder = WebApplication.CreateBuilder(args);
            
            // Use Serilog for logging
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
            );

            builder.AddServiceDefaults();

        // Add services to the container.
        var connectionString = builder.Configuration
                               .GetConnectionString("desafiobycoders");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The settings ConnectionStrings:desafiobycoders is required.");
        }
        
        // Add custom health checks for database
        builder.Services.AddHealthChecks()
               .AddNpgSql(
                   connectionString,
                   name: "postgresql",
                   tags: new[] { "ready", "db" },
                   timeout: TimeSpan.FromSeconds(5)
               )
               .AddDbContextCheck<TransactionDbContext>(
                   name: "transactiondb_context",
                   tags: new[] { "ready", "db", "ef" }
               );
        
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

        // Add Serilog request logging (before UseHttpsRedirection)
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
            };
        });

        // Add global exception handling middleware
        app.UseExceptionHandling();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        // Minimal APIs
        app.MapStoreList();

        app.MapControllers();

            Log.Information("DesafioByCoders API started successfully");
            
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.Information("Shutting down DesafioByCoders API");
            Log.CloseAndFlush();
        }
    }
}