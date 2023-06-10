using MemoryIO.Internals;
using MemoryIO.Pine.Internals;
using System.Net.Sockets;
using System.Text;

namespace MemoryIO.Pine
{
    public class PineMemoryIO : IMemoryIO
    {
        private const string DefaultConnectionString = "localhost";
        private const int DefaultConnectionPort = 28011;
        private TcpClient client;
        private PineTcpQueuer tcpQueuer;

        public PineMemoryIO(TcpClient tcpClient)
        {
            client = tcpClient;
            if (!client.Connected)
            {
                client.Connect(DefaultConnectionString, DefaultConnectionPort);
            }
            tcpQueuer = new PineTcpQueuer(client);
        }
        public PineMemoryIO(string host, int port)
        {
            // TcpClient will automatically attempt to connect and throw its own exceptions if the connection could not be estabilished
            client = new TcpClient(host, port);
            if (!client.Connected)
            {
                throw new ArgumentException("Unable to establish connection to the server.");
            }
            tcpQueuer = new PineTcpQueuer(client);
        }
        public PineMemoryIO() : this(DefaultConnectionString, DefaultConnectionPort)
        {

        }

        public T Read<T>(IntPtr address) where T : unmanaged
        {
            int dataSize = MarshalType<T>.Size;
            byte[] buffer = ReadData(address, dataSize);
            return MarshalType<T>.ByteArrayToObject(buffer);
        }
        public async Task<T> ReadAsync<T>(IntPtr address)
        {
            int dataSize = MarshalType<T>.Size;
            byte[] buffer = await ReadDataAsync(address, dataSize);
            return MarshalType<T>.ByteArrayToObject(buffer);
        }

        public T[] ReadArray<T>(IntPtr address, int arrayLength) where T : unmanaged
        {
            int dataSize = MarshalType<T>.Size;
            byte[] buffer = ReadData(address, dataSize * arrayLength);
            if (MarshalType<T>.TypeCode == TypeCode.Byte)
                return (T[])(object)buffer;
            T[] tArray = new T[arrayLength];
            for (int i = 0; i < arrayLength; i++)
                tArray[i] = MarshalType<T>.ByteArrayToObject(buffer[(dataSize * i)..(dataSize * (i + 1))]);
            return tArray;
        }

        public byte[] ReadData(IntPtr address, int dataLength)
        {
            int address32bit = (int)address;
            AnswerMessage answer = tcpQueuer.SendRequestAsync(RequestBuilder.BuildBatchReadMsg(address32bit, dataLength)).GetAwaiter().GetResult();
            return answer.Argument;
        }

        public async Task<byte[]> ReadDataAsync(IntPtr address, int dataLength)
        {
            int address32bit = (int)address;
            AnswerMessage answer = await tcpQueuer.SendRequestAsync(RequestBuilder.BuildBatchReadMsg(address32bit, dataLength));
            return answer.Argument;
        }

        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            string encoded = encoding.GetString(ReadData(address, maxLength));
            int nullCharPos = encoded.IndexOf('\0');
            return nullCharPos == -1 ? encoded : encoded.Substring(0, nullCharPos);
        }
        public async Task<string> ReadStringAsync(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            string encoded = encoding.GetString(await ReadDataAsync(address, maxLength));
            int nullCharPos = encoded.IndexOf('\0');
            return nullCharPos == -1 ? encoded : encoded.Substring(0, nullCharPos);
        }

        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            string encoded = encoding.GetString(ReadData(address, maxLength));
            string[] split = encoded.Split('\0');
            for (int i = 0; i < split.Length; i++)
                if (split[i] == string.Empty)
                    return split[0..i];
            return split;
        }

        public void Write<T>(IntPtr address, T value) where T : unmanaged
        {
            WriteData(address, MarshalType<T>.ObjectToByteArray(value));
        }

        public void WriteArray<T>(IntPtr address, T[] value) where T : unmanaged
        {
            if (value is byte[] byteArray)
            {
                WriteData(address, byteArray);
                return;
            }
            int typeSize = MarshalType<T>.Size;
            byte[] buffer = new byte[value.Length * typeSize];
            for (int i = 0; i < value.Length; i++)
            {
                Buffer.BlockCopy(MarshalType<T>.ObjectToByteArray(value[i]), 0, buffer, i * typeSize, typeSize);
            }
            WriteData(address, buffer);
        }

        public void WriteData(IntPtr address, byte[] data)
        {
            // If in the future we can all the write methods to return bool, we can check AnswerMessage.ResultCode
            /* AnswerMessage answer =*/ tcpQueuer.SendRequestAsync(RequestBuilder.BuildBatchWriteMsg((int)address, data)).GetAwaiter().GetResult();
        }

        public async Task WriteDataAsync(IntPtr address, byte[] data)
        {
           /* AnswerMessage answer =*/ await tcpQueuer.SendRequestAsync(RequestBuilder.BuildBatchWriteMsg((int)address, data));
        }

        public void WriteString(IntPtr address, string text, Encoding encoding)
        {
            WriteData(address, encoding.GetBytes(text + '\0'));
        }

        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding)
        {
            int totalLength = 0; ;
            foreach (string s in text)
                totalLength += encoding.GetByteCount(s) + 1;
            byte[] buffer = new byte[totalLength];
            int bufferOffset = 0;
            foreach (string s in text)
            {
                int byteCount = encoding.GetBytes(s, buffer.AsSpan(bufferOffset));
                buffer[bufferOffset + byteCount] = 0;
                bufferOffset += byteCount + 1;
            }
            WriteData(address, buffer);
        }
    }
}