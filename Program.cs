using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace mine_tank_server
{
    public class ClientState
    {
        public Socket socket;
        public byte[] readBuff = new byte[1024];
        public float posX = 0;
        public float posY = 0;
        public short dirX = 0;
        public short dirY = 0;
    }
    class Program
    {
        static Socket listenfd;
        static int buffCount = 0;
        public static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();
        public static void Main(string[] args)
        {
            //Socket
            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //Bind
            IPAddress ipAdr = IPAddress.Parse("0.0.0.0");
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 1234);
            listenfd.Bind(ipEp);
            //listen
            listenfd.Listen(0);
            Console.WriteLine("[server] run success");
            List<Socket> checkRead = new List<Socket>();
            while (true)
            {
                checkRead.Clear();
                checkRead.Add(listenfd);
                foreach(ClientState s in clients.Values)
                {
                    checkRead.Add(s.socket);
                }
                // select
                Socket.Select(checkRead, null, null, 1000);
                // check
                foreach( Socket s in checkRead)
                {
                    if(s == listenfd)
                    {
                        ReadListenfd(s);
                    }
                    else
                    {
                        ReadClientfd(s);
                    }
                }
            }
        }

        public static void ReadListenfd(Socket listenfd)
        {
            Console.WriteLine("Accepted");
            Socket clientfd = listenfd.Accept();
            ClientState state = new ClientState();
            state.socket = clientfd;
            clients.Add(clientfd, state);
        }

        public static void ReadClientfd(Socket clientfd)
        {
            ClientState state = clients[clientfd];
            try
            {
                buffCount = clientfd.Receive(state.readBuff);
            }catch(SocketException ex)
            {
                MethodInfo mei = typeof(EventHandler).GetMethod("OnDisConnect");
                object[] ob = { state };
                mei.Invoke(null, ob);

                clientfd.Close();
                clients.Remove(clientfd);
                Console.WriteLine("Receive SocketException " + ex.ToString());
                return ;
            }

            if(buffCount == 0)
            {
                MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
                object[] ob = { state };
                mei.Invoke(null, ob);

                clientfd.Close();
                clients.Remove(clientfd);
                Console.WriteLine("Socket Close");
                return ;
            }

            OnReceiveData(state);
            return ;
        }

        private static void OnReceiveData(ClientState state)
        {
            if (buffCount <= 2) { return; }
            Int16 bodyLength = BitConverter.ToInt16(state.readBuff, 0);
            if (buffCount < 2 + bodyLength) { return; }
            //broadcast
            int end = 2 + bodyLength;
            string recvStr = System.Text.Encoding.Default.GetString(state.readBuff, 2, end);
            Console.WriteLine("Receive " + recvStr);

            string[] split = recvStr.Split('|');
            string msgName = split[0];
            string msgArgs = split[1];
            string funName = "Msg" + msgName;
            MethodInfo mi = typeof(MsgHandler).GetMethod(funName);
            object[] o = { state, msgArgs };
            mi.Invoke(null, o);

            int count = buffCount - end;
            Array.Copy(state.readBuff, end, state.readBuff, 0, count);
            buffCount -= end;
            OnReceiveData(state);
        }

        public static void Send(ClientState cs, string sendStr)
        {
            byte[] bodyByte = System.Text.Encoding.Default.GetBytes(sendStr);
            Int16 len = (Int16)bodyByte.Length;
            byte[] headByte = BitConverter.GetBytes(len);
            byte[] sendByte = headByte.Concat(bodyByte).ToArray();
            cs.socket.Send(sendByte);
        }
    }
}
