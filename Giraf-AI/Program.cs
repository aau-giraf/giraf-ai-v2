using Giraf_AI.Configuration;
using Giraf_AI.Services;
using Giraf_AI.Services.AI;
using Giraf_AI.Services.Queue;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IQueueService, QueueService>();
builder.Services.AddHostedService<QueueHostedService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<TranslationService>();
builder.Services.AddScoped<ContentFilterService>();
builder.Services.AddScoped<TitleService>();
builder.Services.AddScoped<ImageGenService>();
builder.Services.AddOpenApi();
builder.Services.AddAiClients();

var app = builder.Build();

app.MapOpenApi();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
