using Microsoft.AspNetCore.SignalR.Client;
using EnvironmentBuilder.Core.Models;

namespace EnvironmentBuilder.Web.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly IConfiguration _configuration;

    public event Action<ProgressUpdate>? OnProgressUpdate;
    public event Action<string>? OnLogMessage;
    public event Action<OperationResult>? OnOperationComplete;
    public event Action<string>? OnError;
    public event Action<string>? OnConnected;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public SignalRService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task ConnectAsync()
    {
        var apiUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5000";
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{apiUrl}/hubs/progress")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>("Connected", (connectionId) =>
        {
            OnConnected?.Invoke(connectionId);
        });

        _hubConnection.On<ProgressUpdate>("ProgressUpdate", (update) =>
        {
            OnProgressUpdate?.Invoke(update);
        });

        _hubConnection.On<LogEntry>("LogMessage", (entry) =>
        {
            OnLogMessage?.Invoke(entry.Message);
        });

        _hubConnection.On<OperationResult>("OperationComplete", (result) =>
        {
            OnOperationComplete?.Invoke(result);
        });

        _hubConnection.On<string>("Error", (error) =>
        {
            OnError?.Invoke(error);
        });

        await _hubConnection.StartAsync();
    }

    public async Task JoinOperationAsync(string operationId)
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.InvokeAsync("JoinOperation", operationId);
        }
    }

    public async Task LeaveOperationAsync(string operationId)
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.InvokeAsync("LeaveOperation", operationId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}

