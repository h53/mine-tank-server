using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace mine_tank_server
{
    public class ByteArray
    {
        const int DEFAULT_SIZE = 1024;
        int initSize = 0;
        private int capacity = 0;
        public byte[] bytes;
        public int readIdx = 0;
        public int writeIdx = 0;
        public int length { get { return writeIdx - readIdx; } }
        public int remain { get { return capacity - writeIdx; } }
        public ByteArray(byte[] defaultBytes)
        {
            bytes = defaultBytes;
            capacity = defaultBytes.Length;
            initSize = defaultBytes.Length;
            readIdx = 0;
            writeIdx = defaultBytes.Length;
        }
        public ByteArray(int size = DEFAULT_SIZE)
        {
            bytes = new byte[size];
            capacity = size;
            initSize = size;
            readIdx = 0;
            writeIdx = 0;
        }

        //public void Resize(int size)
        //{
        //    if (size < length) return;
        //    if (size < initSize) return;
        //    int n = 1;
        //    while (n < size) n *= 2;
        //    capacity = n;
        //    byte[] newBytes = new byte[capacity];
        //    Array.Copy(bytes, readIdx, newBytes, 0, writeIdx - readIdx);
        //    bytes = newBytes;
        //    writeIdx = length;
        //    readIdx = 0;
        //}

        public void CheckAndMoveBytes()
        {
            Console.WriteLine("check bytes ---------");
            if (length < 8)
            {
                Console.WriteLine("Check yes ++++++++++");
                MoveBytes();
            }
        }

        public void MoveBytes()
        {
            Console.Write("MoveBytes, readIdx = " + readIdx);
            Console.Write(" writeIdx = " + writeIdx);
            Console.Write(" length = " + length);
            Console.Write(" remain = " + remain);
            Console.Write(" size = " + capacity);
            Console.WriteLine();
            Array.Copy(bytes, readIdx, bytes, 0, length);
            writeIdx = length;
            readIdx = 0;

            Console.Write("++++++++++ readIdx = " + readIdx);
            Console.Write(" writeIdx = " + writeIdx);
            Console.Write(" length = " + length);
            Console.Write(" remain = " + remain);
            Console.Write(" size = " + capacity);
            Console.WriteLine();
        }

        //public int Write(byte[] bs, int offset, int count)
        //{
        //    if (remain < count)
        //    {
        //        Resize(length + count);
        //    }
        //    Array.Copy(bs, offset, bytes, writeIdx, count);
        //    writeIdx += count;
        //    return count;
        //}

        //public int Read(byte[] bs, int offset, int count)
        //{
        //    count = Math.Min(count, length);
        //    Array.Copy(bytes, 0, bs, offset, count);
        //    readIdx += count;
        //    CheckAndMoveBytes();
        //    return count;
        //}
    }
    public class ClientState
    {
        public Socket socket;
        public ByteArray readBuff = new ByteArray();
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
            int port;
            try
            {
                string portStr = Environment.GetEnvironmentVariable("PORT");
                port = int.Parse(portStr);
            }catch(ArgumentNullException)
            {
                //Console.WriteLine("no PORT env found, use default port: 1234");
                port = 1234;
            }
            IPEndPoint ipEp = new IPEndPoint(ipAdr, port);
            listenfd.Bind(ipEp);
            //listen
            listenfd.Listen(0);
            Console.WriteLine("[server] run success on port: " + port);
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
                foreach(Socket s in checkRead)
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
            ClientState state = new ClientState
            {
                socket = clientfd
            };
            clients.Add(clientfd, state);
        }

        public static void ReadClientfd(Socket clientfd)
        {
            ClientState state = clients[clientfd];
            try
            {
                buffCount = clientfd.Receive(state.readBuff.bytes,state.readBuff.writeIdx, state.readBuff.remain, SocketFlags.None);
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
            Console.WriteLine("buffcount = " + buffCount);
            state.readBuff.writeIdx += buffCount;
            OnReceiveData(state);
            if (state.readBuff.remain < 8)
            {
                //state.readBuff.Resize(state.readBuff.length * 2);
                state.readBuff.MoveBytes();
            }
            return ;
        }

        private static void OnReceiveData(ClientState state)
        {
            if (state.readBuff.length <= 2) { return; }
            Int16 bodyLength = BitConverter.ToInt16(state.readBuff.bytes, state.readBuff.readIdx);
            if (state.readBuff.length < 2 + bodyLength) { return; }
            //broadcast
            state.readBuff.readIdx += 2;
            string recvStr = System.Text.Encoding.ASCII.GetString(state.readBuff.bytes, state.readBuff.readIdx, bodyLength);
            state.readBuff.readIdx += bodyLength;
            state.readBuff.CheckAndMoveBytes();
            Console.WriteLine("Receive " + recvStr);

            string[] split = recvStr.Split('|');
            string msgName = split[0];
            string msgArgs = split[1];
            string funName = "Msg" + msgName;
            MethodInfo mi = typeof(MsgHandler).GetMethod(funName);
            object[] o = { state, msgArgs };
            mi.Invoke(null, o);
            
            OnReceiveData(state);
        }

        public static void Send(ClientState cs, string sendStr)
        {
            byte[] bodyByte = System.Text.Encoding.ASCII.GetBytes(sendStr);
            Int16 len = (Int16)bodyByte.Length;
            byte[] headByte = BitConverter.GetBytes(len);
            byte[] sendByte = headByte.Concat(bodyByte).ToArray();
            cs.socket.Send(sendByte);
        }
    }
}
