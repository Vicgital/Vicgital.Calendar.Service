using Vicgital.Calendar.Service;
using Vicgital.Calendar.Service.Implementation;
using Vicgital.Calendar.Service.Interceptors;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddConfiguration(Vicgital.Core.Configuration.ConfigurationBuilder.BuildConfiguration());

// WebApplication.CreateBuilder registers default logging providers (Console, Debug, ...).
// AddSerilogLogging (below, via SetupServices) adds Serilog as an *additional* provider rather
// than replacing them, so without this every log line gets written twice - once by the default
// text console provider, once by Serilog's JSON console sink. Must run before SetupServices.
builder.Logging.ClearProviders();

// Add services to the container.
builder.Services.AddGrpc(options => options.Interceptors.Add<ExceptionTranslationInterceptor>());
builder.Services.SetupServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<CalendarService>();
app.MapGet("/", () => "Calendar Service is running!");

app.Run();
