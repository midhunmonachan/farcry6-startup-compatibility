using System;
using System.Collections.Generic;

internal static class RepairCode
{
    internal static byte[] LoaderThunk(long originalTarget)
    {
        var code=new List<byte>{0x53,0x48,0x83,0xec,0x20,0x49,0xbb};
        code.AddRange(BitConverter.GetBytes(originalTarget));
        code.AddRange(new byte[]{0x41,0xff,0xd3,0x48,0x83,0xc4,0x20,0x5b,0xc3});
        return code.ToArray();
    }
    internal static byte[] CrcInitializer(long storage)
    {
        var code=new List<byte>{0x49,0xbb};
        code.AddRange(BitConverter.GetBytes(storage));
        code.AddRange(new byte[]{0x41,0xc7,0x03,0x1c,0xc5,0x95,0xec,0x41,0x83,0x4b,0x04,1,0x4c,0x89,0xd8,0xc3});
        return code.ToArray();
    }
    internal static byte[] Branch(byte opcode, long from, long to)
    {
        long distance = to - from - 5;
        if (distance < int.MinValue || distance > int.MaxValue) throw new ArgumentOutOfRangeException("to");
        var result = new List<byte> { opcode };
        result.AddRange(BitConverter.GetBytes((int)distance));
        return result.ToArray();
    }
}
