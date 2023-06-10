namespace MemoryIO.Pine.Internals
{
    internal enum ResultCode : byte
    {
        OK = 0,
        Fail = 1,
        OutOfMemory = 2,
        NoConnection = 3,
        Unimplemented = 4,
        Unknown = 5,
        Failure = 0xFF,
    }
}
