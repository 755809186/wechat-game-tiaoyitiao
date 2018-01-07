namespace TiaoYiTiao
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnJump = new System.Windows.Forms.Button();
            this.btnHelper = new System.Windows.Forms.Button();
            this.chkAutoJump = new System.Windows.Forms.CheckBox();
            this.chkRecognize = new System.Windows.Forms.CheckBox();
            this.lbMsg = new System.Windows.Forms.Label();
            this.nJumpCount = new System.Windows.Forms.NumericUpDown();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nJumpCount)).BeginInit();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 661);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(369, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(88, 17);
            this.toolStripStatusLabel1.Text = "正在检查设备...";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(43, 17);
            this.toolStripStatusLabel2.Text = "位置：";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(369, 661);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.nJumpCount);
            this.panel1.Controls.Add(this.btnJump);
            this.panel1.Controls.Add(this.btnHelper);
            this.panel1.Controls.Add(this.chkAutoJump);
            this.panel1.Controls.Add(this.chkRecognize);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 635);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(369, 26);
            this.panel1.TabIndex = 3;
            // 
            // btnJump
            // 
            this.btnJump.Enabled = false;
            this.btnJump.Location = new System.Drawing.Point(221, 2);
            this.btnJump.Name = "btnJump";
            this.btnJump.Size = new System.Drawing.Size(27, 23);
            this.btnJump.TabIndex = 3;
            this.btnJump.Text = "跳";
            this.btnJump.UseVisualStyleBackColor = true;
            this.btnJump.Click += new System.EventHandler(this.btnJump_Click);
            // 
            // btnHelper
            // 
            this.btnHelper.Location = new System.Drawing.Point(337, 3);
            this.btnHelper.Name = "btnHelper";
            this.btnHelper.Size = new System.Drawing.Size(27, 20);
            this.btnHelper.TabIndex = 2;
            this.btnHelper.Text = "?";
            this.btnHelper.UseVisualStyleBackColor = true;
            this.btnHelper.Click += new System.EventHandler(this.btnHelper_Click);
            // 
            // chkAutoJump
            // 
            this.chkAutoJump.AutoSize = true;
            this.chkAutoJump.Enabled = false;
            this.chkAutoJump.Location = new System.Drawing.Point(90, 5);
            this.chkAutoJump.Name = "chkAutoJump";
            this.chkAutoJump.Size = new System.Drawing.Size(60, 16);
            this.chkAutoJump.TabIndex = 1;
            this.chkAutoJump.Text = "自动跳";
            this.chkAutoJump.UseVisualStyleBackColor = true;
            this.chkAutoJump.CheckedChanged += new System.EventHandler(this.chkJump_CheckedChanged);
            // 
            // chkRecognize
            // 
            this.chkRecognize.AutoSize = true;
            this.chkRecognize.Enabled = false;
            this.chkRecognize.Location = new System.Drawing.Point(12, 5);
            this.chkRecognize.Name = "chkRecognize";
            this.chkRecognize.Size = new System.Drawing.Size(72, 16);
            this.chkRecognize.TabIndex = 0;
            this.chkRecognize.Text = "自动识别";
            this.chkRecognize.UseVisualStyleBackColor = true;
            this.chkRecognize.CheckedChanged += new System.EventHandler(this.chkRecognize_CheckedChanged);
            // 
            // lbMsg
            // 
            this.lbMsg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbMsg.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbMsg.Location = new System.Drawing.Point(27, 67);
            this.lbMsg.Name = "lbMsg";
            this.lbMsg.Padding = new System.Windows.Forms.Padding(5);
            this.lbMsg.Size = new System.Drawing.Size(94, 80);
            this.lbMsg.TabIndex = 4;
            this.lbMsg.Text = "help";
            this.lbMsg.Visible = false;
            this.lbMsg.DoubleClick += new System.EventHandler(this.lbMsg_DoubleClick);
            // 
            // nJumpCount
            // 
            this.nJumpCount.Location = new System.Drawing.Point(156, 3);
            this.nJumpCount.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nJumpCount.Name = "nJumpCount";
            this.nJumpCount.Size = new System.Drawing.Size(43, 21);
            this.nJumpCount.TabIndex = 4;
            this.nJumpCount.ValueChanged += new System.EventHandler(this.nJumpCount_ValueChanged);
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(369, 683);
            this.Controls.Add(this.lbMsg);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.statusStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "微信跳一跳辅助程序【Bao】";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nJumpCount)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox chkRecognize;
        private System.Windows.Forms.CheckBox chkAutoJump;
        private System.Windows.Forms.Button btnHelper;
        private System.Windows.Forms.Label lbMsg;
        private System.Windows.Forms.Button btnJump;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.NumericUpDown nJumpCount;
    }
}

