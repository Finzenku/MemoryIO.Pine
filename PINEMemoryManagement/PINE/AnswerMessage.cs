using System.Runtime.InteropServices;

namespace Pine.Internals
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AnswerMessage
    {
        public ResultCode ResultCode { get; set; }
        public byte[] Argument { get; set; }
    }
}
