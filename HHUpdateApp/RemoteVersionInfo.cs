using System;
using System.Collections.Generic;
using System.Text;

namespace HHUpdateApp
{
    /// <summary>
    /// 服务器上版本更新的json文件 实体
    /// </summary>
    public class RemoteVersionInfo
    {
        /// <summary>
        /// 更新后启动的应用程序名
        /// </summary>
        public string ApplicationStart { get; set; }
        /// <summary>
        /// 发布时间
        /// </summary>
        public string ReleaseDate { get; set; }
        /// <summary>
        /// 发布地址
        /// </summary>
        public string ReleaseUrl { get; set; }
        /// <summary>
        /// 发布版本号
        /// </summary>
        public string ReleaseVersion { get; set; }
        /// <summary>
        /// 更新方式：Cover表示覆盖原文件更新，NewInstall表示删除源文件全新安装
        /// </summary>
        public string UpdateMode { get; set; }
        /// <summary>
        /// 更新说明
        /// </summary>
        public string VersionDesc { get; set; }
        /// <summary>
        /// 忽略文件
        /// </summary>
        public string IgnoreFile { get; set; }
    }
}
