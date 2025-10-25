namespace be_movie_booking.Hubs;

using Microsoft.AspNetCore.SignalR;

public class AppHub : Hub
{
    // Method để cho phép client tham gia vào một nhóm
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
    
    // Method để cho phép client rời khỏi một nhóm
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
