#pragma warning disable OPENAI001
using MCP.Infrastructure.AI;
using MCPClient.AI.Helper;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using OpenAI.Responses;
using System.Text.Json;

var mcpServerEndpoint = Environment.GetEnvironmentVariable("MCP_SERVER_ENDPOINT");
if(string.IsNullOrWhiteSpace(mcpServerEndpoint))
{
    Console.WriteLine("MCP_SERVER_ENDPOINT environment variable not set. Set it and restart.");
    return;
};

var clientTransport = new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Endpoint = new Uri(mcpServerEndpoint)
    });

await using var client = await McpClient.CreateAsync(clientTransport);

Console.WriteLine("Connected to MCP Server.");

IList<McpClientTool> tools = await client.ListToolsAsync();
var openAITools = OpenAITool.CreateOpenAITools(tools);

// Prefer keeping secrets out of source - read API key from environment variable.
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("OPENAI_API_KEY environment variable not set. Set it and restart.");
    return;
}

AIClient aiClient = new AIClient(apiKey, openAITools);

var initialQuestion = "What is 10 multiply 20?";

ResponseResult response;
try
{
    response = await aiClient.GenerateAnswerAsync(initialQuestion);
}
catch (Exception ex)
{
    Console.WriteLine("Error calling AI: " + ex.Message);
    return;
}

// Handle function calls iteratively: if the model requests a function call, execute it and
// send back the output; repeat until the model returns assistant messages without function calls.
while (true)
{
    var functionCall = response.OutputItems.OfType<FunctionCallResponseItem>().FirstOrDefault();
    if (functionCall == null)
    {
        // No function call - print assistant messages and exit loop.
        foreach (var outItem in response.OutputItems.OfType<MessageResponseItem>())
        {
            foreach (var c in outItem.Content)
            {
                Console.WriteLine(c.Text);
            }
        }
        break;
    }

    Console.WriteLine($"OpenAI requested tool: {functionCall.FunctionName}");

    var argsJson = functionCall.FunctionArguments.ToString();
    Console.WriteLine($"Arguments: {argsJson}");

    Dictionary<string, object?> arguments;
    using var argumentsDoc = JsonDocument.Parse(argsJson);
        var root = argumentsDoc.RootElement;
        arguments = new Dictionary<string, object?>
        {
            ["firstNumber"] = root.GetProperty("firstNumber").GetInt32(),
            ["secondNumber"] = root.GetProperty("secondNumber").GetInt32()
        };
    

    var toolResult = await client.CallToolAsync(functionCall.FunctionName, arguments);
  

    Console.WriteLine();
    Console.WriteLine("MCP tool result:");
    foreach (var content in toolResult.Content)
    {
        Console.WriteLine(content);
    }

    var functionOutput = string.Join("\n", toolResult.Content.Select(c => c.ToString()));

    try
    {
        response = await aiClient.SendFunctionOutputAsync(
            functionCall.CallId,
            functionCall.FunctionName,
            argsJson,
            functionOutput);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error sending function output to AI: " + ex.Message);
        return;
    }

    // loop will continue and either process another function call or print assistant messages
}
#pragma warning restore OPENAI001

