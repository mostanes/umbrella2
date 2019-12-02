namespace Umbrella2.Visualizer.Winforms
{
	public partial class PropertyViewer
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			//
			// RichTextBox1
			//
			this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Right;
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.ReadOnly = true;
			this.richTextBox1.Size = new System.Drawing.Size(400, 10);
			//
			// TreeView1
			//
			this.treeView1.Location = new System.Drawing.Point(0, 0);
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView1.Name = "treeView1";
			this.treeView1.Size = new System.Drawing.Size(876, 481);
			this.treeView1.TabIndex = 0;
			this.treeView1.BeforeExpand += TreeView1_BeforeExpand;
			this.treeView1.AfterSelect += TreeView1_AfterSelect;
			//
			// PropertyViewer
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1076, 481);
			this.Controls.Add(this.treeView1);
			this.Controls.Add(this.richTextBox1);
			this.KeyPreview = true;
			this.Name = "PropertyViewer";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TreeView treeView1;
		private System.Windows.Forms.RichTextBox richTextBox1;
	}
}
