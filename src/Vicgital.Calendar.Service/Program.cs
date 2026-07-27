using Vicgital.Calendar.Service.Implementation;
using Vicgital.Calendar.Service;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddConfiguration(Vicgital.Core.Configuration.ConfigurationBuilder.BuildConfiguration());

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.SetupServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<CalendarService>();
app.MapGet("/", () => "Calendar Service is running!");

app.Run();
