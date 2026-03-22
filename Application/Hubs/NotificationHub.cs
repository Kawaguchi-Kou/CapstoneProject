using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Application.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinSchedule(string scheduleId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, scheduleId);
        }

        public async Task LeaveSchedule(string scheduleId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, scheduleId);
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            Console.WriteLine($"Disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(ex);
        }
    }
}
