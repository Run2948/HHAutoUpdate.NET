using Ionic.Zip;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace HHUpdateApp
{
    public class UpdateWork
    {
        #region 字段

        /// <summary>
        /// 临时目录（WIN7以及以上在C盘只有对于temp目录有操作权限）
        /// </summary>
        string tempPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), @"HHUpdateApp\temp\");

        /// <summary>
        /// 备份目录
        /// </summary>
        string bakPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), @"HHUpdateApp\bak\");

        /// <summary>
        /// 此更新程序所在目录
        /// </summary>
        private string mainDirectoryName;

        #endregion

        #region 属性

        /// <summary>
        /// 远程服务器上版本更新参数
        /// </summary>
        public RemoteVersionInfo RemoteVerInfo { get; set; }

        /// <summary>
        /// 需要更新的业务应用程序所在目录
        /// </summary>
        public string ProgramDirectoryName { get; set; }

        /// <summary>
        /// 静默安装更新
        /// </summary>
        public bool SilentUpdate { get; set; }

        #endregion

        /// <summary>
        /// 初始化配置目录信息
        /// </summary>
        /// <param name="programDirectoryName">需要更新的业务应用程序</param>
        /// <param name="remoteVerInfo">远程服务器上版本更新参数</param>
        /// <param name="silentUpdate">静默安装更新</param>
        public UpdateWork(string programDirectoryName, RemoteVersionInfo remoteVerInfo, bool silentUpdate)
        {
            ProgramDirectoryName = programDirectoryName;
            RemoteVerInfo = remoteVerInfo;
            SilentUpdate = silentUpdate;

            Process currentProcess = Process.GetCurrentProcess();

            mainDirectoryName = Path.GetFileName(Path.GetDirectoryName(currentProcess.MainModule.FileName));

            //创建备份目录信息
            DirectoryInfo bakInfo = new DirectoryInfo(bakPath);
            if (bakInfo.Exists == false)
            {
                bakInfo.Create();
            }

            //创建临时目录信息
            DirectoryInfo tempInfo = new DirectoryInfo(tempPath);
            if (tempInfo.Exists == false)
            {
                tempInfo.Create();
            }
        }

        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public UpdateWork()
        {
        }

        /// <summary>
        /// 业务应用重启
        /// </summary>
        public void AppStart()
        {
            string[] appStartName = RemoteVerInfo.ApplicationStart.Split('#');
            foreach (var item in appStartName)
            {
                LogManger.Instance.Info("应用程序重启：启动" + item);
                Process.Start(Path.Combine(ProgramDirectoryName, item));
            }

            return;
        }

        /// <summary>
        /// 下载方法
        /// </summary>
        public void DownLoad()
        {
            //从服务器上下载升级文件包
            using (WebClient web = new WebClient())
            {
                try
                {
                    LogManger.Instance.Info("下载更新程序：下载更新包文件" + RemoteVerInfo.ReleaseVersion);
                    web.DownloadFile(RemoteVerInfo.ReleaseUrl, tempPath + RemoteVerInfo.ReleaseVersion + ".zip");
                }
                catch (Exception ex)
                {
                    LogManger.Instance.Error("下载更新程序：更新包文件" + RemoteVerInfo.ReleaseVersion + "下载失败,本次停止更新，异常信息：" + ex);
                    throw ex;
                }
            }
        }

        /// <summary>
        /// 备份应用程序
        /// </summary>
        public void Bak()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(ProgramDirectoryName);
                foreach (var item in di.GetFiles())
                {
                    //判断文件是否是指定忽略的文件
                    if (!RemoteVerInfo.IgnoreFile.Contains(item.Name))
                    {
                        File.Copy(item.FullName, bakPath + item.Name, true);
                    }
                }

                //文件夹复制 
                foreach (var item in di.GetDirectories())
                {
                    //升级程序文件不需要备份
                    if (item.Name != mainDirectoryName)
                    {
                        CopyDirectory(item.FullName, bakPath);
                    }
                }

                LogManger.Instance.Info("备份应用程序：备份操作执行完成");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 更新应用程序
        /// </summary>
        public void Update()
        {
            try
            {
                //如果是全新安装的话，先删除原先的所有程序
                if (RemoteVerInfo.UpdateMode == "NewInstall")
                {
                    DelLocal();
                }

                string packageFileName = tempPath + RemoteVerInfo.ReleaseVersion + ".zip";

                using (ZipFile zip = new ZipFile(packageFileName, Encoding.Default))
                {
                    zip.ExtractAll(ProgramDirectoryName, ExtractExistingFileAction.OverwriteSilently);
                    LogManger.Instance.Info("更新应用程序：" + RemoteVerInfo.ReleaseVersion + ".zip" + " 解压完成");
                }
            }
            catch (Exception ex)
            {
                LogManger.Instance.Error("更新应用程序错误", ex);
                LogManger.Instance.Info("进行回滚操作");
                Restore();
            }
            finally
            {
                //删除下载的临时文件
                DelTempFile(RemoteVerInfo.ReleaseVersion + ".zip"); //删除更新包
                LogManger.Instance.Info("更新程序：临时文件 " + RemoteVerInfo.ReleaseVersion + ".zip" + " 删除完成");
            }
        }

        /// <summary>
        /// 文件拷贝
        /// </summary>
        /// <param name="srcDir">源目录</param>
        /// <param name="desDir">目标目录</param>
        private void CopyDirectory(string srcDir, string desDir)
        {
            string folderName = srcDir.Substring(srcDir.LastIndexOf("\\") + 1);

            string desFolderDir = desDir + "\\" + folderName;

            if (desDir.LastIndexOf("\\") == (desDir.Length - 1))
            {
                desFolderDir = desDir + folderName;
            }

            string[] fileNames = Directory.GetFileSystemEntries(srcDir);
            foreach (string file in fileNames) // 遍历所有的文件和目录
            {
                if (Directory.Exists(file)) // 先当作目录处理如果存在这个目录就递归Copy该目录下面的文件
                {
                    string currentDir = desFolderDir + "\\" + file.Substring(file.LastIndexOf("\\") + 1);
                    if (!Directory.Exists(currentDir))
                    {
                        Directory.CreateDirectory(currentDir);
                    }

                    CopyDirectory(file, desFolderDir);
                }
                else // 否则直接copy文件
                {
                    string srcFileName = file.Substring(file.LastIndexOf("\\") + 1);
                    srcFileName = desFolderDir + "\\" + srcFileName;
                    if (!Directory.Exists(desFolderDir))
                    {
                        Directory.CreateDirectory(desFolderDir);
                    }

                    File.Copy(file, srcFileName, true);
                }
            }
        }


        /// <summary>
        /// 删除临时文件
        /// </summary>
        private void DelTempFile(string name)
        {
            FileInfo file = new FileInfo(tempPath + name);
            file.Delete();
            return;
        }

        /// <summary>
        /// 更新失败的情况下，回滚当前更新
        /// </summary>
        private void Restore()
        {
            DelLocal();
            CopyDirectory(bakPath, ProgramDirectoryName);
            return;
        }

        /// <summary>
        /// 删除本地文件夹的文件
        /// </summary>
        private void DelLocal()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(ProgramDirectoryName);
                foreach (var item in di.GetFiles())
                {
                    //判断文件是否是指定忽略的文件
                    if (!RemoteVerInfo.IgnoreFile.Contains(item.Name))
                    {
                        File.Delete(item.FullName);
                    }
                }

                foreach (var item in di.GetDirectories())
                {
                    if (item.Name != mainDirectoryName)
                    {
                        item.Delete(true);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}