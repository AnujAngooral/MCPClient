namespace MCPClient.Application.Interfaces
{
    public interface IMcpChatService
    {
        Task<string> AskAsync(string question, CancellationToken cancellationToken = default);
    }
}
