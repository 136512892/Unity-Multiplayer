using System;
using ProtoBuf;
using UnityEngine;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Generic;

using Multiplayer;

namespace SK.Framework.Sockets
{
    [Package("Socket", "1.0.0")]
    public class NetworkManager : MonoBehaviour
    {
        #region Variables & Properties
        //定义套接字
        private Socket socket;
        //接收缓冲区
        private ByteArray readBuff;
        //写入队列
        private Queue<ByteArray> writeQueue;
        //是否正在连接
        private bool isConnecting = false;
        //是否正在关闭
        private bool isClosing = false;
        //消息列表
        private List<IExtensible> msgList;
        //消息列表长度
        private int msgCount;
        //每一次Update处理的消息量
        private readonly int MAX_MESSAGE_FIRE = 10;
        
        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected
        {
            get { return socket != null && socket.Connected; }
        }
        #endregion

        #region >> Private Methods
        //初始化状态
        private void Init()
        {
            //Socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //接受缓冲区
            readBuff = new ByteArray();
            //写入队列
            writeQueue = new Queue<ByteArray>();
            //是否正在连接
            isConnecting = false;
            //是否正在关闭
            isClosing = false;
            //消息列表
            msgList = new List<IExtensible>();
            //消息列表长度
            msgCount = 0;
        }
        //Connect回调
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndConnect(ar);
                isConnecting = false;
                Debug.Log("成功连接服务端");

                //开始接收数据
                socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
            }
            catch (SocketException error)
            {
                Debug.LogError(string.Format("连接服务端失败：{0} {1}", error.Source, error.Message));
                isConnecting = false;
                //TODO：失败回调
            }
        }
        //Receive回调
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)(ar.AsyncState);
                //获取接收数据长度
                int count = socket.EndReceive(ar);
                if (count == 0)
                {
                    Close();
                    Debug.Log("关闭与服务端的连接");
                    return;
                }
                readBuff.writeIdx += count;
                //处理二进制消息
                OnReceiveData();
                //继续接收数据
                if (readBuff.remain < 8)
                {
                    readBuff.MoveBytes();
                    readBuff.ReSize(readBuff.length * 2);
                }
                socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
            }
            catch (SocketException error)
            {
                Debug.LogError(string.Format("接收数据失败：{0} {1}", error.Source, error.Message));
            }
        }
        //数据处理
        private void OnReceiveData()
        {
            //消息长度
            if (readBuff.length <= 2) return;
            //获取消息体长度
            int readIdx = readBuff.readIdx;
            byte[] bytes = readBuff.bytes;
            Int16 bodyLength = (Int16)((bytes[readIdx + 1] << 8) | bytes[readIdx]);
            if (readBuff.length < bodyLength + 2) return;
            readBuff.readIdx += 2;
            //解析协议名
            string protoName = ProtoUtility.DecodeName(readBuff.bytes, readBuff.readIdx, out int nameCount);
            if (string.IsNullOrEmpty(protoName))
            {
                Debug.Log("协议名解码失败");
                return;
            }
            readBuff.readIdx += nameCount;
            //解析协议体
            int bodyCount = bodyLength - nameCount;
            IExtensible proto = ProtoUtility.Decode(protoName, readBuff.bytes, readBuff.readIdx, bodyCount);
            readBuff.readIdx += bodyCount;
            readBuff.CheckAndMoveBytes();
            //添加到消息队列
            lock (msgList)
            {
                msgList.Add(proto);
            }
            msgCount++;
            //继续读取消息
            if (readBuff.length > 2)
            {
                OnReceiveData();
            }
        }
        //Send回调
        private void SendCallback(IAsyncResult ar)
        {
            //获取State、EndSend的处理
            Socket socket = (Socket)ar.AsyncState;
            //状态判断
            if (socket == null || !socket.Connected) return;
            //EndSend
            int count = socket.EndSend(ar);
            //获取写入队列第一条数据
            ByteArray ba;
            lock (writeQueue)
            {
                ba = writeQueue.First();
            }
            //完整发送
            ba.readIdx += count;
            if (ba.length == 0)
            {
                lock (writeQueue)
                {
                    writeQueue.Dequeue();
                    ba = writeQueue.Count > 0 ? writeQueue.First() : null;
                }
            }
            //继续发送
            if (ba != null)
            {
                socket.BeginSend(ba.bytes, ba.readIdx, ba.length, 0, SendCallback, socket);
            }
            //正在关闭
            else if (isClosing)
            {
                socket.Close();
            }
        }

        private void Update()
        {
            if (!IsConnected) return;
            if (msgCount == 0) return;
            //重复处理消息
            for (int i = 0; i < MAX_MESSAGE_FIRE; i++)
            {
                //获取第一条消息
                IExtensible msg = null;
                lock (msgList)
                {
                    if (msgList.Count > 0)
                    {
                        msg = msgList[0];
                        msgList.RemoveAt(0);
                        msgCount--;
                    }
                }
                //解析消息
                if (msg != null)
                {
                    DealMessage(msg);
                }
                else
                {
                    break;
                }
            }
        }
        
        private void OnDestroy()
        {
            Close();
        }
        #endregion

        #region >> Public Methods
        /// <summary>
        /// 连接服务端
        /// </summary>
        /// <param name="ip">服务器IP地址</param>
        /// <param name="port">端口</param>
        public void Connect(string ip, int port)
        {
            //状态判断
            if ((socket != null && socket.Connected) || isConnecting) return;
            //初始化
            Init();
            //参数设置
            socket.NoDelay = true;
            //连接
            isConnecting = true;
            socket.BeginConnect(ip, port, ConnectCallback, socket);
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="proto">协议</param>
        public void Send(IExtensible proto)
        {
            //状态判断
            if (socket == null || !socket.Connected) return;
            if (isConnecting || isClosing) return;
            //数据编码
            byte[] nameBytes = ProtoUtility.EncodeName(proto);
            byte[] bodyBytes = ProtoUtility.Encode(proto);
            int len = nameBytes.Length + bodyBytes.Length;
            byte[] sendBytes = new byte[2 + len];
            //组装长度
            sendBytes[0] = (byte)(len % 256);
            sendBytes[1] = (byte)(len / 256);
            //组装名字
            Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
            //组装消息体
            Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);
            //写入队列
            ByteArray ba = new ByteArray(sendBytes);
            int count = 0;
            lock (writeQueue)
            {
                writeQueue.Enqueue(ba);
                count = writeQueue.Count;
            }
            //Send
            if (count == 1)
            {
                socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, socket);
            }
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            //状态判断
            if (socket == null || !socket.Connected) return;
            if (isConnecting) return;
            //还有数据在发送
            if (writeQueue.Count > 0)
            {
                isClosing = true;
            }
            //没有数据在发送
            else
            {
                Debug.Log("关闭与服务端的连接");
                socket.Close();
            }
        }
        #endregion

        #region >> 消息处理
        //处理消息
        private void DealMessage(IExtensible proto)
        {
            string protoName = proto.GetType().Name;
            switch (protoName)
            {
                case "AvatarProperty": Main.Events.Publish(ProtoEventID.AvatarProperty, proto); break;
                default: Debug.Log(string.Format("未知协议类型：{0}", protoName)); break;
            }
        }
        #endregion
    }
}