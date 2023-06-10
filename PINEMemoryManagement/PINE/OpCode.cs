namespace Pine.Internals
{
    internal enum OpCode : byte
    {
        MsgRead8 = 0,
        MsgRead16 = 1,
        MsgRead32 = 2,
        MsgRead64 = 3,
        MsgWrite8 = 4,
        MsgWrite16 = 5,
        MsgWrite32 = 6,
        MsgWrite64 = 7,
        MsgVersion = 8,
        MsgSaveState = 9,
        MsgLoadState = 10,
        MsgTitle = 11,
        MsgID = 12,
        MsgUUID = 13,
        MsgGameVersion = 14,
        MsgStatus = 15,
        MsgUnimplemented = 0xFF
    }
}
