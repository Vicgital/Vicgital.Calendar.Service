using Vicgital.Calendar.Service;
using Vicgital.Calendar.Service.Implementation;
using Vicgital.Grpc;

var builder = VicgitalGrpcService.CreateWebApplicationBuilder(args);

// Add services to the container.
builder.Services.SetupServices();
var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseRouting();
app.MapGrpcService<CalendarService>();
app.MapVicgitalGrpcEndpoints();

app.Run();

