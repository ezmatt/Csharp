namespace DataValidation
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
            cboDataFormats = new ComboBox();
            lblbDataFormats = new Label();
            folderBrowserDialog1 = new FolderBrowserDialog();
            btnBrowse = new Button();
            txtDataPath = new TextBox();
            lblDataFileLocation = new Label();
            rtbLog = new RichTextBox();
            btnValidate = new Button();
            SuspendLayout();
            // 
            // cboDataFormats
            // 
            cboDataFormats.FormattingEnabled = true;
            cboDataFormats.Location = new Point(114, 12);
            cboDataFormats.Name = "cboDataFormats";
            cboDataFormats.Size = new Size(276, 23);
            cboDataFormats.TabIndex = 1;
            // 
            // lblbDataFormats
            // 
            lblbDataFormats.AutoSize = true;
            lblbDataFormats.Location = new Point(28, 15);
            lblbDataFormats.Name = "lblbDataFormats";
            lblbDataFormats.Size = new Size(80, 15);
            lblbDataFormats.TabIndex = 2;
            lblbDataFormats.Text = "Data Formats:";
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(466, 44);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(47, 23);
            btnBrowse.TabIndex = 3;
            btnBrowse.Text = "Open";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // txtDataPath
            // 
            txtDataPath.Location = new Point(114, 45);
            txtDataPath.Name = "txtDataPath";
            txtDataPath.Size = new Size(346, 23);
            txtDataPath.TabIndex = 4;
            // 
            // lblDataFileLocation
            // 
            lblDataFileLocation.AutoSize = true;
            lblDataFileLocation.Location = new Point(22, 48);
            lblDataFileLocation.Name = "lblDataFileLocation";
            lblDataFileLocation.Size = new Size(86, 15);
            lblDataFileLocation.TabIndex = 5;
            lblDataFileLocation.Text = "Datafile Folder:";
            // 
            // rtbLog
            // 
            rtbLog.Location = new Point(12, 80);
            rtbLog.Name = "rtbLog";
            rtbLog.Size = new Size(448, 182);
            rtbLog.TabIndex = 6;
            rtbLog.Text = "";
            // 
            // btnValidate
            // 
            btnValidate.Enabled = false;
            btnValidate.Location = new Point(466, 239);
            btnValidate.Name = "btnValidate";
            btnValidate.Size = new Size(75, 23);
            btnValidate.TabIndex = 7;
            btnValidate.Text = "Validate";
            btnValidate.UseVisualStyleBackColor = true;
            btnValidate.Click += btnValidate_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(553, 274);
            Controls.Add(btnValidate);
            Controls.Add(rtbLog);
            Controls.Add(lblDataFileLocation);
            Controls.Add(txtDataPath);
            Controls.Add(btnBrowse);
            Controls.Add(lblbDataFormats);
            Controls.Add(cboDataFormats);
            Name = "Form1";
            Text = "Data Validation";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private void UpdateValidateButtonState()
        {
            btnValidate.Enabled = !string.IsNullOrWhiteSpace(txtDataPath.Text)
                                  && !string.IsNullOrWhiteSpace(cboDataFormats.Text);
        }

        #endregion
        private ComboBox cboDataFormats;
        private Label lblbDataFormats;
        private FolderBrowserDialog folderBrowserDialog1;
        private Button btnBrowse;
        private TextBox txtDataPath;
        private Label lblDataFileLocation;
        private RichTextBox rtbLog;
        private Button btnValidate;

        
    }
}
