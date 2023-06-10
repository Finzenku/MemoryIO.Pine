using Pine.TCP;
using System.Net.Sockets;

namespace Pine.Internals
{
    internal class PineTcpQueuer : TcpQueuer<AnswerMessage>
    {
        public PineTcpQueuer(TcpClient tcpClient) : base(tcpClient)
        {

        }

        protected override AnswerMessage ReadResponse()
        {
            byte[] answerLengthBuffer = new byte[4];
            int bytesRead = networkStream.Read(answerLengthBuffer, 0, 4);
            if (bytesRead < 4)
            {
                throw new IOException("Failed to read answer length from the network stream.");
            }
            int answerLength = BitConverter.ToInt32(answerLengthBuffer) - 4;
            if (answerLength < 1)
            {
                throw new IOException("Failed to read answer. Answer length was not a positive value.");
            }

            byte[] answer = new byte[answerLength];

            bytesRead = networkStream.Read(answer, 0, answerLength);
            if (bytesRead < answerLength)
            {
                throw new IOException("Failed to read complete answer from the network stream.");
            }
            return ParseMessage(answer);
        }

        private AnswerMessage ParseMessage(byte[] answerData)
        {
            // Check if the answer data is smaller than the minimum expected length
            if (answerData.Length < 1)
            {
                throw new IOException("Invalid answer data length.");
            }

            ResultCode resultCode = (ResultCode)answerData[0];

            byte[] argument = new byte[0];
            int argumentLength = answerData.Length - 1;

            // Check if there is any argument data
            if (argumentLength > 0)
            {
                int paddedLength = (argumentLength + 3) & ~3;
                argument = new byte[paddedLength];
                Buffer.BlockCopy(answerData, 1, argument, 0, argumentLength);

            }
            return new AnswerMessage { ResultCode=resultCode, Argument=argument };
        }
    }
}
