using System;
using System.Text;
using System.Runtime.InteropServices;
using EasyHook;
using System.Threading;
using System.Windows.Forms;
using ClassLibrary1;
using System.Collections.Generic;

namespace AxibugInject
{

    
    [Serializable]
    public class HookParameter
    {
        public string Msg { get; set; }
        public int HostProcessId { get; set; }
        public string RedirectorArrs { get; set; }
    }

    public class Main : IEntryPoint
    {
        public LocalHook GetHostByNameHook = null;

        public static Dictionary<string, string> mDictHostToIP = new Dictionary<string, string>();
        public Main(
            RemoteHooking.IContext context,
            string channelName
            , HookParameter parameter
            )
        {
            
            
            string[] RedirectorArrs = parameter.RedirectorArrs.Split('|');
            try
            {
                for(int i = 0;i < RedirectorArrs.Length;i++)
                {
                    string line = RedirectorArrs[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;
                    string[] arr = RedirectorArrs[i].Trim().Split(':');
                    if (arr.Length < 2)
                        continue;
                    mDictHostToIP[arr[0].Trim()] = arr[1].Trim();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            MessageBox.Show(parameter.Msg + ",并加载:" + mDictHostToIP.Count + "个重定向配置", "Hooked");
        }

        public void Run(
            RemoteHooking.IContext context,
            string channelName
            , HookParameter parameter
            )
        {
            try
            {
                GetHostByNameHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "gethostbyname"),
                    new DGetHostByName(GetHostByName_Hooked),
                    this);
                GetHostByNameHook.ThreadACL.SetExclusiveACL(new int[1]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            try
            {
                while (true)
                {
                    Thread.Sleep(10);
                }
            }
            catch
            {

            }
        }

        #region gethostname

        [DllImport("ws2_32.dll", EntryPoint = "gethostbyname", CharSet = CharSet.Ansi)]
        public static extern IntPtr gethostbyname(String name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        delegate IntPtr DGetHostByName(String name);
        static IntPtr GetHostByName_Hooked(
            String name)
        {
            try
            {
                Main This = (Main)HookRuntimeInfo.Callback;
                if (mDictHostToIP.ContainsKey(name.ToLower()))
                {
                    ConsoleShow.Log($"[访问并重定向]{name}->{mDictHostToIP[name]}");
                    name = mDictHostToIP[name.ToLower()];
                }
                else
                {
                    ConsoleShow.Log("[访问]：" + name);
                }
            }
            catch
            {
            }

            // call original API...
            return gethostbyname(
                name);
        }
        #endregion
    }
}
