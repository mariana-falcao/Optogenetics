namespace MyCustomCameraOverlay
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnToggleOverlay = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabelImageSize = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelFrameRate = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnCameraSettings = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(799, 424);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // btnToggleOverlay
            // 
            this.btnToggleOverlay.Location = new System.Drawing.Point(12, 38);
            this.btnToggleOverlay.Name = "btnToggleOverlay";
            this.btnToggleOverlay.Size = new System.Drawing.Size(92, 23);
            this.btnToggleOverlay.TabIndex = 1;
            this.btnToggleOverlay.Text = "Show Overlay";
            this.btnToggleOverlay.UseVisualStyleBackColor = true;
            this.btnToggleOverlay.Click += new System.EventHandler(this.btnToggleOverlay_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabelImageSize,
            this.toolStripStatusLabelFrameRate});
            this.statusStrip1.Location = new System.Drawing.Point(0, 428);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(800, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabelImageSize
            // 
            this.toolStripStatusLabelImageSize.Name = "toolStripStatusLabelImageSize";
            this.toolStripStatusLabelImageSize.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabelImageSize.Text = "toolStripStatusLabel1";
            // 
            // toolStripStatusLabelFrameRate
            // 
            this.toolStripStatusLabelFrameRate.Name = "toolStripStatusLabelFrameRate";
            this.toolStripStatusLabelFrameRate.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabelFrameRate.Text = "toolStripStatusLabel2";
            // 
            // btnCameraSettings
            // 
            this.btnCameraSettings.Location = new System.Drawing.Point(12, 9);
            this.btnCameraSettings.Name = "btnCameraSettings";
            this.btnCameraSettings.Size = new System.Drawing.Size(92, 23);
            this.btnCameraSettings.TabIndex = 3;
            this.btnCameraSettings.Text = "Camera Settings";
            this.btnCameraSettings.UseVisualStyleBackColor = true;
            this.btnCameraSettings.Click += new System.EventHandler(this.btnCameraSettings_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnCameraSettings);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.btnToggleOverlay);
            this.Controls.Add(this.pictureBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Click += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnToggleOverlay;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelImageSize;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelFrameRate;
        private System.Windows.Forms.Button btnCameraSettings;
        private System.Windows.Forms.ToolStrip toolStrip1;
    }
}

