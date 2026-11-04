using Giraf_AI.Configuration;
using Giraf_AI.Data;
using Giraf_AI.Services;
using Giraf_AI.Services.AI;
using Giraf_AI.Services.Queue;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<GirafAiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("GirafAiDatabase")));
builder.Services.AddSingleton<IQueueService, QueueService>();
builder.Services.AddHostedService<QueueHostedService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<TranslationService>();
builder.Services.AddScoped<ContentFilterService>();
builder.Services.AddScoped<TitleService>();
builder.Services.AddScoped<ImageGenService>();
builder.Services.AddScoped<IImageGenerationQueueRepository, ImageGenerationQueueRepository>();
builder.Services.AddOpenApi();
builder.Services.AddAiClients();

var app = builder.Build();

app.MapOpenApi();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
