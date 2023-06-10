using System.Runtime.InteropServices;

namespace Pine.Internals
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RequestMessage
    {
        public OpCode OpCode { get; set; }
        public byte[] Argument { get; set; }
    }
}
