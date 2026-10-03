#pragma warning disable OPENAI001
using ModelContextProtocol.Client;
using OpenAI.Responses;
using System.Text.Json.Nodes;

namespace MCPClient.AI.Helper
{
    public class OpenAITool
    {

        public static List<ResponseTool> CreateOpenAITools(
            IList<McpClientTool> mcpTools)
        {
            var tools = new List<ResponseTool>();

            foreach (var mcpTool in mcpTools)
            {
                // ResponseTool.CreateFunctionTool expects the functionParameters as System.BinaryData.
                // McpClientTool.JsonSchema is a JsonElement. Ensure the schema includes
                // "additionalProperties": false at the root (OpenAI requires it) and convert
                // the resulting JSON to BinaryData.
                var raw = mcpTool.JsonSchema.GetRawText();
                JsonNode? node = null;
                try
                {
                    node = JsonNode.Parse(raw);
                }
                catch
                {
                    // If parsing fails, fall back to an empty object so we can still set the required field.
                    node = new JsonObject();
                }

                if (node is not JsonObject obj)
                {
                    obj = new JsonObject();
                }

                // Ensure additionalProperties is present and set to false (required by API schema validation).
                obj["additionalProperties"] = false;

                var functionParameters = System.BinaryData.FromString(obj.ToJsonString());

                var openAITool = ResponseTool.CreateFunctionTool(
                    functionName: mcpTool.Name,
                    functionDescription: mcpTool.Description,
                    functionParameters: functionParameters,
                    strictModeEnabled: true);

                tools.Add(openAITool);
            }

            return tools;
        }

    }
}
#pragma warning restore OPENAI001
