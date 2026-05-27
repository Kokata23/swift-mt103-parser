using NLog;
using NLog.Web;
using SwiftMT103Parser.Data;
using SwiftMT103Parser.Services;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    logger.Info("Starting SwiftMT103Parser");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "SwiftMT103Parser API", Version = "v1" });
    });

    builder.Services.AddSingleton<DatabaseService>();
    builder.Services.AddSingleton<Mt103ParserService>();

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SwiftMT103Parser v1");
        c.RoutePrefix = string.Empty;
    });

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    LogManager.Shutdown();
}
