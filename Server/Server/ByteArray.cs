namespace SK.Framework.Sockets
{
    public class ByteArray
    {
        //默认大小
        private const int DEFAULT_SIZE = 1024;
        //初始大小
        private readonly int initSize = 0;
        //缓冲区
        public byte[] bytes;
        //读取位置
        public int readIdx = 0;
        //写入位置
        public int writeIdx = 0;
        //容量
        private int capacity = 0;
        //剩余空间
        public int remain { get { return capacity - writeIdx; } }
        //数据长度
        public int length { get { return writeIdx - readIdx; } }

        //构造函数
        public ByteArray(int size = DEFAULT_SIZE)
        {
            bytes = new byte[size];
            capacity = size;
            initSize = size;
            writeIdx = 0;
            readIdx = 0;
        }
        //构造函数
        public ByteArray(byte[] defaultBytes)
        {
            bytes = defaultBytes;
            capacity = defaultBytes.Length;
            initSize = defaultBytes.Length;
            readIdx = 0;
            writeIdx = defaultBytes.Length;
        }
        //重设尺寸
        public void ReSize(int size)
        {
            if (size < length) return;
            if (size < initSize) return;
            int n = 1;
            while (n < size)
            {
                n *= 2;
            }
            capacity = n;
            byte[] newBytes = new byte[capacity];
            Array.Copy(bytes, readIdx, newBytes, 0, writeIdx - readIdx);
            bytes = newBytes;
            writeIdx = length;
            readIdx = 0;
        }
        //检查并移动数据
        public void CheckAndMoveBytes()
        {
            if (length < 8)
            {
                MoveBytes();
            }
        }
        //移动数据
        public void MoveBytes()
        {
            if (length > 0)
            {
                Array.Copy(bytes, readIdx, bytes, 0, length);
            }
            writeIdx = length;
            readIdx = 0;
        }
        //写入数据
        public int Write(byte[] bs, int offset, int count)
        {
            if (remain < count)
            {
                ReSize(length + count);
            }
            Array.Copy(bs, offset, bytes, writeIdx, count);
            writeIdx += count;
            return count;
        }
        //读取数据
        public int Read(byte[] bs, int offset, int count)
        {
            count = Math.Min(count, length);
            Array.Copy(bytes, readIdx, bs, offset, count);
            readIdx += count;
            CheckAndMoveBytes();
            return count;
        }
        //读取Int16
        public Int16 ReadInt16()
        {
            if (length < 2) return 0;
            Int16 ret = (Int16)((bytes[readIdx + 1]) << 8 | bytes[readIdx]);
            readIdx += 2;
            CheckAndMoveBytes();
            return ret;
        }
        //读取Int32
        public Int32 ReadInt32()
        {
            if (length < 4) return 0;
            Int32 ret = (Int32)((bytes[readIdx + 3] << 24) |
                                (bytes[readIdx + 2] << 16) |
                                (bytes[readIdx + 1] << 8) |
                                bytes[readIdx + 0]);
            readIdx += 4;
            CheckAndMoveBytes();
            return ret;
        }
    }
}