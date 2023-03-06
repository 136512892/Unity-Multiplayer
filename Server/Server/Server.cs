using ProtoBuf;
using System.Net;
using System.Net.Sockets;

namespace SK.Framework.Sockets
{
    /// <summary>
    /// 服务器
    /// </summary>
    public class Server
    {
        #region Variables
        //定义套接字
        private static Socket? socket;
        //用于检测可读性的Socket列表
        private readonly static List<Socket> checkReadableList = new List<Socket>();
        //客户端Socket及客户端信息字典
        private readonly static Dictionary<Socket, Client> clients = new Dictionary<Socket, Client>();
        //写入队列
        private readonly static Queue<ByteArray> writeQueue = new Queue<ByteArray>();
        #endregion

        #region Private Methods
        private static void Main(string[] args)
        {
            Init(8801);
        }

        //服务器初始化
        //port: 端口
        private static void Init(int port)
        {
            Console.WriteLine("服务器启动...");
            //Socket Tcp协议
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //服务器IP地址
            IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);
            //Bind
            socket.Bind(ipEndPoint);
            //Listen 开启监听
            socket.Listen(0);

            //循环
            while (true)
            {
                //首先重置用于检测可读性的Socket列表
                OnCheckReadableListReset();
                //使用Select检测可读 实现non-block非阻塞方式
                //arg4: 超时值 单位毫秒 此处设置1000表示 1秒内没有可读消息时停止阻塞 返回空的列表
                Socket.Select(checkReadableList, null, null, 1000);
                //遍历检查可读对象
                for (int i = 0; i < checkReadableList.Count; i++)
                {
                    Socket s = checkReadableList[i];
                    if (s == socket) OnListenEvent(s);
                    else OnClientEvent(s);
                }

                //检测心跳
                //CheckPing();
            }
        }
        private static void OnCheckReadableListReset()
        {
            checkReadableList.Clear();
            //进行Select的列表包含监听套接字socket以及每个已经连接的客户端套接字
            checkReadableList.Add(socket);
            foreach (Client client in clients.Values)
            {
                checkReadableList.Add(client.socket);
            }
        }
        //监听事件
        private static void OnListenEvent(Socket s)
        {
            try
            {
                //接受客户端连接
                Socket socket = s.Accept();
                Console.WriteLine($"客户端接入: {socket.RemoteEndPoint}");
                Client client = new Client(socket);
                //加入字典
                clients.Add(socket, client);
            }
            catch (SocketException error)
            {
                Console.WriteLine($"客户端接入失败: {error}");
            }
        }
        //客户端消息事件
        private static void OnClientEvent(Socket s)
        {
            //从字典中获取该客户端信息类
            Client client = clients[s];
            //该客户端的读缓冲区
            ByteArray readBuff = client.readBuff;
            //如果缓冲区剩余空间不足 清除
            if (readBuff.remain <= 0)
            {
                OnReceiveData(client);
                readBuff.MoveBytes();
            }
            //如果依然不足 接收数据失败 关闭客户端连接 返回
            //缓冲区默认大小为1024 根据最大单条数据长度进行调整
            if (readBuff.remain <= 0)
            {
                Console.WriteLine($"接收数据失败，超出缓冲区长度。 {s.RemoteEndPoint}");
                //关闭客户端连接
                Close(client);
                return;
            }
            //接收数据长度
            int length;
            try
            {
                length = s.Receive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0);
            }
            catch (SocketException error)
            {
                Console.WriteLine($"接收数据失败: {error}. {s.RemoteEndPoint}");
                Close(client);
                return;
            }
            //客户端主动断开连接时 收到数据长度为0 此时调用Close关闭连接
            if (length <= 0)
            {
                Close(client);
                return;
            }
            //处理数据
            readBuff.writeIdx += length;
            OnReceiveData(client);
            //移动缓冲区
            readBuff.CheckAndMoveBytes();
        }
        //数据处理
        private static void OnReceiveData(Client client)
        {
            ByteArray readBuff = client.readBuff;
            byte[] bytes = readBuff.bytes;
            //根据缓冲区中前两个字节判断是否接收到一条完整的协议
            //如果接收到完整协议，进行解析
            //如果没有接收到完整协议，等待下一条协议
            if (readBuff.length < 2) return;
            Int16 bodyLength = (Int16)((bytes[readBuff.readIdx + 1] << 8) | bytes[readBuff.readIdx]);
            if (readBuff.length < bodyLength) return;
            readBuff.readIdx += 2;
            //解析协议名
            string protoName = ProtoUtility.DecodeName(readBuff.bytes, readBuff.readIdx, out int nameLength);
            //协议名不可为空
            if (string.IsNullOrEmpty(protoName))
            {
                Console.WriteLine($"协议名解码失败：{client.socket.RemoteEndPoint}");
                Close(client);
                return;
            }
            readBuff.readIdx += nameLength;
            //解析协议体
            int bodyCount = bodyLength - nameLength;
            IExtensible proto = ProtoUtility.Decode(protoName, readBuff.bytes, readBuff.readIdx, bodyCount);
            readBuff.readIdx += bodyCount;
            readBuff.CheckAndMoveBytes();
            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine(string.Format("接收到客户端{0}数据：{1}", client.socket.RemoteEndPoint, protoName));
            //处理消息
            DealMessage(client, protoName, proto);
            //继续读取消息
            if (readBuff.length > 2)
            {
                OnReceiveData(client);
            }
        }
        //发送回调
        private static void SendCallback(IAsyncResult ar)
        {
            Socket? socket = ar.AsyncState as Socket;
            //状态判断
            if (socket == null || !socket.Connected) return;
            //结束发送
            int length = socket.EndSend(ar);
            ByteArray? byteArray;
            lock (writeQueue)
            {
                byteArray = writeQueue.First();
            }
            //完整发送
            byteArray.readIdx += length;
            if (byteArray.length == 0)
            {
                lock (writeQueue)
                {
                    writeQueue.Dequeue();
                    byteArray = writeQueue.Count > 0 ? writeQueue.First() : null;
                }
            }
            //继续发送
            if (byteArray != null)
            {
                socket.BeginSend(byteArray.bytes, byteArray.readIdx, byteArray.remain, 0, SendCallback, socket);
            }
        }
        //Ping检查
        private static void CheckPing()
        {
            long ts = TimeUtility.GetTimeStamp();
            foreach (Client client in clients.Values)
            {
                if(ts - client.lastPingTime > Client.pingInterval * 4)
                {
                    Close(client);
                    return;
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 向客户端发送协议(单点发送)
        /// </summary>
        /// <param name="client">客户端</param>
        /// <param name="proto">协议</param>
        public static void Send(Client client, IExtensible proto)
        {
            //状态判断
            if (client == null || !client.socket.Connected) return;
            //数据编码
            byte[] nameBytes = ProtoUtility.EncodeName(proto);
            byte[] bodyBytes = ProtoUtility.Encode(proto);
            int length = nameBytes.Length + bodyBytes.Length;
            byte[] sendBytes = new byte[2 + length];
            //组装长度
            sendBytes[0] = (byte)(length % 256);
            sendBytes[1] = (byte)(length / 256);
            //组装名字
            Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
            //组装消息体
            Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);
            //写入队列
            ByteArray byteArray = new ByteArray(sendBytes);
            lock (writeQueue)
            {
                writeQueue.Enqueue(byteArray);
            }
            if (writeQueue.Count == 1)
            {
                //发送
                client.socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, client.socket);
            }    
        }
        /// <summary>
        /// 向所有客户端发送协议(广播)
        /// </summary>
        /// <param name="proto">协议</param>
        public static void Send(IExtensible proto)
        {
            foreach (Client client in clients.Values)
            {
                Send(client, proto);
            }
        }
        /// <summary>
        /// 向指定客户端之外的所有客户端发送协议
        /// </summary>
        /// <param name="proto">协议</param>
        /// <param name="except">不需要发送的客户端</param>
        public static void Send(IExtensible proto, Client except)
        {
            foreach (Client client in clients.Values)
            {
                if (client != except)
                {
                    Send(client, proto);
                }
            }
        }
        /// <summary>
        /// 关闭客户端连接
        /// </summary>
        /// <param name="client">客户端</param>
        public static void Close(Client client)
        {
            Console.WriteLine($"客户端关闭：{client.socket.RemoteEndPoint}");
            //TODO：通知其他客户端

            //关闭Socket连接
            client.socket.Close();
            //从字典中移除
            clients.Remove(client.socket);
        }
        #endregion

        #region >> 数据处理
        //处理消息
        private static void DealMessage(Client sender, string protoName, IExtensible message)
        {
            //Console.WriteLine(protoName);
            switch (protoName)
            {
                case "proto.AvatarProperty.AvatarProperty": OnAvatarPropertyMessage(sender, message); break;
                default: break;
            }
        }

        //Avatar属性
        private static void OnAvatarPropertyMessage(Client sender, IExtensible message)
        {
            //向除了Sender本身之外的客户端发送同步消息
            Send(message, sender);
        }
        #endregion
    }
}