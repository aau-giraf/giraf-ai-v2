using Giraf_AI.Data;
using Giraf_AI.Configuration;
using Giraf_AI.Models;
using Giraf_AI.Services;
using Giraf_AI.Services.AI;
using Giraf_AI.Services.Queue;
using Giraf_AI.Services.TTS;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    Path.Combine("DataBase", "appsettings.json"),
    optional: true,
    reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<IdType>("id_type");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddControllers();
builder.Services.AddSingleton<IQueueService, QueueService>();
builder.Services.AddHostedService<QueueHostedService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, npgsqlOptions =>
        npgsqlOptions.MapEnum<IdType>("id_type")));
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<LlmService>();
builder.Services.AddScoped<ImageGenService>();
builder.Services.AddOpenApi();
builder.Services.AddAiClients();

// Each TTS provider implements the same interface; additional engines can be added later.
builder.Services.AddHttpClient<PlapreTtsProvider>(client =>
{
    string url = builder.Configuration["Tts:PlapreBaseUrl"]
                 ?? "http://localhost:8000/";

    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddTransient<ITtsProvider>(serviceProvider =>
    serviceProvider.GetRequiredService<PlapreTtsProvider>());
builder.Services.AddScoped<TtsGenerationService>();

// Separate from the existing image queue: one TTS worker, bounded FIFO queue.
// TTS has no database or file storage; the HTTP response carries the WAV.
builder.Services.AddSingleton<TtsQueueService>();
builder.Services.AddHostedService<TtsQueueWorker>();

var app = builder.Build();

app.MapOpenApi();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
