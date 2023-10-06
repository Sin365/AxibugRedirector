using AxibugInject;
using EasyHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AxibugRedirector
{
    internal class Program
    {
        static Dictionary<string, string> mDictHostToIP;
        static string mHostToIPArr;
        static void Main(string[] args)
        {
            if (!Config.LoadConfig(out Dictionary<string, string> dictHostToIP))
            {
                Console.WriteLine("请检查配置文件!");
                Console.ReadLine();
                return;
            }
            mDictHostToIP = dictHostToIP;
            Console.WriteLine("配置文件加载完毕!");
            foreach (var d in mDictHostToIP)
            {
                Console.WriteLine($"{d.Key}->{d.Value}");
                mHostToIPArr += $"{d.Key}:{d.Value}|";
            }
            Console.WriteLine("Pipie Server加载!");

            bool bflag = false;
            while (!bflag)
            {
                Console.WriteLine("----请指定进程----");
                Console.WriteLine("[1]使用PID注入，[2]使用进程名（不带exe）[3]指定exe路径，启动exe后hook");
                string readStr = Console.ReadLine();
                if (int.TryParse(readStr, out int type))
                {
                    if (type == 1)
                    {
                        Console.Write("请输入目标进程PID：");
                        if (int.TryParse(readStr, out int pid))
                        {
                            if (DoInjectByPid(pid))
                            {
                                bflag = true;
                            }
                        }
                    }
                    else if (type == 2)
                    {
                        Console.Write("使用进程名（不带exe）：");
                        string readName = Console.ReadLine();
                        if (string.IsNullOrEmpty(readName))
                        {
                            continue;
                        }
                        if (GetPidForProName(readName, out int targetPid))
                        {
                            if (DoInjectByPid(targetPid))
                            {
                                bflag = true;
                            }
                        }
                        else
                        {
                            Console.WriteLine("进程不存在");
                        }
                    }
                    else if (type == 3)
                    {
                        Console.Write("指定exe路径，启动exe后hook:");
                        string path = Console.ReadLine();
                        if (string.IsNullOrEmpty(path))
                        {
                            continue;
                        }

                        if (StartProcessWithHook(path))
                        {
                            bflag = true;
                        }
                    }

                }
            }

            Console.WriteLine("已就绪");
            while (true)
            {
                string str = Console.ReadLine();
                if (int.TryParse(str, out int cmd))
                {
                    if (cmd == 4)
                    {
                        Console.WriteLine($"再次注入PID{CurrPid}");
                        if (DoInjectByPid(cmd))
                        {
                            bflag = true;
                            Console.WriteLine($"再次注入PID{CurrPid}成功！");
                        }
                    }
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);


        private static bool RegGACAssembly()
        {
            var dllName = "EasyHook.dll";
            var dllPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
            if (System.Runtime.InteropServices.RuntimeEnvironment.FromGlobalAccessCache(Assembly.LoadFrom(dllPath)))
                new System.EnterpriseServices.Internal.Publish().GacRemove(dllPath);
            Thread.Sleep(100);
            new System.EnterpriseServices.Internal.Publish().GacInstall(dllPath);
            Thread.Sleep(100);
            if (System.Runtime.InteropServices.RuntimeEnvironment.FromGlobalAccessCache(Assembly.LoadFrom(dllPath)))
                Console.WriteLine("{0} registered to GAC successfully.", dllName);
            else
            {
                Console.WriteLine("{0} registered to GAC failed.", dllName);
                return false;
            }

            dllName = "AxibugInject.dll";
            dllPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
            if (System.Runtime.InteropServices.RuntimeEnvironment.FromGlobalAccessCache(Assembly.LoadFrom(dllPath)))
                new System.EnterpriseServices.Internal.Publish().GacRemove(dllPath);
            Thread.Sleep(100);
            new System.EnterpriseServices.Internal.Publish().GacInstall(dllPath);
            Thread.Sleep(100);
            if (System.Runtime.InteropServices.RuntimeEnvironment.FromGlobalAccessCache(Assembly.LoadFrom(dllPath)))
                Console.WriteLine("{0} registered to GAC successfully.", dllName);
            else
            {
                Console.WriteLine("{0} registered to GAC failed.", dllName);
                return false;
            }
            return true;
        }
        //private static bool RegGACAssembly()
        //{
        //    var dllName = "EasyHook.dll";
        //    var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
        //    if (!RuntimeEnvironment.FromGlobalAccessCache(Assembly.LoadFrom(dllPath)))
        //    {
        //        new System.EnterpriseServices.Internal.Publish().GacInstall(dllPath);
        //        Thread.Sleep(100);
        //    }

        //    dllName = "AxibugInject.dll";
        //    dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
        //    new System.EnterpriseServices.Internal.Publish().GacRemove(dllPath);
        //    if (!RuntimeEnvironment.FromGlobalAccessCache(Assembly.LoadFrom(dllPath)))
        //    {
        //        new System.EnterpriseServices.Internal.Publish().GacInstall(dllPath);
        //        Thread.Sleep(100);
        //    }

        //    return true;
        //}

        private static bool InstallHookInternal(int processId)
        {
            try
            {
                var parameter = new HookParameter
                {
                    Msg = "已经成功注入目标进程",
                    HostProcessId = RemoteHooking.GetCurrentProcessId(),
                    RedirectorArrs = mHostToIPArr
                };

                RemoteHooking.Inject(
                    processId,
                    InjectionOptions.Default,
                    typeof(HookParameter).Assembly.Location,
                    typeof(HookParameter).Assembly.Location,
                    string.Empty,
                    parameter
                );
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                return false;
            }

            return true;
        }

        private static bool IsWin64Emulator(int processId)
        {
            var process = Process.GetProcessById(processId);
            if (process == null)
                return false;

            if ((Environment.OSVersion.Version.Major > 5)
                || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)))
            {
                bool retVal;

                return !(IsWow64Process(process.Handle, out retVal) && retVal);
            }

            return false; // not on 64-bit Windows Emulator
        }
        public static bool DoInjectByPid(int Pid)
        {
            var p = Process.GetProcessById(Pid);
            if (p == null)
            {
                Console.WriteLine("指定的进程不存在!");
                return false;
            }

            if (IsWin64Emulator(p.Id) != IsWin64Emulator(Process.GetCurrentProcess().Id))
            {
                var currentPlat = IsWin64Emulator(Process.GetCurrentProcess().Id) ? 64 : 32;
                var targetPlat = IsWin64Emulator(p.Id) ? 64 : 32;
                Console.WriteLine(string.Format("当前程序是{0}位程序，目标进程是{1}位程序，请调整编译选项重新编译后重试！", currentPlat, targetPlat));
                return false;
            }

            RegGACAssembly();
            InstallHookInternal(p.Id);
            return true;
        }

        public static bool GetPidForProName(string ProcessName,out int targetPid)
        {
            Process[] process = Process.GetProcessesByName(ProcessName);
            if (process.Length > 0)
            {
                targetPid = process.FirstOrDefault().Id;
                return true;
            }
            else
            {
                targetPid = -1;
                return false;
            }
        }

        #region 运行时处理
        static int CurrPid;
        public static bool StartProcessWithHook(string path)
        {
            var pro = new Process();
            try
            {
                pro.StartInfo.FileName = path;
                pro.EnableRaisingEvents = true;
                //退出函数
                //pro.Exited += new EventHandler(StaticComm.LianJiNiang_ProcessExit);
                //pro.TotalProcessorTime
                pro.StartInfo.UseShellExecute = true;

                //参数
                //pro.StartInfo.Arguments = StaticComm.getLink(0);
                pro.Start();
                pro.WaitForInputIdle();
                //Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine("失败："+ex.ToString());
                return false;
            }
            CurrPid = pro.Id;
            return DoInjectByPid(pro.Id);
        }
        #endregion

    }
}
