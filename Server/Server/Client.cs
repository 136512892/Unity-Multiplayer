using System.Net.Sockets;

namespace SK.Framework.Sockets
{
    /// <summary>
    /// 客户端信息类
    /// </summary>
    public class Client
    {
        /// <summary>
        /// 套接字
        /// </summary>
        public Socket socket;
        /// <summary>
        /// 缓冲区
        /// </summary>
        public ByteArray readBuff;
        /// <summary>
        /// 上一次收到PING协议时间
        /// </summary>
        public long lastPingTime = 0;
        /// <summary>
        /// 时间间隔
        /// </summary>
        public static long pingInterval = 30;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="socket">套接字</param>
        public Client(Socket socket)
        {
            this.socket = socket;
            readBuff = new ByteArray();
            lastPingTime = TimeUtility.GetTimeStamp();
        }
    }
}