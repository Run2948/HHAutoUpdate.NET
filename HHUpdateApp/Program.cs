using HHUpdateApp.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace HHUpdateApp
{
    static class Program
    {
        /// <summary>
        /// 程序主入口
        /// </summary>
        /// <param name="args">[0]程序名称</param>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //C# Mutex：（互斥锁）线程同步
            //避免程序重复运行
            Mutex mutex = new Mutex(true, "HHUpdateApp_OnlyRunOneInstance", out bool isRunning);
            //第一个参数:true--给调用线程赋予互斥体的初始所属权
            //第一个参数:TQ_WinClient_OnlyRunOneInstance--互斥体的名称
            //第三个参数:返回值isRunning,如果调用线程已被授予互斥体的初始所属权,则返回true

            if (isRunning)
            {
                try
                {
                    if (args.Length == 0)
                    {
                        SettingForm set = new SettingForm();
                        set.ShowDialog();
                    }
                    else
                    {
                        //拉起更新请求的业务程序，稍后更新时，根据这个值关闭对应的进程
                        LaunchAppName = args[0];
                        //安装更新模式：1,静默安装；0,手动安装（区别就是，自动更新的状态下，如果有新版本更新，就会后台静默安装）
                        SilentUpdate = args[1] == "1" || args[1] == "True";
                    }

                    /* 
                     * 当前用户是管理员的时候，直接启动应用程序 
                     * 如果不是管理员，则使用启动对象启动程序，以确保使用管理员身份运行 
                     */

                    //获得当前登录的Windows用户标示 
                    System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                    System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);

                    //判断当前登录用户是否为管理员 
                    if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                    {
                        Application.Run(new MainForm(LaunchAppName, SilentUpdate));
                    }
                    else
                    {
                        string result = Environment.GetEnvironmentVariable("systemdrive");
                        if (AppDomain.CurrentDomain.BaseDirectory.Contains(result))
                        {
                            //创建启动对象 
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                //设置运行文件 
                                FileName = Application.ExecutablePath,
                                //设置启动动作,确保以管理员身份运行 
                                Verb = "runas",

                                Arguments = " " + LaunchAppName
                            };
                            //如果不是管理员，则启动UAC 
                            Process.Start(startInfo);
                        }
                        else
                        {
                            Application.Run(new MainForm(LaunchAppName, SilentUpdate));
                        }
                    }
                }
                catch (Exception ex)
                {
                    HHMessageBox.Show(ex.Message, "错误");
                }
            }
        }

        /// <summary>
        /// 拉起更新请求的业务程序名，稍后更新时，根据这个值关闭对应的进程
        /// </summary>
        public static string LaunchAppName = Settings.Default.LaunchAppName;

        /// <summary>
        /// 用于记录检查更新时一个可以忽略的版本
        /// </summary>
        public static string LocalIgnoreVer = Settings.Default.LocalIgnoreVer;

        /// <summary>
        /// 更新信息的JSON文件所在位置
        /// </summary>
        public static string ServerUpdateUrl = Settings.Default.ServerUpdateUrl;

        /// <summary>
        /// 安装更新模式：true,静默安装；false,手动安装（区别就是，自动更新的状态下，如果有新版本更新，就会后台静默安装）
        /// </summary>
        public static bool SilentUpdate = Settings.Default.SilentUpdate;
    }
}
