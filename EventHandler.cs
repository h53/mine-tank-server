using System;
using System.Collections.Generic;
using System.Text;

namespace mine_tank_server
{
    public class EventHandler
    {
        public static void OnDisconnect(ClientState c)
        {
            Console.WriteLine("OnDisconnect");
            string desc = c.socket.RemoteEndPoint.ToString();
            string sendStr = "Leave|" + desc;
            foreach(ClientState cs in Program.clients.Values)
            {
                Program.Send(cs, sendStr);
            }
        }
    }
}
