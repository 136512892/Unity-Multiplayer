using ProtoBuf;
using System.Text;

namespace SK.Framework.Sockets
{
    /// <summary>
    /// 协议工具
    /// </summary>
    public static class ProtoUtility
    {
        /// <summary>
        /// 协议编码
        /// </summary>
        /// <param name="proto">协议</param>
        /// <returns>返回编码后的字节数据</returns>
        public static byte[] Encode(IExtensible proto)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, proto);
                return ms.ToArray();
            }
        }
        /// <summary>
        /// 协议解码
        /// </summary>
        /// <param name="protoName">协议名</param>
        /// <param name="bytes">要解码的byte数组</param>
        /// <param name="offset">协议体所在起始位置</param>
        /// <param name="count">协议体长度</param>
        /// <returns>返回解码后的协议</returns>
        public static IExtensible Decode(string protoName, byte[] bytes, int offset, int count)
        {
            using (MemoryStream ms = new MemoryStream(bytes, offset, count))
            {
                Type type = Type.GetType(protoName);
                return (IExtensible)Serializer.NonGeneric.Deserialize(type, ms);
            }
        }
        /// <summary>
        /// 协议名编码
        /// </summary>
        /// <param name="proto">协议</param>
        /// <returns>返回编码后的字节数据</returns>
        public static byte[] EncodeName(IExtensible proto)
        {
            //名字bytes和长度
            byte[] nameBytes = Encoding.UTF8.GetBytes(proto.GetType().FullName);
            Int16 length = (Int16)nameBytes.Length;
            //申请bytes数值
            byte[] bytes = new byte[length + 2];
            //组装2字节的长度信息
            bytes[0] = (byte)(length % 256);
            bytes[1] = (byte)(length / 256);
            //组装名字bytes
            Array.Copy(nameBytes, 0, bytes, 2, length);
            return bytes;
        }
        /// <summary>
        /// 协议名解码
        /// </summary>
        /// <param name="bytes">要解码的byte数组</param>
        /// <param name="offset">起始位置</param>
        /// <param name="length">长度</param>
        /// <returns>返回解码后的协议名</returns>
        public static string DecodeName(byte[] bytes, int offset, out int length)
        {
            length = 0;
            //必须大于2字节
            if (offset + 2 > bytes.Length) return string.Empty;
            //获取长度
            Int16 l = (Int16)((bytes[offset + 1] << 8) | bytes[offset]);
            if (l <= 0) return string.Empty;
            //长度必须足够
            if (offset + 2 + l > bytes.Length) return string.Empty;
            //解析
            length = 2 + l;
            string name = Encoding.UTF8.GetString(bytes, offset + 2, l);
            return name;
        }
    }
}