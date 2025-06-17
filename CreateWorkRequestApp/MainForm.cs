using CommonLibrary;
using CommonInterfaces;
using System;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Data;

namespace CreateWorkRequestApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // Initialize statusForm and statusLogger in the constructor
            
            // Attach validation to input events
            txtWRName.TextChanged += ValidateForm;
            cmbWBSCode.TextChanged += ValidateForm;
            cmbCostCentre.TextChanged += ValidateForm;
            cmbGLCode.SelectedIndexChanged += ValidateForm;
            radTypeBAU.CheckedChanged += ValidateForm;
            radTypeCampaign.CheckedChanged += ValidateForm;
            chkBrandMPL.CheckedChanged += ValidateForm;
            chkBrandAHM.CheckedChanged += ValidateForm;
            chkBrandMPLOSHC.CheckedChanged += ValidateForm;
            chkBrandAHMOSHC.CheckedChanged += ValidateForm;

            btnGo.Click += BtnGo_Click;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Populate the GL Code dropdown
            string filePath = @"\\mplfiler\Groups\Operational Delivery\Fulfilment\1. Team\Financials\Cost centres GL codes and WBS info for fulfilment\Fulfilment GL Coding.xlsx";

            // Define mapping: Excel Column Names → Object Properties
            var dataDictionary = new Dictionary<string, string>
            {
                { "C", "CostCentre" },
                { "D", "CostCentreName" },
                { "E", "GLCode" },
                { "F", "GLName" },
                { "G", "WBSCode" },
                { "H", "WBSName" },
            };

            foreach (Control control in gpBrand.Controls)
            {
                if (control is CheckBox checkBox)
                {
                    checkBox.CheckedChanged += CheckBox_CheckedChanged;
                }
            }

            // Read in the cost centre spreadsheet
            var reader = new ExcelReader<FulfilmentGLCoding>(filePath, dataDictionary, 3);
            List<FulfilmentGLCoding> records = [];
            try
            {
                records = reader.ReadRecords();

            }
            catch (Exception ex) {
                MessageBox.Show( $"Error!\n\nThe data file does not exist or is in the wrong format.\n\n" + "Error Message:\n " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            

            foreach (var record in records)
            {
                if (string.IsNullOrEmpty(record.GLCode) && string.IsNullOrEmpty(record.WBSCode)) continue;
                // Only add non-null and unique GL Codes
                if (!string.IsNullOrEmpty(record.GLCode))
                {
                    string GL = (string.IsNullOrEmpty(record.GLName)) ? record.GLCode : $"{record.GLCode} -- {record.GLName}";
                    if (!cmbGLCode.Items.Contains(GL)) cmbGLCode.Items.Add(GL);
                }
                if (!string.IsNullOrEmpty(record.WBSCode))
                {
                    string WBS = (string.IsNullOrEmpty(record.WBSName)) ? record.WBSCode : $"{record.WBSCode} -- {record.WBSName}";
                    if (!cmbWBSCode.Items.Contains(WBS)) cmbWBSCode.Items.Add(WBS);
                }
                if (!string.IsNullOrEmpty(record.CostCentre))
                {
                    string CC = (string.IsNullOrEmpty(record.CostCentreName)) ? record.CostCentre : $"{record.CostCentre} -- {record.CostCentreName}";
                    if (!cmbCostCentre.Items.Contains(CC)) cmbCostCentre.Items.Add(CC);
                }
            }
            cmbGLCode.SelectedIndex = 0; // No selection initially
            cmbWBSCode.SelectedIndex = 0; // No selection initially 
            cmbCostCentre.SelectedIndex = 0;

            radTypeBAU.Checked = true; // Default selection
            chkBrandMPL.Checked = true; // Default selection
            
            btnGo.Enabled = false; // Disable button at the start
        }

        private void CheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkBrandMPL.Checked || chkBrandMPLOSHC.Checked || chkBrandAHMOSHC.Checked)
            {
                chkBrandAHM.Checked = false;
                chkBrandAHM.Enabled = false;
            }
            else
            {
                chkBrandAHM.Enabled = true;
            }

            if (chkBrandAHM.Checked)
            {
                chkBrandMPL.Checked = false;
                chkBrandMPLOSHC.Checked = false;
                chkBrandAHMOSHC.Checked = false;
                chkBrandMPLOSHC.Enabled = false;
                chkBrandMPL.Enabled = false;
                chkBrandAHMOSHC.Enabled = false;
            }
            else
            {
                chkBrandMPLOSHC.Enabled = true;
                chkBrandMPL.Enabled = true;
                chkBrandAHMOSHC.Enabled = true;
            }
        }

        private void ValidateForm(object sender, EventArgs e)
        {

            bool istxtWRName = !string.IsNullOrWhiteSpace(txtWRName.Text);
            bool isWBSSelected = !string.IsNullOrWhiteSpace(cmbWBSCode.Text);
            bool isCostCentreSelected = !string.IsNullOrWhiteSpace(cmbCostCentre.Text);
            bool isGLSelected = !string.IsNullOrWhiteSpace(cmbGLCode.Text);
            bool isTypeSelected = radTypeBAU.Checked || radTypeCampaign.Checked;
            bool isBrandSelected = chkBrandMPL.Checked || chkBrandAHM.Checked || chkBrandMPLOSHC.Checked || chkBrandAHMOSHC.Checked;

            if (radTypeBAU.Checked) isCostCentreSelected = true;
            if (radTypeCampaign.Checked) isWBSSelected = true;

            // Enable button only if all fields are valid
            btnGo.Enabled = istxtWRName && isWBSSelected && isCostCentreSelected && isGLSelected && isTypeSelected && isBrandSelected;
        }

        private void gbType_Changed(object sender, EventArgs e)
        {
            if (radTypeBAU.Checked)
            {
                cmbCostCentre.Text = "";
            }
            if (radTypeCampaign.Checked)
            {
                cmbCostCentre.SelectedIndex = 0;
            }

            cmbCostCentre.Enabled = radTypeCampaign.Checked;
        }

        private string GetComboBoxText(ComboBox comboBox)
        {
            // Check if we need to marshal the call to the UI thread
            if (comboBox.InvokeRequired)
            {
                // If we're not on the UI thread, invoke the method on the UI thread
                return (string)comboBox.Invoke(new Func<string>(() => comboBox.Text));
            }
            else
            {
                // If we are already on the UI thread, directly get the text
                return comboBox.Text;
            }
        }

        private string GetTextBoxText(TextBox textBox)
        {
            // Check if we need to marshal the call to the UI thread
            if (textBox.InvokeRequired)
            {
                // If we're not on the UI thread, invoke the method on the UI thread
                return (string)textBox.Invoke(new Func<string>(() => textBox.Text));
            }
            else
            {
                // If we are already on the UI thread, directly get the text
                return textBox.Text;
            }
        }

        private async void BtnGo_Click(object sender, EventArgs e)
        {
            btnGo.Enabled = false; // Disable button to prevent multiple clicks

            txtStatus.Clear();

            UpdateStatus("Processing...");

            await Task.Run(() => ProcessWR());

            UpdateStatus("Complete...");

            btnGo.Enabled = true;
        }

        private void ProcessWR()
        {
            try
            {
                
                string sourceDirectory = System.IO.Directory.GetCurrentDirectory();
                string excelFileDirectory = @"\\mplfiler\Groups\Operational Delivery\Fulfilment\1. Team\Reporting";
                string? MPLorAHMWRprefix = "O";

                if (chkBrandAHM.Checked)
                {
                    MPLorAHMWRprefix = "A";
                    sourceDirectory = @"\\mplfiler\Groups\Operational Delivery\Fulfilment\2. ahm\1. ahm Work Requests";
                }
                else
                {
                    sourceDirectory = @"\\mplfiler\Groups\Operational Delivery\Fulfilment\3. MPL\1. MPL Work Requests";
                    MPLorAHMWRprefix = "O";
                }

                // Get the next WR number by polling the WR directory.
                List<int> WRNumbers = [];
                var directories = Directory.GetDirectories(sourceDirectory);

                UpdateStatus("Getting latest WR #.");

                foreach (var dir in directories)
                {
                    var match = Regex.Match(dir, @".*?\\(.)WR(\d+)?");
                    if (int.TryParse(match.Groups[2].Value, out int WRNumber))
                    {
                        MPLorAHMWRprefix = match.Groups[1].Value;
                        if (WRNumber < 6000)
                        {
                            WRNumbers.Add(WRNumber);
                        }
                    }
                }

                // Get highest WR Number in the folder and add 1
                int currentWR = WRNumbers.Max() + 1;
                string nextWR = currentWR.ToString();
                string newWR = nextWR.PadLeft(6, '0');
                
                string targetDirectory = sourceDirectory + @"\" + MPLorAHMWRprefix + "WR" + newWR + " - " + txtWRName.Text;

                // Copy WR Templates and rename office files
                UpdateStatus("Copying WR Template folder to new WR: " + MPLorAHMWRprefix + "WR" + newWR + " - " + txtWRName.Text);
                DirectoryInfo dirInfoSourceDirectory = new DirectoryInfo(sourceDirectory + @"\WR Templates");
                DirectoryInfo dirInfoTargetDirectory = new DirectoryInfo(targetDirectory);
                Utilities.CopyAll(dirInfoSourceDirectory, dirInfoTargetDirectory);

                System.Diagnostics.Debug.WriteLine("\n:");
                var docxFiles = Directory.GetFiles(targetDirectory, "WR*.*x", SearchOption.AllDirectories);

                UpdateStatus("Renaming Word and Excel Files");
                foreach (var renameFile in docxFiles)
                {
                    FileInfo fi = new FileInfo(renameFile);
                    string newFileName = Path.Combine(fi.DirectoryName, $"WR{newWR} - {txtWRName.Text}{fi.Extension}");
                    fi.MoveTo(newFileName);
                    System.Diagnostics.Debug.WriteLine("New File: " + newFileName);
                }

                // Helper method to clean code splitting logic
                string CleanCode(string code) => code.Contains("--") ? code.Split("--")[0].Trim() : code.Trim();

                // GL and WBS codes
                string GLCode = CleanCode(GetComboBoxText(cmbGLCode));
                string CGLCode = GLCode;  // Initially the same
                string WBSCode = CleanCode(GetComboBoxText(cmbWBSCode));
                string CWBSCode = WBSCode;  // Initially the same
                string costCentre = GetComboBoxText(cmbCostCentre);
                string WRName = GetTextBoxText(txtWRName);
                string lodgement = MPLorAHMWRprefix + "WR" + newWR + "_";

                // Update for BAU type (can be simplified further)
                if (radTypeBAU.Checked)
                {
                    lodgement += string.IsNullOrEmpty(WBSCode) ? GLCode + "_" : GLCode + "/" + WBSCode + "_";
                    CGLCode = CWBSCode = "";  // No need for these in BAU
                }
                else
                {
                    lodgement += string.IsNullOrEmpty(CWBSCode) ? CGLCode + "_" : CGLCode + "/" + CWBSCode + "_";
                    WBSCode = GLCode = "";  // No need for these in non-BAU
                }

                // Update the WR document with details
                docxFiles = Directory.GetFiles(targetDirectory, "WR*.docx", SearchOption.AllDirectories);

                UpdateStatus("Automatically filling word doc with WR details.");
                foreach (var renameFile in docxFiles)
                {
                    
                    System.Diagnostics.Debug.WriteLine("\nUpdating details in: " + renameFile);
                    using (var wordUtil = new WordUtilities(renameFile))
                    {
                        
                        Dictionary<string, string> replacements = new Dictionary<string, string>
                        {
                            { "[WR]", MPLorAHMWRprefix + "WR" + newWR },
                            { "[WRN]", WRName },
                            { "[BGLC]", GLCode },
                            { "[BWBS]", WBSCode },
                            { "[CGLC]", CGLCode },
                            { "[CC]", costCentre },
                            { "[CWBS]", CWBSCode },
                            { "[LODGE]", lodgement },
                        };
                        wordUtil.ReplaceText(replacements);
                        foreach (CheckBox checkBox in gpBrand.Controls.OfType<CheckBox>())
                        {
                            // Use InvokeRequired to ensure thread-safe access
                            if (checkBox.InvokeRequired)
                            {
                                checkBox.Invoke(new Action(() =>
                                {
                                    string brand = checkBox.Text.Contains("MPL") ? "MPL" : "AHM";
                                    brand += checkBox.Text.Contains("OSHC") ? "OSHC" : "";

                                    if (checkBox.Checked)
                                    {
                                        wordUtil.CheckBox(brand, true);
                                    }
                                }));
                            }
                            else
                            {
                                string brand = checkBox.Text.Contains("MPL") ? "MPL" : "AHM";
                                brand += checkBox.Text.Contains("OSHC") ? "OSHC" : "";

                                if (checkBox.Checked)
                                {
                                    wordUtil.CheckBox(brand, true);
                                }
                            }
                        }
                        wordUtil.CloseandSaveWordDocument();
                    }
                }

                // Write out to Excel
                UpdateStatus("Writing details to Excel file.");
                string filePath = Path.Combine(excelFileDirectory, "WorkRequests.xlsx");
                string[] headerRow = { "Brand", "WR Number", "WR Name", "GL Code", "WBS Code", "Cost Centre", "Date Created" };
                WRExcelReport writer = new WRExcelReport(filePath, headerRow);

                string brandCode = chkBrandMPL.Checked ? "MPL" : "AHM";

                string[] rowData = (radTypeBAU.Checked)
                    ? [brandCode, MPLorAHMWRprefix + "WR" + newWR, WRName, GLCode, WBSCode, costCentre, DateTime.Now.ToString()]
                    : [brandCode, MPLorAHMWRprefix + "WR" + newWR, WRName, CGLCode, CWBSCode, costCentre, DateTime.Now.ToString()];

                writer.WriteRow(rowData, 1, targetDirectory);

                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
       
        private void UpdateStatus (string message)
        {
            if (txtStatus.InvokeRequired)
            {
                txtStatus.Invoke(new Action(() => txtStatus.AppendText(message + "\n")));
            }
            else
            {
                txtStatus.AppendText(message + "\n");
            }
        }
    }
}
