#pragma warning disable OPENAI001
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Infrastructure.AI
{
    public sealed class AIClient
    {
        private readonly ResponsesClient _client;
        private readonly List<ResponseTool> _tools;
        private readonly string _model;

        public AIClient(string apiKey, IEnumerable<ResponseTool> tools, string model = "gpt-5.2")
        {
            _client = new ResponsesClient(apiKey ?? throw new ArgumentNullException(nameof(apiKey)));
            _tools = tools?.ToList() ?? new List<ResponseTool>();
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        // Build a fresh CreateResponseOptions for each call to avoid stateful accumulation of InputItems
        private CreateResponseOptions CreateOptions()
        {
            var options = new CreateResponseOptions { Model = _model };
            foreach (var t in _tools)
            {
                options.Tools.Add(t);
            }
            return options;
        }

        public async Task<ResponseResult> GenerateAnswerAsync(string question, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("question is required", nameof(question));

            var options = CreateOptions();
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(question));

            var response = await _client.CreateResponseAsync(options, cancellationToken);
            return response;
        }

        // Send the function call (as an input item) and the function output back to the model,
        // then return the follow-up response. The Responses API requires a prior function_call
        // input with the same call id before sending a FunctionCallOutputResponseItem.
        public async Task<ResponseResult> SendFunctionOutputAsync(
            string functionCallId,
            string functionName,
            string functionArgumentsJson,
            string functionOutput,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(functionCallId))
                throw new ArgumentException("functionCallId is required", nameof(functionCallId));

            var options = CreateOptions();

            var argsBinary = System.BinaryData.FromString(functionArgumentsJson ?? string.Empty);

            options.InputItems.Add(ResponseItem.CreateFunctionCallItem(functionCallId, functionName, argsBinary));
            options.InputItems.Add(new FunctionCallOutputResponseItem(functionCallId, functionOutput ?? string.Empty));

            var response = await _client.CreateResponseAsync(options, cancellationToken);
            return response;
        }

    }
}

#pragma warning restore OPENAI001