using System.Runtime.CompilerServices;

namespace SampleDataFileBuilder
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();
            lblSelectedPath = new Label();
            lblSelectedPath.Location = new Point(50, 50);
            lblSelectedPath.Size = new Size(400, 30);
            lblSelectedPath.Text = "Selected Path:";

            txtSelectedPath = new TextBox();
            txtSelectedPath.Location = new Point(50, 90);
            txtSelectedPath.Size = new Size(400, 30);

            btnSelectFolder = new Button();
            btnSelectFolder.Location = new Point(450, 90);
            btnSelectFolder.Size = new Size(50,50);
            btnSelectFolder.Image = Image.FromFile(@"C:\Users\608138\DevelopmentGithub\C#\SampleDataFileBuilder\Images\folderOpen.png");
            

            btnSelectFolder.Click += BtnSelectFolder_Click;

            Controls.Add(btnSelectFolder);
            Controls.Add(txtSelectedPath);
            Controls.Add(lblSelectedPath);


            ClientSize = new Size(651, 624);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion
        private TextBox txtSelectedPath;
        private Button btnSelectFolder;
        private Label lblSelectedPath;

    }
}
