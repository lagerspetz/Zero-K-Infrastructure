﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlasmaShared;
using ZeroKLobby.Controls;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
    public partial class StartMatchMakerDialog : ZklBaseForm
    {
        
        public StartMatchMakerDialog()
        {
            InitializeComponent();
            ZklBaseControl.Init(flowLayoutPanel1);
            flowLayoutPanel1.BackColor = Color.Transparent;
            

            foreach (var qt in Program.TasClient.PossibleQueues)
            {
                var cb = new CheckBox();
                cb.Text = qt.Name;
                Program.ToolTip.SetText(cb, qt.Description);
                cb.Checked = Program.Conf.LastMatchMakerQueues?.Contains(qt.Name) == true;
                ZklBaseControl.Init(cb);
                cb.Font = Config.GeneralFontBig;
                cb.Tag = qt;
                flowLayoutPanel1.Controls.Add(cb);
            }

        }



        void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            var queues = flowLayoutPanel1.Controls.OfType<CheckBox>().Where(x => x.Checked).Select(x => x.Text).ToList();
            Program.TasClient.MatchMakerQueueRequest(queues);
            Program.Conf.LastMatchMakerQueues = queues;
            Program.SaveConfig();
            Close();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var page = Program.MainWindow.navigationControl.CurrentNavigatable as Control;
            if (page?.BackgroundImage != null) this.RenderControlBgImage(page, e);
            else e.Graphics.Clear(Config.BgColor);
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.Shraka);
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}