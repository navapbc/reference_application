using Microsoft.OpenApi.Models;
using PIQI_Engine.Server.Engines;
using PIQI_Engine.Server.Services;
using Serilog;
using System.Reflection;

namespace PIQI_Engine.Server;

public partial class Program
{
    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        // Add CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        OpenApiInfo openApi = builder.Configuration.GetSection("ApiDefinition").Get<OpenApiInfo>();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(openApi.Version, openApi);

            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
        });

        builder.Services.AddSingleton<FileCacheService>();

        // Add engine
        builder.Services.AddScoped<PIQIEngine>();
        builder.Services.AddScoped<ReferenceDataEngine>();

        // Add client for FHIR server
        builder.Services.AddHttpClient<IFHIRClientProvider, FHIRClientProvider>(client =>
        {
            // Pull base URL from configuration
            var baseUrl = builder.Configuration["Fhir:BaseUrl"];

            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");
        });
        // Client for API SAMs, etc.
        builder.Services.AddHttpClient();

        builder.Services.AddScoped<SAMReferenceDataService>();

        var app = builder.Build();

        var loggerFactory = app.Services.GetService<ILoggerFactory>();
        var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().CreateLogger();
        loggerFactory.AddSerilog(logger);

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.UseCors("AllowAll");

        app.MapControllers();

        app.MapFallbackToFile("/index.html");

        app.Run();
    }
}
