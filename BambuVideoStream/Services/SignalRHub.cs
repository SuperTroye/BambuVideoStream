using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace BambuVideoStream
{
    public class SignalRHub : Hub
    {


        public SignalRHub()
        {
        }


        public async Task SendPrintMessage(PrintMessage p)
        {
            await Clients.All.SendAsync("SendPrintMessage", p);
        }


    }
}
