using MCP.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


var mcpServerEndpoint =
    Environment.GetEnvironmentVariable("MCP_SERVER_ENDPOINT")
    ?? throw new InvalidOperationException(
        "MCP_SERVER_ENDPOINT is not configured.");

var openAiApiKey =
    Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException(
        "OPENAI_API_KEY is not configured.");

builder.Services.AddInfrastructure(mcpServerEndpoint, openAiApiKey);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
