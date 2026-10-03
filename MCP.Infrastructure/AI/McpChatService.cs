#pragma warning disable OPENAI001

using MCP.Infrastructure.AI;
using MCPClient.AI.Helper;
using MCPClient.Application.Interfaces;
using ModelContextProtocol.Client;
using OpenAI.Responses;
using System.Text.Json;

public class McpChatService : IMcpChatService
{
    private readonly string _mcpServerEndpoint;
    private readonly string _openAiApiKey;

    public McpChatService(
        string mcpServerEndpoint,
        string openAiApiKey)
    {
        _mcpServerEndpoint = mcpServerEndpoint;
        _openAiApiKey = openAiApiKey;
    }

    public async Task<string> AskAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        var clientTransport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(_mcpServerEndpoint)
            });

        await using var client =
            await McpClient.CreateAsync(
                clientTransport,
                cancellationToken: cancellationToken);

        IList<McpClientTool> tools =
            await client.ListToolsAsync(
                cancellationToken: cancellationToken);

        var openAITools =
            OpenAITool.CreateOpenAITools(tools);

        var aiClient = new AIClient(
            _openAiApiKey,
            openAITools);

        var response =
            await aiClient.GenerateAnswerAsync(question);

        while (true)
        {
            var functionCall = response.OutputItems
                .OfType<FunctionCallResponseItem>()
                .FirstOrDefault();

            if (functionCall == null)
            {
                return string.Join(
                    "\n",
                    response.OutputItems
                        .OfType<MessageResponseItem>()
                        .SelectMany(x => x.Content)
                        .Select(x => x.Text)
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
            }

            var argsJson =
                functionCall.FunctionArguments.ToString();

            Dictionary<string, object?> arguments;

            using (var argumentsDoc =
                   JsonDocument.Parse(argsJson))
            {
                var root = argumentsDoc.RootElement;

                arguments = new Dictionary<string, object?>
                {
                    ["firstNumber"] =
                        root.GetProperty("firstNumber").GetInt32(),

                    ["secondNumber"] =
                        root.GetProperty("secondNumber").GetInt32()
                };
            }

            var toolResult =
                await client.CallToolAsync(
                    functionCall.FunctionName,
                    arguments,
                    cancellationToken: cancellationToken);

            var functionOutput = string.Join(
                "\n",
                toolResult.Content.Select(x => x.ToString()));

            response =
                await aiClient.SendFunctionOutputAsync(
                    functionCall.CallId,
                    functionCall.FunctionName,
                    argsJson,
                    functionOutput);
        }
    }
}

#pragma warning restore OPENAI001