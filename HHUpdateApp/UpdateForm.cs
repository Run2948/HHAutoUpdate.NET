﻿using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HHUpdateApp
{
    public partial class UpdateForm : Form
    {
        private readonly UpdateWork work;

        public UpdateForm(UpdateWork work)
        {
            InitializeComponent();
            this.work = work;
        }

        private void UpdateForm_Load(object sender, EventArgs e)
        {
            if (work.SilentUpdate)
            {
                this.Hide(); //隐藏当前窗口

                BGWorkerUpdate.RunWorkerAsync();

                this.Size = Size.Empty;
            }

            btnWelcome.Visible = false;
        }

        private void btnWelcome_Click(object sender, EventArgs e)
        {
            work.AppStart();
            this.DialogResult = DialogResult.OK;
        }

        private void UpdateForm_Shown(object sender, EventArgs e)
        {
            if (!work.SilentUpdate)
            {
                BGWorkerUpdate.RunWorkerAsync();
            }
            
        }

        private void BGWorkerUpdate_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (!work.SilentUpdate)
                BGWorkerUpdate.ReportProgress(10, "正在升级...");
            work.Bak();

            Thread.Sleep(500);
            if (!work.SilentUpdate)
                BGWorkerUpdate.ReportProgress(50, "正在下载更新文件...");
            work.DownLoad();

            Thread.Sleep(500);
            if (!work.SilentUpdate)
                BGWorkerUpdate.ReportProgress(80, "正在配置更新...");
            work.Update();

            Thread.Sleep(500);
            if (!work.SilentUpdate)
                BGWorkerUpdate.ReportProgress(100);
        }

        private void BGWorkerUpdate_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            //修改进度条的显示。
            this.updateBar.Value = e.ProgressPercentage;

            //如果有更多的信息需要传递，可以使用 e.UserState 传递一个自定义的类型。
            //这是一个 object 类型的对象，您可以通过它传递任何类型。
            //我们在这里是接受的string 用于在UI界面上显示
            if (e.UserState != null)
            {
                this.lblMsg.Text = e.UserState.ToString();
            }
        }

        private void BGWorkerUpdate_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (work.SilentUpdate)
            {
                work.AppStart();
                this.DialogResult = DialogResult.OK;
                return;
            }

            if (e.Cancelled)
            {
                this.Close();
            }

            btnWelcome.Visible = true;

            //过程中的异常会被抓住，在这里可以进行处理。
            if (e.Error == null)
            {
                btnWelcome.Text = "欢迎使用";
                lblMsg.Text = "程序集版本:" + work.RemoteVerInfo.ReleaseVersion;
                lblAd.Visible = false;
                lblAd.Text = "用心让交管业务更便捷";
            }
            else
            {
                LogManger.Instance.Error("更新应用程序升级失败：", e.Error);
                lblMsg.Text = e.Error.Message;
                btnWelcome.Text = "确定";
            }
        }
    }
}