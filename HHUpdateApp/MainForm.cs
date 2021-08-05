using HHUpdateApp.Properties;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HHUpdateApp
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// 需要更新的业务应用程序,稍后如果需要更新,根据这个名字把相应进程关闭
        /// </summary>
        private readonly string launchAppName;

        /// <summary>
        /// 需要更新的业务应用程序所在目录
        /// </summary>
        private string launchAppDirectoryName;

        /// <summary>
        /// 需要更新的业务应用程序
        /// </summary>
        private readonly string[] allLaunchAppNames;

        /// <summary>
        /// 需要更新的业务应用程序版本号
        /// </summary>
        private string launchAppVer;

        /// <summary>
        /// 需要更新的业务应用程序关联的进程
        /// </summary>
        private Process[] launchProcess;

        /// <summary>
        /// 安装更新模式：true,静默安装；false,手动安装（区别就是，自动更新的状态下，如果有新版本更新，就会后台静默安装）
        /// </summary>
        private readonly bool silentUpdate;

        /// <summary>
        /// 服务器上的版本信息
        /// </summary>
        private RemoteVersionInfo verInfo;

        public MainForm(string launchAppName, bool silentUpdate)
        {
            InitializeComponent();
            allLaunchAppNames = launchAppName.Split('#');
            this.launchAppName = allLaunchAppNames[0];
            this.silentUpdate = silentUpdate;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //通过业务应用程序名，获取其进程信息
            launchProcess = Process.GetProcessesByName(launchAppName);

            if (launchProcess.Length > 0)
            {
                launchAppDirectoryName = Path.GetDirectoryName(launchProcess[0].MainModule.FileName);
                launchAppVer = launchProcess[0].MainModule.FileVersionInfo.ProductVersion;

                //检查是否是被设置忽略的版本号
                if (launchAppVer == Program.LocalIgnoreVer)
                {
                    if (!silentUpdate)
                    {
                        HHMessageBox.Show("当前版本已经是最新版本");
                    }
                    else
                    {
                        LogManger.Instance.Info($"当前版本 {launchAppVer} 是被设置为忽略更新的版本");
                    }

                    Application.Exit();
                }
            }
            else
            {
                if (!silentUpdate)
                {
                    HHMessageBox.Show("应用程序未启动: _" + launchAppName);
                }
                else
                {
                    LogManger.Instance.Info("应用程序未启动: _" + launchAppName);
                }

                Application.Exit();
            }

            //下载服务器上版本更新信息
            verInfo = DownloadUpdateInfo(Program.ServerUpdateUrl);

            if (verInfo != null)
            {
                //比较版本号
                if (VersionCompare(launchAppVer, verInfo.ReleaseVersion) >= 0)
                {
                    //this.Hide();//隐藏当前窗口

                    if (!silentUpdate)
                    {
                        HHMessageBox.Show("当前版本已经是最新版本");
                    }
                    else
                    {
                        LogManger.Instance.Info("当前版本已经是最新版本");
                    }

                    Application.Exit();
                }
                else
                {
                    if (!silentUpdate)
                    {
                        this.lblContent.Text = verInfo.VersionDesc;
                    }
                    else
                    {
                        this.Hide(); //隐藏当前窗口

                        UpdateWork work = new UpdateWork(launchAppDirectoryName, verInfo, silentUpdate);

                        //关闭业务应用程序关联的进程
                        foreach (var appName in allLaunchAppNames)
                        {
                            foreach (var process in Process.GetProcessesByName(appName))
                            {
                                process.Kill();
                                process.Close();
                            }
                        }

                        UpdateForm updateForm = new UpdateForm(work);
                        if (updateForm.ShowDialog() == DialogResult.OK)
                        {
                            Application.Exit();
                        }
                    }
                }
            }
            else
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// 如果以后更新,则将更新程序关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateLater_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// 立即更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateNow_Click(object sender, EventArgs e)
        {
            this.Hide(); //隐藏当前窗口

            UpdateWork work = new UpdateWork(launchAppDirectoryName, verInfo, silentUpdate);

            //关闭业务应用程序关联的进程
            foreach (var appName in allLaunchAppNames)
            {
                foreach (var process in Process.GetProcessesByName(appName))
                {
                    process.Kill();
                    process.Close();
                }
            }

            UpdateForm updateForm = new UpdateForm(work);
            if (updateForm.ShowDialog() == DialogResult.OK)
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// 忽略本次版本更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnIgnore_Click(object sender, EventArgs e)
        {
            //忽略这个版本更新，后面版本在做吧
            Application.Exit();
        }


        #region 让窗体变成可移动

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

        [DllImport("User32.dll")]
        private static extern IntPtr WindowFromPoint(Point p);

        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;
        private IntPtr moveObject = IntPtr.Zero; //拖动窗体的句柄

        private void PNTop_MouseDown(object sender, MouseEventArgs e)
        {
            if (moveObject == IntPtr.Zero)
            {
                if (this.Parent != null)
                {
                    moveObject = this.Parent.Handle;
                }
                else
                {
                    moveObject = this.Handle;
                }
            }

            ReleaseCapture();
            SendMessage(moveObject, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 下载服务器上版本信息
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <returns></returns>
        private RemoteVersionInfo DownloadUpdateInfo(string serverUrl)
        {
            using (WebClient updateClt = new WebClient())
            {
                string updateJson;
                try
                {
                    byte[] bJson = updateClt.DownloadData(serverUrl);
                    updateJson = System.Text.Encoding.UTF8.GetString(bJson);
                }
                catch (Exception ex)
                {
                    LogManger.Instance.Error("下载服务器上版本信息错误", ex);
                    HHMessageBox.Show($"升级信息从 {serverUrl} 下载失败：{ex.Message}", "错误");
                    return null;
                }

                try
                {
                    RemoteVersionInfo info = JsonConvert.DeserializeObject<RemoteVersionInfo>(updateJson);
                    return info;
                }
                catch (Exception ex)
                {
                    LogManger.Instance.Error("升级 json 文件错误", ex);
                    HHMessageBox.Show($"升级 json 文件错误：{ex.Message}\r\n{ex.Message}", "错误");
                    return null;
                }
            }
        }

        /// <summary>
        /// 比较两个版本号的大小。
        /// </summary>
        /// <param name="ver1">版本 1。</param>
        /// <param name="ver2">版本 2。</param>
        /// <returns>大于 0，则 ver1 大；小于 0，则 ver2 大；0，则相等。</returns>
        /// <remarks>通过将版本号中的数字点拆分为字符串数组进行比较，比较每个字符串的大小，如果字符串可以转换为数字，则使用数字比较。
        /// </remarks>
        private static int VersionCompare(string ver1, string ver2)
        {
            if (string.IsNullOrEmpty(ver2)) return 1;

            string[] item1 = ver1.Split('.');
            string[] item2 = ver2.Split('.');
            int len = item1.Length > item2.Length ? item1.Length : item2.Length;
            int i = 0;
            int cmpValue = 0;
            while (i < len && cmpValue == 0)
            {
                if (int.TryParse(item1[i], out int i1) && int.TryParse(item2[i], out int i2))
                {
                    cmpValue = i1 - i2;
                }
                else
                {
                    cmpValue = string.CompareOrdinal(item1[i], item2[i]);
                }

                i++;
            }

            // 两个版本长度不一致，但是前一部分相同的，以长度长的为大。
            if (cmpValue == 0 && item1.Length != item2.Length)
            {
                cmpValue = item1.Length - item2.Length;
            }

            return cmpValue;
        }

        #endregion
    }
}