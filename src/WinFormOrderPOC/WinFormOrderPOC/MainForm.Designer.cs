namespace WinFormOrderPOC
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button btnUp;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Button btnSave;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.btnUp = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(12, 12);
            this.listBox1.Size = new System.Drawing.Size(260, 199);
            this.listBox1.TabIndex = 0;
            // 
            // btnUp
            // 
            this.btnUp.Location = new System.Drawing.Point(290, 12);
            this.btnUp.Size = new System.Drawing.Size(75, 30);
            this.btnUp.Text = "↑";
            this.btnUp.UseVisualStyleBackColor = true;
            this.btnUp.Click += new System.EventHandler(this.BtnUp_Click);
            // 
            // btnDown
            // 
            this.btnDown.Location = new System.Drawing.Point(290, 48);
            this.btnDown.Size = new System.Drawing.Size(75, 30);
            this.btnDown.Text = "↓";
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.BtnDown_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(290, 181);
            this.btnSave.Size = new System.Drawing.Size(75, 30);
            this.btnSave.Text = "Sauvegarder";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(384, 231);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.btnUp);
            this.Controls.Add(this.btnDown);
            this.Controls.Add(this.btnSave);
            this.Text = "POC Réorganisation (SQLite)";
            this.ResumeLayout(false);
        }
    }
}
