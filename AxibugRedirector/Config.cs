using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxibugRedirector
{
    //public struct CfgInfo
    //{
    //    public string hostname;
    //    public string targetIP;
    //}
    public static class Config
    {
        public static bool LoadConfig(out Dictionary<string, string> dictHostToIP)
        {
            dictHostToIP = new Dictionary<string, string>();
            try
            {
                StreamReader sr = new StreamReader(System.Environment.CurrentDirectory + "//config.cfg", Encoding.Default);
                String line;
                while (!string.IsNullOrEmpty((line = sr.ReadLine())))
                {
                    if (!line.Contains(":"))
                        continue;
                    try
                    {
                        dictHostToIP[line.Split(':')[0].Trim()] = line.Split(':')[1].Trim();
                    }
                    catch
                    {
                        continue;
                    }
                }
                sr.Close();
                if (dictHostToIP.Count > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("配置文件异常：" + ex.ToString());
                return false;
            }
        }
    }
}
