using Giraf_AI.Data;
using Giraf_AI.Configuration;
using Giraf_AI.Models;
using Giraf_AI.Services;
using Giraf_AI.Services.AI;
using Giraf_AI.Services.Queue;
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
dataSourceBuilder.MapEnum<IdType>();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddControllers();
builder.Services.AddSingleton<IQueueService, QueueService>();
builder.Services.AddHostedService<QueueHostedService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource));
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