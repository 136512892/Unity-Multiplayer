namespace SK.Framework
{
    /// <summary>
    /// 时间类工具
    /// </summary>
    public class TimeUtility
    {
        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns>时间戳</returns>
        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }
    }
}