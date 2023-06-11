namespace MemoryIO.Pine.Internals
{
    public enum EmulatorStatus : int
    {
        Running = 0,
        Paused = 1,
        Shutdown = 2,
        Unknown = 0xFF,
    }
}
