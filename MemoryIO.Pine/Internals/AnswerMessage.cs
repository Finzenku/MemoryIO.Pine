namespace MemoryIO.Pine.Internals
{
    internal struct AnswerMessage
    {
        public ResultCode ResultCode { get; set; }
        public byte[] Argument { get; set; }
    }
}
