using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal static class WindowsLoaderTests
{
    static readonly byte[] Before={0x4c,0x8b,0xce,0x45,0x8b,0xc6,0x49,0x8b,0xd7,0x48,0x8b,0xcf};
    static readonly byte[] After={0x88,0x44,0x24,0x30,0x65,0x48,0x8b,0x04,0x25,0x60,0,0,0};
    static int checks;

    static void Check(bool condition,string message)
    {
        checks++;
        if(!condition)throw new Exception(message);
    }

    static byte[] Image(int site,int target)
    {
        var image=new byte[512];
        Array.Copy(Before,0,image,site,Before.Length);
        int call=site+Before.Length;
        image[call]=0xe8;
        Array.Copy(BitConverter.GetBytes(target-call-5),0,image,call+1,4);
        Array.Copy(After,0,image,call+5,After.Length);
        return image;
    }

    static void Reject(byte[] image,string message)
    {
        bool rejected=false;
        try {WindowsLoader.Find(image,0x10000000,0x1000);}
        catch(InvalidOperationException) {rejected=true;}
        Check(rejected,message);
    }

    static int Main()
    {
        foreach(int site in new[]{24,200}) {
            byte[] image=Image(site,400);
            var found=WindowsLoader.Find(image,0x10000000,0x1000);
            Check(found.Call==0x10001000+site+Before.Length,"moved call site");
            Check(found.Target==0x10001190,"moved call target");
        }
        byte[] changed=Image(24,400);
        changed[24]=0x90;
        Reject(changed,"changed loader instructions must stop");
        byte[] outside=Image(24,600);
        Reject(outside,"call target outside code must stop");
        byte[] ambiguous=Image(24,400), second=Image(200,400);
        Array.Copy(second,200,ambiguous,200,Before.Length+5+After.Length);
        Reject(ambiguous,"ambiguous matches must stop");

        long baseAddress=Native.GetModuleHandleW("ntdll.dll").ToInt64();
        var live=WindowsLoader.Locate(baseAddress,(address,length)=>{
            var bytes=new byte[length];
            Marshal.Copy(new IntPtr(address),bytes,0,length);
            return bytes;
        });
        Check(live.Call>=baseAddress && live.Target>=baseAddress,"current Windows loader was found");

        string notepad=Path.Combine(Environment.SystemDirectory,"notepad.exe");
        var startup=new Native.StartupInfo();startup.cb=(uint)Marshal.SizeOf(startup);
        Native.ProcessInfo child;
        Check(Native.CreateProcessW(notepad,new StringBuilder("\""+notepad+"\""),IntPtr.Zero,IntPtr.Zero,false,4,IntPtr.Zero,Environment.SystemDirectory,ref startup,out child),"start suspended test process");
        try {
            long childBase=baseAddress;
            var remote=WindowsLoader.Locate(childBase,(address,length)=>{
                var bytes=new byte[length];UIntPtr read;
                return Native.ReadProcessMemory(child.process,new IntPtr(address),bytes,new UIntPtr((uint)length),out read) && read.ToUInt64()==(ulong)length ? bytes:null;
            });
            Check(remote.Call>=childBase && remote.Target>=childBase,"find repair site in suspended process");
        }
        finally {
            Native.TerminateProcess(child.process,1);
            Native.CloseHandle(child.thread);
            Native.CloseHandle(child.process);
        }
        Console.WriteLine(checks+" Windows loader checks passed");
        return 0;
    }
}
