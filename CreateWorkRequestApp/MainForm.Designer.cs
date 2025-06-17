namespace CreateWorkRequestApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

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
            label1 = new Label();
            txtWRName = new TextBox();
            label2 = new Label();
            cmbGLCode = new ComboBox();
            radTypeBAU = new RadioButton();
            radTypeCampaign = new RadioButton();
            label4 = new Label();
            gbType = new GroupBox();
            label3 = new Label();
            gpBrand = new GroupBox();
            chkBrandAHMOSHC = new CheckBox();
            chkBrandMPLOSHC = new CheckBox();
            chkBrandAHM = new CheckBox();
            chkBrandMPL = new CheckBox();
            btnGo = new Button();
            cmbWBSCode = new ComboBox();
            cmbCostCentre = new ComboBox();
            txtStatus = new RichTextBox();
            gbType.SuspendLayout();
            gpBrand.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(21, 9);
            label1.Name = "label1";
            label1.Size = new Size(63, 15);
            label1.TabIndex = 0;
            label1.Text = "WR Name:";
            // 
            // txtWRName
            // 
            txtWRName.Location = new Point(94, 6);
            txtWRName.Name = "txtWRName";
            txtWRName.Size = new Size(188, 23);
            txtWRName.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(29, 105);
            label2.Name = "label2";
            label2.Size = new Size(55, 15);
            label2.TabIndex = 2;
            label2.Text = "GL Code:";
            // 
            // cmbGLCode
            // 
            cmbGLCode.FormattingEnabled = true;
            cmbGLCode.Location = new Point(94, 102);
            cmbGLCode.Name = "cmbGLCode";
            cmbGLCode.Size = new Size(188, 23);
            cmbGLCode.TabIndex = 3;
            // 
            // radTypeBAU
            // 
            radTypeBAU.AutoSize = true;
            radTypeBAU.Location = new Point(6, 22);
            radTypeBAU.Name = "radTypeBAU";
            radTypeBAU.Size = new Size(48, 19);
            radTypeBAU.TabIndex = 5;
            radTypeBAU.Text = "BAU";
            radTypeBAU.UseVisualStyleBackColor = true;
            radTypeBAU.CheckedChanged += gbType_Changed;
            // 
            // radTypeCampaign
            // 
            radTypeCampaign.AutoSize = true;
            radTypeCampaign.Location = new Point(60, 22);
            radTypeCampaign.Name = "radTypeCampaign";
            radTypeCampaign.Size = new Size(80, 19);
            radTypeCampaign.TabIndex = 6;
            radTypeCampaign.Text = "Campaign";
            radTypeCampaign.UseVisualStyleBackColor = true;
            radTypeCampaign.CheckedChanged += gbType_Changed;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(21, 137);
            label4.Name = "label4";
            label4.Size = new Size(65, 15);
            label4.TabIndex = 7;
            label4.Text = "WBS Code:";
            // 
            // gbType
            // 
            gbType.Controls.Add(radTypeBAU);
            gbType.Controls.Add(radTypeCampaign);
            gbType.Location = new Point(94, 35);
            gbType.Name = "gbType";
            gbType.Size = new Size(141, 54);
            gbType.TabIndex = 9;
            gbType.TabStop = false;
            gbType.Text = "WR Type";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 167);
            label3.Name = "label3";
            label3.Size = new Size(72, 15);
            label3.TabIndex = 10;
            label3.Text = "Cost Centre:";
            // 
            // gpBrand
            // 
            gpBrand.Controls.Add(chkBrandAHMOSHC);
            gpBrand.Controls.Add(chkBrandMPLOSHC);
            gpBrand.Controls.Add(chkBrandAHM);
            gpBrand.Controls.Add(chkBrandMPL);
            gpBrand.Location = new Point(307, 9);
            gpBrand.Name = "gpBrand";
            gpBrand.Size = new Size(101, 129);
            gpBrand.TabIndex = 13;
            gpBrand.TabStop = false;
            gpBrand.Text = "Market Brand";
            // 
            // chkBrandAHMOSHC
            // 
            chkBrandAHMOSHC.AutoSize = true;
            chkBrandAHMOSHC.Location = new Point(6, 97);
            chkBrandAHMOSHC.Name = "chkBrandAHMOSHC";
            chkBrandAHMOSHC.Size = new Size(89, 19);
            chkBrandAHMOSHC.TabIndex = 3;
            chkBrandAHMOSHC.Text = "AHM OSHC";
            chkBrandAHMOSHC.UseVisualStyleBackColor = true;
            // 
            // chkBrandMPLOSHC
            // 
            chkBrandMPLOSHC.AutoSize = true;
            chkBrandMPLOSHC.Location = new Point(6, 72);
            chkBrandMPLOSHC.Name = "chkBrandMPLOSHC";
            chkBrandMPLOSHC.Size = new Size(85, 19);
            chkBrandMPLOSHC.TabIndex = 2;
            chkBrandMPLOSHC.Text = "MPL OSHC";
            chkBrandMPLOSHC.UseVisualStyleBackColor = true;
            // 
            // chkBrandAHM
            // 
            chkBrandAHM.AutoSize = true;
            chkBrandAHM.Location = new Point(6, 47);
            chkBrandAHM.Name = "chkBrandAHM";
            chkBrandAHM.Size = new Size(54, 19);
            chkBrandAHM.TabIndex = 1;
            chkBrandAHM.Text = "AHM";
            chkBrandAHM.UseVisualStyleBackColor = true;
            // 
            // chkBrandMPL
            // 
            chkBrandMPL.AutoSize = true;
            chkBrandMPL.Location = new Point(6, 22);
            chkBrandMPL.Name = "chkBrandMPL";
            chkBrandMPL.Size = new Size(50, 19);
            chkBrandMPL.TabIndex = 0;
            chkBrandMPL.Text = "MPL";
            chkBrandMPL.UseVisualStyleBackColor = true;
            // 
            // btnGo
            // 
            btnGo.Location = new Point(307, 164);
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(101, 23);
            btnGo.TabIndex = 14;
            btnGo.Text = "Create WR";
            btnGo.UseVisualStyleBackColor = true;
            // 
            // cmbWBSCode
            // 
            cmbWBSCode.FormattingEnabled = true;
            cmbWBSCode.Location = new Point(94, 134);
            cmbWBSCode.Name = "cmbWBSCode";
            cmbWBSCode.Size = new Size(188, 23);
            cmbWBSCode.TabIndex = 15;
            // 
            // cmbCostCentre
            // 
            cmbCostCentre.FormattingEnabled = true;
            cmbCostCentre.Location = new Point(94, 164);
            cmbCostCentre.Name = "cmbCostCentre";
            cmbCostCentre.Size = new Size(188, 23);
            cmbCostCentre.TabIndex = 16;
            // 
            // txtStatus
            // 
            txtStatus.Location = new Point(12, 193);
            txtStatus.Name = "txtStatus";
            txtStatus.ReadOnly = true;
            txtStatus.Size = new Size(396, 96);
            txtStatus.TabIndex = 17;
            txtStatus.Text = "";
            // 
            // MainForm
            // 
            ClientSize = new Size(423, 295);
            Controls.Add(txtStatus);
            Controls.Add(cmbCostCentre);
            Controls.Add(cmbWBSCode);
            Controls.Add(btnGo);
            Controls.Add(gpBrand);
            Controls.Add(label3);
            Controls.Add(gbType);
            Controls.Add(label4);
            Controls.Add(cmbGLCode);
            Controls.Add(label2);
            Controls.Add(txtWRName);
            Controls.Add(label1);
            Name = "MainForm";
            Load += MainForm_Load;
            gbType.ResumeLayout(false);
            gbType.PerformLayout();
            gpBrand.ResumeLayout(false);
            gpBrand.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
        private Label label1;
        private TextBox txtWRName;
        private Label label2;
        private ComboBox cmbGLCode;
        private RadioButton radTypeBAU;
        private RadioButton radTypeCampaign;
        private Label label4;
        private GroupBox gbType;
        private Label label3;
        private GroupBox gpBrand;
        private CheckBox chkBrandAHMOSHC;
        private CheckBox chkBrandMPLOSHC;
        private CheckBox chkBrandAHM;
        private CheckBox chkBrandMPL;
        private Button btnGo;
        private ComboBox cmbWBSCode;
        private ComboBox cmbCostCentre;
        private RichTextBox txtStatus;
    }
}
