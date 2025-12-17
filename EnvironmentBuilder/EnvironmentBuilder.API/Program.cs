using EnvironmentBuilder.API.Hubs;
using EnvironmentBuilder.API.Services;
using EnvironmentBuilder.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Environment Builder API", Version = "v1" });
});

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add CORS for web client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register services
builder.Services.AddSingleton<EnvironmentManager>();
builder.Services.AddSingleton<EnvironmentConfig>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapHub<ProgressHub>("/hubs/progress");

// Minimal API endpoints
app.MapGet("/", () => new 
{ 
    Name = "Environment Builder API",
    Version = "1.0.0",
    Tagline = "Test Brutally - Build Your Level of Complexity"
});

app.MapGet("/api/presets", () => new[]
{
    ComplexityPreset.Simple,
    ComplexityPreset.Medium,
    ComplexityPreset.Complex,
    ComplexityPreset.Brutal
});

app.Run();

