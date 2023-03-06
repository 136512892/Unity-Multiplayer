/****************************************************
*	文件：ProtoEventID.cs
*	作者：张寿昆(CoderZ)
*	邮箱：136512892@qq.com
*	日期：2022/12/12 14:05:58
*	功能：各协议类型事件ID
*****************************************************/

namespace Multiplayer
{
    /// <summary>
    /// 各协议类型事件ID
    /// </summary>
    public static class ProtoEventID
    {
        public static readonly int AvatarProperty = typeof(proto.AvatarProperty.AvatarProperty).GetHashCode();
    }
}