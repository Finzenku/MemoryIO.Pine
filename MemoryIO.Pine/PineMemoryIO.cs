using MemoryIO.Pine.Internals;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        #region Methods

        #region MemoryIO

        #region Read

        #region ReadData
        // Currently PINE only works with 32bit memory addresses, which is fine for most (all?) emulators
        //   so we'll just ensure that any IntPtrs we get are truncated to an Int32 before sending them to the RequestBuilder
        public byte[] ReadData(IntPtr address, int dataLength) => ReadData((int)address, dataLength);
        public byte[] ReadData(int address, int dataLength) => tcpQueuer.SendRequestAsync(RequestBuilder.BuildReadMsg(address, dataLength)).GetAwaiter().GetResult().Argument;
        public async Task<byte[]> ReadDataAsync(IntPtr address, int dataLength) => await ReadDataAsync((int)address, dataLength);
        public async Task<byte[]> ReadDataAsync(int address, int dataLength) => (await tcpQueuer.SendRequestAsync(RequestBuilder.BuildReadMsg(address, dataLength))).Argument;
        #endregion

        #region Read<T>
        public T Read<T>(IntPtr address) where T : unmanaged => MemoryMarshal.Cast<byte, T>(ReadData(address, Marshal.SizeOf<T>()))[0];
        public T Read<T>(int address) where T : unmanaged => MemoryMarshal.Cast<byte, T>(ReadData(address, Marshal.SizeOf<T>()))[0];
        public async Task<T> ReadAsync<T>(IntPtr address) where T : unmanaged => MemoryMarshal.Cast<byte, T>(await ReadDataAsync(address, Marshal.SizeOf<T>()))[0];
        public async Task<T> ReadAsync<T>(int address) where T : unmanaged => MemoryMarshal.Cast<byte, T>(await ReadDataAsync(address, Marshal.SizeOf<T>()))[0];
        #endregion

        #region ReadArray<T>
        public T[] ReadArray<T>(IntPtr address, int arrayLength) where T : unmanaged => MemoryMarshal.Cast<byte, T>(ReadData(address, Marshal.SizeOf<T>() * arrayLength)).ToArray();
        public T[] ReadArray<T>(int address, int arrayLength) where T : unmanaged => MemoryMarshal.Cast<byte, T>(ReadData(address, Marshal.SizeOf<T>() * arrayLength)).ToArray();
        public async Task<T[]> ReadArrayAsync<T>(IntPtr address, int arrayLength) where T : unmanaged => MemoryMarshal.Cast<byte, T>(await ReadDataAsync(address, Marshal.SizeOf<T>() * arrayLength)).ToArray();
        public async Task<T[]> ReadArrayAsync<T>(int address, int arrayLength) where T : unmanaged => MemoryMarshal.Cast<byte, T>(await ReadDataAsync(address, Marshal.SizeOf<T>() * arrayLength)).ToArray();
        #endregion

        #region ReadString
        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            byte[] buffer = ReadData(address, maxLength);
            ReadOnlySpan<byte> bytes = buffer.AsSpan();

            int nullCharPos = bytes.IndexOf((byte)'\0');
            if (nullCharPos != -1)
                bytes = bytes.Slice(0, nullCharPos);

            return encoding.GetString(bytes);
        }
        public string ReadString(int address, Encoding encoding, int maxLength = 512)
        {
            byte[] buffer = ReadData(address, maxLength);
            ReadOnlySpan<byte> bytes = buffer.AsSpan();

            int nullCharPos = bytes.IndexOf((byte)'\0');
            if (nullCharPos != -1)
                bytes = bytes.Slice(0, nullCharPos);

            return encoding.GetString(bytes);
        }
        public async Task<string> ReadStringAsync(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            byte[] buffer = await ReadDataAsync(address, maxLength);
            ReadOnlyMemory<byte> memory = buffer;

            int nullCharPos = memory.Span.IndexOf((byte)'\0');
            if (nullCharPos != -1)
                memory = memory.Slice(0, nullCharPos);

            return encoding.GetString(memory.Span);
        }
        public async Task<string> ReadStringAsync(int address, Encoding encoding, int maxLength = 512)
        {
            byte[] buffer = await ReadDataAsync(address, maxLength);
            ReadOnlyMemory<byte> memory = buffer;

            int nullCharPos = memory.Span.IndexOf((byte)'\0');
            if (nullCharPos != -1)
                memory = memory.Slice(0, nullCharPos);

            return encoding.GetString(memory.Span);
        }
        #endregion

        #region ReadStringArray
        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            string encoded = encoding.GetString(ReadData(address, maxLength));
            string[] split = encoded.Split('\0');
            int emptyIndex = Array.IndexOf(split, string.Empty);
            return emptyIndex >= 0 ? split[..emptyIndex] : split;
        }
        public string[] ReadStringArray(int address, Encoding encoding, int maxLength = 512)
        {
            string encoded = encoding.GetString(ReadData(address, maxLength));
            string[] split = encoded.Split('\0');
            int emptyIndex = Array.IndexOf(split, string.Empty);
            return emptyIndex >= 0 ? split[..emptyIndex] : split;
        }
        public async Task<string[]> ReadStringArrayAsync(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            string encoded = encoding.GetString(await ReadDataAsync(address, maxLength));
            string[] split = encoded.Split('\0');
            int emptyIndex = Array.IndexOf(split, string.Empty);
            return emptyIndex >= 0 ? split[..emptyIndex] : split;
        }
        public async Task<string[]> ReadStringArrayAsync(int address, Encoding encoding, int maxLength = 512)
        {
            string encoded = encoding.GetString(await ReadDataAsync(address, maxLength));
            string[] split = encoded.Split('\0');
            int emptyIndex = Array.IndexOf(split, string.Empty);
            return emptyIndex >= 0 ? split[..emptyIndex] : split;
        }
        #endregion

        #endregion

        #region Write

        #region WriteData
        // tcpQueuer.SendRequestAsync returns an AnswerMessage if in the future we want to add error handling
        public void WriteData(IntPtr address, byte[] data) => WriteData((int)address, data);
        public void WriteData(int address, byte[] data) => tcpQueuer.SendRequestAsync(RequestBuilder.BuildWriteMsg(address, data)).GetAwaiter().GetResult();
        public async Task WriteDataAsync(IntPtr address, byte[] data) => await WriteDataAsync((int)address, data);
        public async Task WriteDataAsync(int address, byte[] data) => await tcpQueuer.SendRequestAsync(RequestBuilder.BuildWriteMsg(address, data));
        #endregion

        #region Write<T>
        public void Write<T>(IntPtr address, T value) where T : unmanaged => WriteData(address, MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref value, 1)).ToArray());
        public void Write<T>(int address, T value) where T : unmanaged => WriteData(address, MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref value, 1)).ToArray());
        public async Task WriteAsync<T>(IntPtr address, T value) where T : unmanaged => await WriteDataAsync(address, MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref value, 1)).ToArray());
        public async Task WriteAsync<T>(int address, T value) where T : unmanaged => await WriteDataAsync(address, MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref value, 1)).ToArray());
        #endregion

        #region WriteArray<T>
        public void WriteArray<T>(IntPtr address, T[] value) where T : unmanaged => WriteData(address, MemoryMarshal.Cast<T, byte>(value.AsSpan()).ToArray());
        public void WriteArray<T>(int address, T[] value) where T : unmanaged => WriteData(address, MemoryMarshal.Cast<T, byte>(value.AsSpan()).ToArray());
        public async Task WriteArrayAsync<T>(IntPtr address, T[] value) where T : unmanaged => await WriteDataAsync(address, MemoryMarshal.Cast<T, byte>(value.AsSpan()).ToArray());
        public async Task WriteArrayAsync<T>(int address, T[] value) where T : unmanaged => await WriteDataAsync(address, MemoryMarshal.Cast<T, byte>(value.AsSpan()).ToArray());
        #endregion

        #region WriteString
        public void WriteString(IntPtr address, string text, Encoding encoding) => WriteData(address, encoding.GetBytes(text + '\0'));
        public void WriteString(int address, string text, Encoding encoding) => WriteData(address, encoding.GetBytes(text + '\0'));
        public async Task WriteStringAsync(IntPtr address, string text, Encoding encoding) => await WriteDataAsync(address, encoding.GetBytes(text + '\0'));
        public async Task WriteStringAsync(int address, string text, Encoding encoding) => await WriteDataAsync(address, encoding.GetBytes(text + '\0'));
        #endregion

        #region WriteStringArray
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding)
        {
            int totalLength = 0;
            foreach (string s in text)
                totalLength += encoding.GetByteCount(s) + 1;

            byte[] buffer = new byte[totalLength];
            int bufferOffset = 0;

            foreach (string s in text)
            {
                int byteCount = encoding.GetBytes(s, buffer.AsSpan(bufferOffset));
                bufferOffset += byteCount + 1;
            }

            WriteData(address, buffer);
        }
        public void WriteStringArray(int address, string[] text, Encoding encoding)
        {
            int totalLength = 0;
            foreach (string s in text)
                totalLength += encoding.GetByteCount(s) + 1;

            byte[] buffer = new byte[totalLength];
            int bufferOffset = 0;

            foreach (string s in text)
            {
                int byteCount = encoding.GetBytes(s, buffer.AsSpan(bufferOffset));
                bufferOffset += byteCount + 1;
            }

            WriteData(address, buffer);
        }
        public async Task WriteStringArrayAsync(IntPtr address, string[] text, Encoding encoding)
        {
            int totalLength = 0;
            foreach (string s in text)
                totalLength += encoding.GetByteCount(s) + 1;

            byte[] buffer = new byte[totalLength];
            int bufferOffset = 0;

            foreach (string s in text)
            {
                int byteCount = encoding.GetBytes(s, buffer.AsSpan(bufferOffset));
                bufferOffset += byteCount + 1;
            }

            await WriteDataAsync(address, buffer);
        }
        public async Task WriteStringArrayAsync(int address, string[] text, Encoding encoding)
        {
            int totalLength = 0;
            foreach (string s in text)
                totalLength += encoding.GetByteCount(s) + 1;

            byte[] buffer = new byte[totalLength];
            int bufferOffset = 0;

            foreach (string s in text)
            {
                int byteCount = encoding.GetBytes(s, buffer.AsSpan(bufferOffset));
                bufferOffset += byteCount + 1;
            }

            await WriteDataAsync(address, buffer);
        }
        #endregion

        #endregion

        #endregion

        #region PINE

        private AnswerMessage GetAnswer(RequestMessage request) => tcpQueuer.SendRequestAsync(RequestBuilder.BuildRequest(request)).GetAwaiter().GetResult();
        private async Task<AnswerMessage> GetAnswerAsync(RequestMessage request) => await tcpQueuer.SendRequestAsync(RequestBuilder.BuildRequest(request));

        // Get an AnswerMessage that returns nothing, return true if ResultCode.OK
        private bool GetNoArgAnswer(RequestMessage request)
        {
            AnswerMessage answer = GetAnswer(request);
            if (answer.ResultCode != ResultCode.OK)
                return false;
            return true;
        }
        private async Task<bool> GetNoArgAnswerAsync(RequestMessage request)
        {
            AnswerMessage answer = await GetAnswerAsync(request);
            if (answer.ResultCode != ResultCode.OK)
                return false;
            return true;
        }

        // Get an AnswerMessage that returns a char[] and convert it to a UTF8 string if ResultCode.OK
        private string GetUTF8Answer(RequestMessage request)
        {
            AnswerMessage answer = GetAnswer(request);
            if (answer.ResultCode != ResultCode.OK)
                return string.Empty;
            // Argument: [int size (4 bytes)] [char[] string]
            return Encoding.UTF8.GetString(answer.Argument.AsSpan().Slice(3));
        }
        private async Task<string> GetUTF8AnswerAsync(RequestMessage request)
        {
            AnswerMessage answer = await GetAnswerAsync(request);
            if (answer.ResultCode != ResultCode.OK)
                return string.Empty;
            // Argument: [int size (4 bytes)] [char[] string]
            return Encoding.UTF8.GetString(answer.Argument.AsSpan().Slice(3));
        }

        public string GetVersion() => GetUTF8Answer(new RequestMessage() { OpCode = OpCode.MsgVersion });
        public async Task<string> GetVersionAsync() => await GetUTF8AnswerAsync(new RequestMessage() { OpCode = OpCode.MsgVersion });
        public bool SaveState(byte saveSlot) => GetNoArgAnswer(new RequestMessage() { OpCode = OpCode.MsgSaveState, Argument = new byte[] { saveSlot } });
        public async Task<bool> SaveStateAsync(byte saveSlot) => await GetNoArgAnswerAsync(new RequestMessage() { OpCode = OpCode.MsgSaveState, Argument = new byte[] { saveSlot } });
        public bool LoadState(byte saveSlot) => GetNoArgAnswer(new RequestMessage() { OpCode = OpCode.MsgLoadState, Argument = new byte[] { saveSlot } });
        public async Task<bool> LoadStateAsync(byte saveSlot) => await GetNoArgAnswerAsync(new RequestMessage() { OpCode = OpCode.MsgLoadState, Argument = new byte[] { saveSlot } });
        public string GetGameTitle() => GetUTF8Answer(new RequestMessage() { OpCode = OpCode.MsgTitle });
        public async Task<string> GetGameTitleAsync() => await GetUTF8AnswerAsync(new RequestMessage() { OpCode = OpCode.MsgTitle });
        public string GetGameID() => GetUTF8Answer(new RequestMessage() { OpCode = OpCode.MsgTitle });
        public async Task<string> GetGameIDAsync() => await GetUTF8AnswerAsync(new RequestMessage() { OpCode = OpCode.MsgTitle });
        public string GetGameUUID() => GetUTF8Answer(new RequestMessage() { OpCode = OpCode.MsgUUID });
        public async Task<string> GetGameUUIDAsync() => await GetUTF8AnswerAsync(new RequestMessage() { OpCode = OpCode.MsgUUID });
        public string GetGameVersion() => GetUTF8Answer(new RequestMessage() { OpCode = OpCode.MsgGameVersion });
        public async Task<string> GetGameVersionAsync() => await GetUTF8AnswerAsync(new RequestMessage() { OpCode = OpCode.MsgGameVersion });
        public EmulatorStatus GetStatus()
        {
            AnswerMessage answer = GetAnswer(new RequestMessage() { OpCode = OpCode.MsgStatus });
            if (answer.ResultCode != ResultCode.OK)
                return EmulatorStatus.Unknown;
            return (EmulatorStatus)BitConverter.ToInt32(answer.Argument);
        }
        public async Task<EmulatorStatus> GetStatusAsync()
        {
            AnswerMessage answer = await GetAnswerAsync(new RequestMessage() { OpCode = OpCode.MsgStatus });
            if (answer.ResultCode != ResultCode.OK)
                return EmulatorStatus.Unknown;
            return (EmulatorStatus)BitConverter.ToInt32(answer.Argument);
        }

        #endregion

        #endregion
    }
}