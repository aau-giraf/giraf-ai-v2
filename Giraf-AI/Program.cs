using Giraf_AI.Configuration;
using Giraf_AI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<ImageService>();
builder.Services.AddOpenApi();
builder.Services.AddAiClients();

var app = builder.Build();

app.MapOpenApi();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
