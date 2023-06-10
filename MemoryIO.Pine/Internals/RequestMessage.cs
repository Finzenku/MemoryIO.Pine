namespace MemoryIO.Pine.Internals
{
    internal struct RequestMessage
    {
        public OpCode OpCode { get; set; }
        public byte[] Argument { get; set; }
    }
}
