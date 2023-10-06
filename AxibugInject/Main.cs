using System;
using System.Text;
using System.Runtime.InteropServices;
using EasyHook;
using System.Threading;
using System.Windows.Forms;
using ClassLibrary1;
using System.Collections.Generic;
using System.Xml.Linq;
using static AxibugInject.ws2_32;

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
        public LocalHook GetHostByAddrHook = null;
        public LocalHook gethostnameHook = null;
        public LocalHook connectHook = null;

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
                ConsoleShow.Log($"Hook函数ws2_32.dll->gethostbyname");
                GetHostByNameHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "gethostbyname"),
                    new DGetHostByName(GetHostByName_Hooked),
                    this);
                GetHostByNameHook.ThreadACL.SetExclusiveACL(new int[1]);

                ConsoleShow.Log($"Hook函数ws2_32.dll->gethostbyaddr");
                GetHostByAddrHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "gethostbyaddr"),
                    new Dgethostbyaddr(gethostbyaddr_Hooked),
                    this);
                GetHostByAddrHook.ThreadACL.SetExclusiveACL(new int[1]);

                ConsoleShow.Log($"Hook函数ws2_32.dll->gethostname");
                gethostnameHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "gethostname"),
                    new Dgethostname(gethostname_Hooked),
                    this);
                gethostnameHook.ThreadACL.SetExclusiveACL(new int[1]);

                ConsoleShow.Log($"Hook函数ws2_32.dll->connect");
                connectHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "connect"),
                    new Dconnect(connect_Hooked),
                    this);
                connectHook.ThreadACL.SetExclusiveACL(new int[1]);
                
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

        #region gethostbyname

        [DllImport("ws2_32.dll", EntryPoint = "gethostbyname", CharSet = CharSet.Ansi)]
        public static extern IntPtr gethostbyname(String name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        delegate IntPtr DGetHostByName(String name);
        static IntPtr GetHostByName_Hooked(
            String name)
        {
            try
            {
                ConsoleShow.Log($"gethostbyname[调用]name->{name}");
                Main This = (Main)HookRuntimeInfo.Callback;
                if (mDictHostToIP.ContainsKey(name.ToLower()))
                {
                    ConsoleShow.Log($"gethostbyname[访问并重定向]{name}->{mDictHostToIP[name]}");
                    name = mDictHostToIP[name.ToLower()];
                }
                else
                {
                    ConsoleShow.Log("gethostbyname[访问]：" + name);
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

        #region gethostname

        [DllImport("ws2_32.dll", SetLastError = true)]
        static extern int gethostname(StringBuilder name, int length);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        delegate int Dgethostname(StringBuilder name, int length);
        static int gethostname_Hooked(StringBuilder name, int length)
        {
            ConsoleShow.Log($"gethostname[调用]name->{name} length->{length}");
            // call original API...
            return gethostname(name, length);
        }
        #endregion

        #region gethostbyaddr

        [DllImport("ws2_32.dll", EntryPoint = "gethostbyaddr", CharSet = CharSet.Ansi)]
        public static extern IntPtr gethostbyaddr(String addr, int len,int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        delegate IntPtr Dgethostbyaddr(String addr, int len, int type);
        static IntPtr gethostbyaddr_Hooked(String addr, int len,int type)
        {
            ConsoleShow.Log($"gethostbyaddr[调用]addr->{addr} len->{len} type->{type}");
            try
            {
                Main This = (Main)HookRuntimeInfo.Callback;
                if (mDictHostToIP.ContainsKey(addr.ToLower()))
                {
                    ConsoleShow.Log($"gethostbyaddr[访问并重定向]{addr}->{mDictHostToIP[addr]}");
                    addr = mDictHostToIP[addr.ToLower()];
                }
                else
                {
                    ConsoleShow.Log("gethostbyaddr[访问]：" + addr);
                }
            }
            catch
            {
            }

            // call original API...
            return gethostbyaddr(addr, len, type);
        }
        #endregion


        #region connect

        //[StructLayout(LayoutKind.Sequential)]
        //public struct sockaddr_in6
        //{
        //    public short sin6_family;
        //    public ushort sin6_port;
        //    public uint sin6_flowinfo;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        //    public byte[] sin6_addr;
        //    public uint sin6_scope_id;
        //}
        [DllImport("Ws2_32.dll")]
        public static extern int connect(IntPtr SocketHandle, ref sockaddr_in_old addr, int addrsize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        delegate int Dconnect(IntPtr SocketHandle, ref sockaddr_in_old addr, int addrsize);
        static int connect_Hooked(IntPtr SocketHandle, ref sockaddr_in_old addr, int addrsize)
        {
            ConsoleShow.Log($"connect[调用]SocketHandle->{SocketHandle} addr->{addr} addrsize->{addrsize}");
            ConsoleShow.Log($"connect sockaddr_in 详情 :sin_family->{addr.sin_family} sin_addr->{addr.sin_addr}" +
                $" sin_port->{GetPort(addr.sin_port)}");
            /*ConsoleShow.Log($"connect sockaddr_in 详情 :sin_family->{addr.sin_family} sin_addr->{addr.sin_addr.s_b1}.{addr.sin_addr.s_b2}.{addr.sin_addr.s_b3}.{addr.sin_addr.s_b4}" +
                $" sin_port->{GetPort(addr.sin_port)}");*/
            // call original API...
            return connect(SocketHandle, ref addr, addrsize);
        }


        static int GetPort(ushort Tbed)
        {
            if (Tbed < 256)
                return Tbed;

            byte gao = (byte)(Tbed >> 8);
            byte di = (byte)(Tbed & 0xff);

            ushort a = (ushort)(gao << 8);
            ushort b = (ushort)di;
            //ushort newBed = (ushort)(a | di);

            ushort newT = (ushort)(gao | di << 8);
            return newT;
        }
        #endregion


    }
}
