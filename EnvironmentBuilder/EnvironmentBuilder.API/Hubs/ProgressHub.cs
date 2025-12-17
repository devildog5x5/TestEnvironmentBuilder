using Microsoft.AspNetCore.SignalR;
using EnvironmentBuilder.Core.Models;

namespace EnvironmentBuilder.API.Hubs;

/// <summary>
/// SignalR hub for real-time progress updates
/// </summary>
public class ProgressHub : Hub
{
    public async Task JoinOperation(string operationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, operationId);
        await Clients.Caller.SendAsync("Joined", operationId);
    }

    public async Task LeaveOperation(string operationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, operationId);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}

/// <summary>
/// Service to broadcast progress updates via SignalR
/// </summary>
public class ProgressBroadcaster
{
    private readonly IHubContext<ProgressHub> _hubContext;

    public ProgressBroadcaster(IHubContext<ProgressHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastProgress(string operationId, ProgressUpdate update)
    {
        await _hubContext.Clients.Group(operationId).SendAsync("ProgressUpdate", update);
    }

    public async Task BroadcastLog(string operationId, string message)
    {
        await _hubContext.Clients.Group(operationId).SendAsync("LogMessage", new { Timestamp = DateTime.UtcNow, Message = message });
    }

    public async Task BroadcastComplete(string operationId, OperationResult result)
    {
        await _hubContext.Clients.Group(operationId).SendAsync("OperationComplete", result);
    }

    public async Task BroadcastError(string operationId, string error)
    {
        await _hubContext.Clients.Group(operationId).SendAsync("Error", error);
    }
}

