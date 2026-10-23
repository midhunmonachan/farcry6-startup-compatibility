using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class Launcher
{
    const string GameDirectory = @"C:\Games\FarCry6\bin";
    static readonly byte[] OriginalLoaderCall = {0xe8,0x65,0x3c,0x10,0};
    static readonly byte[] OriginalInitializer = {0xe9,0x7b,0x9b,0x24,0x1a};
    static readonly Dictionary<string,string> Supported = new Dictionary<string,string> {
        {"FarCry6.exe","95C71228D719E8FD68BEE4139B0C5ED253B34B7434698A77AC8E4D2ADAABCCD9"},
        {"FC_m64d3d12.dll","64A58E8A068ACE1CF10DEC0698C2CEEC0741E3E060F4498CCBCA2AADEAC3B3EF"},
        {"EMP.dll","D6041735C6769FEC381C85DF1117EF58240AA2DBC4802B99963DDCA739B4203A"},
        {"upc_r2_loader64.dll","2CE02509BA79EBBAD93D0EBD44B20FB1DC62FCA20E8A43620648FDA8617B29B0"},
        {"amd_ags_x64.dll","BA6CAFE8F57BD17F63B6B65915C494BB36735EDCDDF8D5FC5E0C6B8B6295F0AC"},
        {"dbdata.dll","EE8CC455429935C7B8C6BE5947D1B0DE95AA4874E3BB48970CFACC5D5337C30C"},
        {"uplay_r2.ini","CCAEF16039F133F89021999820FDCE689FBB8CD4E4350B9D332F87AAC18A3A5F"}
    };

    [STAThread] static int Main()
    {
        try { Console.WriteLine("Started Far Cry 6. Process ID: "+Launch()); return 0; }
        catch(Exception error) { MessageBox.Show(error.Message,"Far Cry 6 compatibility launcher",MessageBoxButtons.OK,MessageBoxIcon.Error); return 1; }
    }

    static uint Launch()
    {
        if(IntPtr.Size!=8)throw new InvalidOperationException("Use the 64-bit compatibility launcher.");
        string exe=Path.Combine(GameDirectory,"FarCry6.exe");
        foreach(var process in Process.GetProcessesByName("FarCry6")) {
            using(process) {
                try { if(string.Equals(process.MainModule.FileName,exe,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Far Cry 6 is already running. Close it before launching it again."); }
                catch(Win32Exception) { }
            }
        }
        foreach(var item in Supported) {
            string actual;
            using(var file=File.OpenRead(Path.Combine(GameDirectory,item.Key)))
            using(var hash=SHA256.Create())actual=BitConverter.ToString(hash.ComputeHash(file)).Replace("-","");
            if(actual!=item.Value)throw new InvalidOperationException("This launcher supports the tested game build only. The file "+item.Key+" has changed. Restore the original file before using this launcher.");
        }
        var si=new Native.StartupInfo();si.cb=(uint)Marshal.SizeOf(si);var pi=new Native.ProcessInfo();
        IntPtr environment=IntPtr.Zero;bool released=false;
        try {
            var sorted=new SortedDictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(DictionaryEntry item in Environment.GetEnvironmentVariables())if(!string.Equals((string)item.Key,"__COMPAT_LAYER",StringComparison.OrdinalIgnoreCase))sorted[(string)item.Key]=(string)item.Value;
            var env=new StringBuilder();foreach(var item in sorted)env.Append(item.Key).Append('=').Append(item.Value).Append('\0');env.Append('\0');
            environment=Marshal.StringToHGlobalUni(env.ToString());
            Check(Native.CreateProcessW(exe,new StringBuilder("\""+exe+"\""),IntPtr.Zero,IntPtr.Zero,false,0x404,environment,GameDirectory,ref si,out pi),"Start Far Cry 6");
            IntPtr process=pi.process;
            long ntdll=Native.GetModuleHandleW("ntdll.dll").ToInt64();long call=ntdll+0x1b2de;
            if(!Equal(Read(process,call,5),OriginalLoaderCall))throw new InvalidOperationException("This Windows loader differs from the tested version. The compatibility launcher cannot safely apply this repair.");
            long thunk=AllocateNear(process,ntdll);Write(process,thunk,RepairCode.LoaderThunk(ntdll+0x11ef48));MakeExecutable(process,thunk);
            byte[] privateCall=RepairCode.Branch(0xe8,call,thunk);Patch(process,call,OriginalLoaderCall,privateCall);
            Check(Native.ResumeThread(pi.thread)!=0xffffffff,"Resume Far Cry 6");
            bool handlerFixed=false,initializerFixed=false,loaderRestored=false;long engine=0;
            var timer=Stopwatch.StartNew();
            while(timer.Elapsed.TotalSeconds<60) {
                if(Native.WaitForSingleObject(process,0)==0)throw new InvalidOperationException("Far Cry 6 exited before its startup repair finished.");
                if(!handlerFixed && Equal(Read(process,0x130220b3,6),new byte[]{0xff,0x15,0x9f,0x40,0xfe,0xff})) {
                    Freeze(process,delegate {
                        // Preserve the Windows exception-handler ABI, and prevent an empty CPU-brand block from matching ordinary zero values.
                        Patch(process,0x1301fb07,new byte[]{0x48,0xc7,0xc3,0xc0,0xfa,1,0x13},new byte[]{0x90,0x90,0x90,0x90,0x90,0x90,0x90});
                        Patch(process,0x13033871,new byte[]{0x75,0x57},new byte[]{0xeb,0x57});
                        Patch(process,0x130267c8,new byte[]{0x75,0x33},new byte[]{0xeb,0x33});
                    });
                    handlerFixed=true;
                }
                if(engine==0)engine=EngineBase(process);
                if(engine!=0 && handlerFixed && !initializerFixed && Equal(Read(process,engine+0x42a2570,5),OriginalInitializer)) {
                    Freeze(process,delegate {
                        long replacement=AllocateNear(process,engine);
                        Write(process,replacement,RepairCode.CrcInitializer(engine+0x6f63118));MakeExecutable(process,replacement);
                        // Equivalent static initializer: CRC32("CNetMsgCharacterStatus_Unicast"), flag bit, and storage pointer.
                        Patch(process,engine+0x42a2570,OriginalInitializer,RepairCode.Branch(0xe9,engine+0x42a2570,replacement));
                    });
                    initializerFixed=true;
                }
                if(engine!=0 && !loaderRestored && Equal(Read(process,engine+0x1ea42010,1),new byte[]{0xc3})) {
                    Freeze(process,delegate {Patch(process,call,privateCall,OriginalLoaderCall);});loaderRestored=true;
                }
                if(handlerFixed && initializerFixed && loaderRestored) {released=true;return pi.pid;}
                Thread.Sleep(10);
            }
            throw new TimeoutException("Far Cry 6 did not finish its startup repair within one minute.");
        }
        finally {
            if(environment!=IntPtr.Zero)Marshal.FreeHGlobal(environment);
            if(pi.process!=IntPtr.Zero) {
                if(!released && Native.WaitForSingleObject(pi.process,0)!=0) {Native.TerminateProcess(pi.process,1);Native.WaitForSingleObject(pi.process,5000);}
                Native.CloseHandle(pi.thread);Native.CloseHandle(pi.process);
            }
        }
    }

    static void Freeze(IntPtr process,Action action)
    {
        if(Native.NtSuspendProcess(process)!=0)throw new InvalidOperationException("Could not pause the new game process for its startup repair.");
        try {action();}finally {if(Native.NtResumeProcess(process)!=0)throw new InvalidOperationException("Could not resume the new game process.");}
    }
    static long EngineBase(IntPtr process)
    {
        var modules=new IntPtr[2048];uint needed;
        if(!Native.EnumProcessModulesEx(process,modules,(uint)(modules.Length*8),out needed,3)) {
            int error=Marshal.GetLastWin32Error();if(error==299)return 0;throw new Win32Exception(error,"Read game modules");
        }
        for(int i=0;i<Math.Min(modules.Length,needed/8);i++) {
            var path=new StringBuilder(4096);
            if(Native.GetModuleFileNameExW(process,modules[i],path,path.Capacity)>0 && string.Equals(Path.GetFileName(path.ToString()),"FC_m64d3d12.dll",StringComparison.OrdinalIgnoreCase))return modules[i].ToInt64();
        }
        return 0;
    }
    static byte[] Read(IntPtr process,long address,int length)
    {
        var bytes=new byte[length];UIntPtr read;
        return Native.ReadProcessMemory(process,new IntPtr(address),bytes,new UIntPtr((uint)length),out read) && read.ToUInt64()==(ulong)length ? bytes:null;
    }
    static bool Equal(byte[] a,byte[] b) {if(a==null || a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    static void Write(IntPtr process,long address,byte[] bytes)
    {
        UIntPtr written;Check(Native.WriteProcessMemory(process,new IntPtr(address),bytes,new UIntPtr((uint)bytes.Length),out written) && written.ToUInt64()==(ulong)bytes.Length,"Write new game process memory");
        Check(Native.FlushInstructionCache(process,new IntPtr(address),new UIntPtr((uint)bytes.Length)),"Refresh game instructions");
    }
    static void Patch(IntPtr process,long address,byte[] original,byte[] replacement)
    {
        if(!Equal(Read(process,address,original.Length),original))throw new InvalidOperationException("The game startup code differs from the verified repair. Startup has been stopped.");
        uint old;Check(Native.VirtualProtectEx(process,new IntPtr(address),new UIntPtr((uint)replacement.Length),0x40,out old),"Prepare game startup repair");
        try {Write(process,address,replacement);}finally {uint ignored;Check(Native.VirtualProtectEx(process,new IntPtr(address),new UIntPtr((uint)replacement.Length),old,out ignored),"Restore game page permissions");}
    }
    static long AllocateNear(IntPtr process,long anchor)
    {
        anchor &= ~0xffffL;
        for(long distance=0x10000;distance<0x70000000;distance+=0x10000)foreach(long address in new long[]{anchor-distance,anchor+distance}) {
            IntPtr allocation=Native.VirtualAllocEx(process,new IntPtr(address),new UIntPtr(4096),0x3000,4);
            if(allocation!=IntPtr.Zero)return allocation.ToInt64();
        }
        throw new InvalidOperationException("There is no available memory near the game startup code.");
    }
    static void MakeExecutable(IntPtr process,long address) {uint old;Check(Native.VirtualProtectEx(process,new IntPtr(address),new UIntPtr(4096),0x20,out old),"Prepare compatibility instructions");}
    static void Check(bool success,string message) {if(!success)throw new Win32Exception(Marshal.GetLastWin32Error(),message);}
}

internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] internal struct StartupInfo {
        internal uint cb;internal IntPtr reserved,desktop,title;internal uint x,y,width,height,xchars,ychars,fill,flags;internal ushort show,reservedLength;internal IntPtr reserved2,stdin,stdout,stderr;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct ProcessInfo {internal IntPtr process,thread;internal uint pid,tid;}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern bool CreateProcessW(string app,StringBuilder command,IntPtr pa,IntPtr ta,bool inherit,uint flags,IntPtr environment,string cwd,ref StartupInfo startup,out ProcessInfo process);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr GetModuleHandleW(string name);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool ReadProcessMemory(IntPtr process,IntPtr address,byte[] data,UIntPtr length,out UIntPtr read);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool WriteProcessMemory(IntPtr process,IntPtr address,byte[] data,UIntPtr length,out UIntPtr written);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool VirtualProtectEx(IntPtr process,IntPtr address,UIntPtr size,uint protection,out uint old);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr VirtualAllocEx(IntPtr process,IntPtr address,UIntPtr size,uint type,uint protection);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool FlushInstructionCache(IntPtr process,IntPtr address,UIntPtr length);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern uint ResumeThread(IntPtr thread);
    [DllImport("kernel32.dll")] internal static extern uint WaitForSingleObject(IntPtr handle,uint milliseconds);
    [DllImport("kernel32.dll")] internal static extern bool TerminateProcess(IntPtr process,uint code);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
    [DllImport("ntdll.dll")] internal static extern int NtSuspendProcess(IntPtr process);
    [DllImport("ntdll.dll")] internal static extern int NtResumeProcess(IntPtr process);
    [DllImport("psapi.dll",SetLastError=true)] internal static extern bool EnumProcessModulesEx(IntPtr process,IntPtr[] modules,uint size,out uint needed,uint filter);
    [DllImport("psapi.dll",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern uint GetModuleFileNameExW(IntPtr process,IntPtr module,StringBuilder name,int size);
}
