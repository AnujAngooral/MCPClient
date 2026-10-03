using MCPClient.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MCP.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, string mcpServerEndpoint, string openAiApiKey)
        {
           
            services.AddScoped<IMcpChatService>(_ => new McpChatService(mcpServerEndpoint,openAiApiKey));


            return services;
        }
    }
}
