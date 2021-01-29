using System;
using System.Collections.Generic;
using System.Text;

namespace mine_tank_server
{
    class EventHandler
    {
        public static void OnDisconnect(ClientState c)
        {
            Console.WriteLine("OnDisconnect");
        }
    }
}
