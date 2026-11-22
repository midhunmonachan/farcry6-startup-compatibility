using System;
using System.Text;

internal sealed class WindowsLoaderSite
{
    internal long Call;
    internal long Target;
    internal byte[] OriginalCall;
}

internal static class WindowsLoader
{
    // Arguments to LdrpCallInitRoutineInternal, followed by its return-value store.
    // The call displacement can move between Windows builds.
    static readonly byte[] Before = {0x4c,0x8b,0xce,0x45,0x8b,0xc6,0x49,0x8b,0xd7,0x48,0x8b,0xcf};
    static readonly byte[] After = {0x88,0x44,0x24,0x30,0x65,0x48,0x8b,0x04,0x25,0x60,0,0,0};

    internal static WindowsLoaderSite Locate(long moduleBase,Func<long,int,byte[]> read)
    {
        byte[] header=read(moduleBase,4096);
        if(header==null || header.Length<4096 || header[0]!='M' || header[1]!='Z')throw Unsupported();
        int pe=BitConverter.ToInt32(header,0x3c);
        if(pe<0x40 || pe>0x800 || BitConverter.ToUInt32(header,pe)!=0x4550)throw Unsupported();
        int sections=BitConverter.ToUInt16(header,pe+6);
        int optional=pe+24, sectionTable=optional+BitConverter.ToUInt16(header,pe+20);
        if(BitConverter.ToUInt16(header,optional)!=0x20b || sections<1 || sections>64 || sectionTable+sections*40>header.Length)throw Unsupported();
        for(int i=0;i<sections;i++) {
            int entry=sectionTable+i*40;
            if(Encoding.ASCII.GetString(header,entry,5)!=".text")continue;
            uint size=BitConverter.ToUInt32(header,entry+8), rva=BitConverter.ToUInt32(header,entry+12);
            if(size<64 || size>4*1024*1024 || rva==0)throw Unsupported();
            byte[] code=read(moduleBase+rva,(int)size);
            if(code==null || code.Length!=(int)size)throw Unsupported();
            return Find(code,moduleBase,rva);
        }
        throw Unsupported();
    }

    internal static WindowsLoaderSite Find(byte[] code,long moduleBase,long textRva)
    {
        WindowsLoaderSite found=null;
        for(int i=0;i<=code.Length-Before.Length-5-After.Length;i++) {
            if(!Matches(code,i,Before) || code[i+Before.Length]!=0xe8 || !Matches(code,i+Before.Length+5,After))continue;
            int callOffset=i+Before.Length;
            long targetRva=textRva+callOffset+5+BitConverter.ToInt32(code,callOffset+1);
            if(targetRva<textRva || targetRva>=textRva+code.Length)continue;
            if(found!=null)throw Unsupported();
            found=new WindowsLoaderSite {
                Call=moduleBase+textRva+callOffset,
                Target=moduleBase+targetRva,
                OriginalCall=new byte[]{code[callOffset],code[callOffset+1],code[callOffset+2],code[callOffset+3],code[callOffset+4]}
            };
        }
        if(found==null)throw Unsupported();
        return found;
    }

    static bool Matches(byte[] code,int start,byte[] pattern)
    {
        for(int i=0;i<pattern.Length;i++)if(code[start+i]!=pattern[i])return false;
        return true;
    }

    static InvalidOperationException Unsupported()
    {
        return new InvalidOperationException("This Windows loader does not have one verified repair site. Startup has been stopped without changing the game.");
    }
}
