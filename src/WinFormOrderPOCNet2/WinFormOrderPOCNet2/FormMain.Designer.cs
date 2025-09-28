namespace WinFormOrderPOCNet2
{
	partial class FormMain
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
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.BtnTop = new System.Windows.Forms.Button();
			this.BtnUp = new System.Windows.Forms.Button();
			this.BtnDown = new System.Windows.Forms.Button();
			this.BtnBottom = new System.Windows.Forms.Button();
			this.BtnSave = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// listBox1
			// 
			this.listBox1.FormattingEnabled = true;
			this.listBox1.ItemHeight = 20;
			this.listBox1.Location = new System.Drawing.Point(12, 12);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(450, 284);
			this.listBox1.TabIndex = 0;
			// 
			// BtnTop
			// 
			this.BtnTop.Location = new System.Drawing.Point(468, 12);
			this.BtnTop.Name = "BtnTop";
			this.BtnTop.Size = new System.Drawing.Size(75, 50);
			this.BtnTop.TabIndex = 1;
			this.BtnTop.Text = "Top";
			this.BtnTop.UseVisualStyleBackColor = true;
			this.BtnTop.Click += new System.EventHandler(this.BtnTop_Click);
			// 
			// BtnUp
			// 
			this.BtnUp.Location = new System.Drawing.Point(468, 68);
			this.BtnUp.Name = "BtnUp";
			this.BtnUp.Size = new System.Drawing.Size(75, 50);
			this.BtnUp.TabIndex = 2;
			this.BtnUp.Text = "Up";
			this.BtnUp.UseVisualStyleBackColor = true;
			this.BtnUp.Click += new System.EventHandler(this.BtnUp_Click);
			// 
			// BtnDown
			// 
			this.BtnDown.Location = new System.Drawing.Point(468, 124);
			this.BtnDown.Name = "BtnDown";
			this.BtnDown.Size = new System.Drawing.Size(75, 50);
			this.BtnDown.TabIndex = 3;
			this.BtnDown.Text = "Down";
			this.BtnDown.UseVisualStyleBackColor = true;
			this.BtnDown.Click += new System.EventHandler(this.BtnDown_Click);
			// 
			// BtnBottom
			// 
			this.BtnBottom.Location = new System.Drawing.Point(468, 180);
			this.BtnBottom.Name = "BtnBottom";
			this.BtnBottom.Size = new System.Drawing.Size(75, 50);
			this.BtnBottom.TabIndex = 4;
			this.BtnBottom.Text = "Bottom";
			this.BtnBottom.UseVisualStyleBackColor = true;
			this.BtnBottom.Click += new System.EventHandler(this.BtnBottom_Click);
			// 
			// BtnSave
			// 
			this.BtnSave.Location = new System.Drawing.Point(468, 246);
			this.BtnSave.Name = "BtnSave";
			this.BtnSave.Size = new System.Drawing.Size(213, 50);
			this.BtnSave.TabIndex = 5;
			this.BtnSave.Text = "Save";
			this.BtnSave.UseVisualStyleBackColor = true;
			this.BtnSave.Click += new System.EventHandler(this.BtnSave_Click);
			// 
			// FormMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.BtnSave);
			this.Controls.Add(this.BtnBottom);
			this.Controls.Add(this.BtnDown);
			this.Controls.Add(this.BtnUp);
			this.Controls.Add(this.BtnTop);
			this.Controls.Add(this.listBox1);
			this.Name = "FormMain";
			this.Text = "Form1";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.Button BtnTop;
		private System.Windows.Forms.Button BtnUp;
		private System.Windows.Forms.Button BtnDown;
		private System.Windows.Forms.Button BtnBottom;
		private System.Windows.Forms.Button BtnSave;
	}
}

