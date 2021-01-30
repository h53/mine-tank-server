using System;
using System.Collections.Generic;
using System.Text;

namespace mine_tank_server
{
    class MsgHandler
    {
        public static void MsgEnter(ClientState c, string msgArgs)
        {
            Console.WriteLine("MsgEnter " + msgArgs);
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            float posX = float.Parse(split[1]);
            float posY = float.Parse(split[2]);
            short dirX = short.Parse(split[3]);
            short dirY = short.Parse(split[4]);

            c.posX = posX;
            c.posY = posY;
            c.dirX = dirX;
            c.dirY = dirY;

            string sendStr = "Enter|" + msgArgs;
            foreach(ClientState cs in Program.clients.Values)
            {
                Program.Send(cs,sendStr);
            }
        }

        public static void MsgList(ClientState c, string msgArgs)
        {
            Console.WriteLine("MsgList " + msgArgs);
            string sendStr = "List|";
            foreach(ClientState cs in Program.clients.Values)
            {
                sendStr += cs.socket.RemoteEndPoint.ToString() + "," +
                    cs.posX + "," +
                    cs.posY + "," +
                    cs.dirX + "," +
                    cs.dirY + ",";
            }
            Program.Send(c, sendStr);
        }

        public static void MsgMove(ClientState c,string msgArgs)
        {
            Console.WriteLine("MsgMove " + msgArgs);
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            float posX = float.Parse(split[1]);
            float posY = float.Parse(split[2]);
            short dirX = short.Parse(split[3]);
            short dirY = short.Parse(split[4]);

            c.posX = posX;
            c.posY = posY;
            c.dirX = dirX;
            c.dirY = dirY;

            string sendStr = "Move|" + msgArgs;
            foreach (ClientState cs in Program.clients.Values)
            {
                Program.Send(cs, sendStr);
            }
        }

        public static void MsgFire(ClientState c, string msgArgs)
        {
            Console.WriteLine("MsgFire " + msgArgs);
            string sendStr = "Fire|" + msgArgs;
            foreach (ClientState cs in Program.clients.Values)
            {
                Program.Send(cs, sendStr);
            }
        }

        public static void MsgHit(ClientState c, string msgArgs)
        {
            Console.WriteLine("MsgHit " + msgArgs);
            string sendStr = "Hit|" + msgArgs;
            foreach (ClientState cs in Program.clients.Values)
            {
                Program.Send(cs, sendStr);
            }
        }
    }
}
