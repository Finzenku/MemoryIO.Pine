namespace MemoryIO.Pine.Internals
{
    internal static class RequestBuilder
    {
        private const int RequestSizeSize = 4;
        private const int OpCodeSize = 1;
        private const int ReadMsgSize = 5;
        private const int SixtyFourBitSize = 8;
        private const int ThirtyTwoBitSize = 4;
        private const int SixteenBitSize = 2;
        private const int EightBitSize = 1;

        public static byte[] BuildMessage(RequestMessage request)
        {
            // [RequestSize (4 bytes)] [OpCode (1 byte)] [Argument (variable length)] 
            int requestSize = RequestSizeSize + OpCodeSize + request.Argument.Length;
            byte[] messageData = new byte[requestSize];
            BitConverter.GetBytes(requestSize).CopyTo(messageData, 0);
            messageData[RequestSizeSize] = (byte)request.OpCode;
            if(request.Argument.Length > 0)
                request.Argument.CopyTo(messageData, RequestSizeSize + OpCodeSize);
            return messageData;
        }

        private static byte[] BuildReadMsg(OpCode opCode, int address)
        {
            int requestSize = RequestSizeSize + ReadMsgSize;
            byte[] messageData = new byte[requestSize];
            BitConverter.GetBytes(requestSize).CopyTo(messageData, 0);
            messageData[RequestSizeSize] = (byte)opCode;
            BitConverter.GetBytes(address).CopyTo(messageData, RequestSizeSize + OpCodeSize);
            return messageData;
        }
        public static byte[] BuildRead8Msg(int address) => BuildReadMsg(OpCode.MsgRead8, address);
        public static byte[] BuildRead16Msg(int address) => BuildReadMsg(OpCode.MsgRead16, address);
        public static byte[] BuildRead32Msg(int address) => BuildReadMsg(OpCode.MsgRead32, address);
        public static byte[] BuildRead64Msg(int address) => BuildReadMsg(OpCode.MsgRead64, address);

        private static byte[] BuildWriteMsg(OpCode opCode, int address, byte[] dataToWrite)
        {
            // [RequestSize (4 bytes)] [OpCode (1 byte)] [Address (4 bytes)] + [WriteData (variable length)]
            int requestSize = RequestSizeSize + OpCodeSize + ThirtyTwoBitSize + dataToWrite.Length;
            byte[] messageData = new byte[requestSize];
            BitConverter.GetBytes(requestSize).CopyTo(messageData, 0);
            messageData[RequestSizeSize] = (byte)opCode;
            BitConverter.GetBytes(address).CopyTo(messageData, RequestSizeSize + OpCodeSize);
            dataToWrite.CopyTo(messageData, RequestSizeSize + OpCodeSize + ThirtyTwoBitSize);
            return messageData;
        }
        public static byte[] BuildWriteMsg(int address, byte data) => BuildWriteMsg(OpCode.MsgWrite8, address, BitConverter.GetBytes(data));
        public static byte[] BuildWriteMsg(int address, short data) => BuildWriteMsg(OpCode.MsgWrite16, address, BitConverter.GetBytes(data));
        public static byte[] BuildWriteMsg(int address, ushort data) => BuildWriteMsg(OpCode.MsgWrite16, address, BitConverter.GetBytes(data));
        public static byte[] BuildWriteMsg(int address, int data) => BuildWriteMsg(OpCode.MsgWrite32, address, BitConverter.GetBytes(data));
        public static byte[] BuildWriteMsg(int address, uint data) => BuildWriteMsg(OpCode.MsgWrite32, address, BitConverter.GetBytes(data));
        public static byte[] BuildWriteMsg(int address, long data) => BuildWriteMsg(OpCode.MsgWrite64, address, BitConverter.GetBytes(data));
        public static byte[] BuildWriteMsg(int address, ulong data) => BuildWriteMsg(OpCode.MsgWrite64, address, BitConverter.GetBytes(data));

        public static byte[] BuildReadMsg(int address, int readLengthInBytes)
        {
            switch (readLengthInBytes)
            {
                case SixtyFourBitSize:
                    return BuildRead64Msg(address);
                case ThirtyTwoBitSize:
                    return BuildRead32Msg(address);
                case SixteenBitSize:
                    return BuildRead16Msg(address);
                case EightBitSize:
                    return BuildRead8Msg(address);
            }

            // Since we know the exact amount of bytes we want to read, this should be more efficient than estimating and copying the buffer later
            int requestSize = RequestSizeSize + ReadMsgSize *
                (readLengthInBytes / SixtyFourBitSize +
                readLengthInBytes % SixtyFourBitSize / ThirtyTwoBitSize +
                readLengthInBytes % ThirtyTwoBitSize / SixteenBitSize +
                readLengthInBytes % SixteenBitSize);

            byte[] buffer = new byte[requestSize];
            BitConverter.GetBytes(requestSize).CopyTo(buffer, 0);

            int bufferOffset = RequestSizeSize;
            int dataLength = readLengthInBytes;
            OpCode opCode;
            int dataSize;

            while (dataLength > 0)
            {
                // See comment in BuildWriteMsg for the if-else chain that this branchless statement represents
                // Assumptions are that OpCode enum has MsgRead8, MsgRead16, MsgRead32, MsgRead64 in acending order (0, 1, 2, 3)
                int opCodeValue = dataLength - SixtyFourBitSize >> 31 & 1;
                opCodeValue |= dataLength - ThirtyTwoBitSize >> 31 & 2;
                opCodeValue |= dataLength - SixteenBitSize >> 31 & 4;

                opCode = (OpCode)((byte)OpCode.MsgRead64 - (opCodeValue & 1) - ((opCodeValue & 2) >> 1) - ((opCodeValue & 4) >> 2));
                dataSize = SixtyFourBitSize - ThirtyTwoBitSize * (opCodeValue & 1) - SixteenBitSize * ((opCodeValue & 2) >> 1) - EightBitSize * ((opCodeValue & 4) >> 2);

                buffer[bufferOffset] = (byte)opCode;
                BitConverter.GetBytes(address).CopyTo(buffer, bufferOffset + OpCodeSize);
                address += dataSize;
                bufferOffset += ReadMsgSize;
                dataLength -= dataSize;
            }

            return buffer;
        }

        public static byte[] BuildWriteMsg(int address, byte[] dataToWrite)
        {
            int dataLength = dataToWrite.Length;
            switch (dataLength)
            {
                case SixtyFourBitSize:
                    return BuildWriteMsg(OpCode.MsgWrite64, address, dataToWrite);
                case ThirtyTwoBitSize:
                    return BuildWriteMsg(OpCode.MsgWrite32, address, dataToWrite);
                case SixteenBitSize:
                    return BuildWriteMsg(OpCode.MsgWrite16, address, dataToWrite);
                case EightBitSize:
                    return BuildWriteMsg(OpCode.MsgWrite8, address, dataToWrite);
            }
            const int maxMsgSize = OpCodeSize + ThirtyTwoBitSize + SixtyFourBitSize;

            // Reserve enough memory to fit all of our MsgWrite64s plus enough for 3 more.
            // Worst case scenario is we have a MsgWrite32, MsgWrite16, and a MsgWrite8 after our MsgWrite64s which will fit easily
            int estimatedMemorySize = RequestSizeSize + (dataLength / SixtyFourBitSize + 3) * maxMsgSize;
            byte[] buffer = new byte[estimatedMemorySize];
            // Skip RequestSizeSize bytes to fit our RequestSize later
            int bufferOffset = RequestSizeSize;

            OpCode opCode;
            int dataSize;
            int dataOffset = 0;

            while (dataLength > 0)
            {
                // I used ChatGPT to help write some branchless programming for the commented out if-else statements below. It does the same thing without any branching
                // Assumptions are that OpCode enum has MsgWrite8, MsgWrite16, MsgWrite32, MsgWrite64 in acending order (4, 5, 6, 7)
                int opCodeValue = dataLength - SixtyFourBitSize >> 31 & 1;
                opCodeValue |= dataLength - ThirtyTwoBitSize >> 31 & 2;
                opCodeValue |= dataLength - SixteenBitSize >> 31 & 4;

                opCode = (OpCode)((byte)OpCode.MsgWrite64 - (opCodeValue & 1) - ((opCodeValue & 2) >> 1) - ((opCodeValue & 4) >> 2));
                dataSize = SixtyFourBitSize - ThirtyTwoBitSize * (opCodeValue & 1) - SixteenBitSize * ((opCodeValue & 2) >> 1) - EightBitSize * ((opCodeValue & 4) >> 2);

                /*
                if (dataLength >= SixtyFourBitSize)
                {
                    opCode = OpCode.MsgWrite64;
                    dataSize = SixtyFourBitSize;
                }
                else if (dataLength >= ThirtyTwoBitSize)
                {
                    opCode = OpCode.MsgWrite32;
                    dataSize = ThirtyTwoBitSize;
                }
                else if (dataLength >= SixteenBitSize)
                {
                    opCode = OpCode.MsgWrite16;
                    dataSize = SixteenBitSize;
                }
                else
                {
                    opCode = OpCode.MsgWrite8;
                    dataSize = EightBitSize;
                }
                */

                byte[] array = new byte[0];
                if (array.Length >= 8)
                {
                    opCode = (OpCode)7;
                    dataSize = 8;
                }
                else if (array.Length >= 4)
                {
                    opCode = (OpCode)6;
                    dataSize = 4;
                }
                else if (array.Length >= 2)
                {
                    opCode = (OpCode)5;
                    dataSize = 2;
                }
                else if (array.Length >= 1)
                {
                    opCode = (OpCode)4;
                    dataSize = 1;
                }

                buffer[bufferOffset] = (byte)opCode;
                BitConverter.GetBytes(address).CopyTo(buffer, bufferOffset + OpCodeSize);
                Buffer.BlockCopy(dataToWrite, dataOffset, buffer, bufferOffset + OpCodeSize + ThirtyTwoBitSize, dataSize);
                address += dataSize;
                dataOffset += dataSize;
                dataLength -= dataSize;
                bufferOffset += OpCodeSize + ThirtyTwoBitSize + dataSize;
            }

            // bufferOffset tells us how much data we've written to the buffer, giving us our RequestSize
            BitConverter.GetBytes(bufferOffset).CopyTo(buffer, 0);
            byte[] messageData = new byte[bufferOffset];
            Buffer.BlockCopy(buffer, 0, messageData, 0, bufferOffset);
            return messageData;
        }
    }
}
