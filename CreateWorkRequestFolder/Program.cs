using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Principal;
using CommonLibrary;

namespace ConsoleApp1
{

    class Program
    {
        public static void Main()
        {
            // Test Folder
            //string sourceDirectory = @"C:\Users\608138\Development Github\Perl\WorkRequests\testFolder";
            
            // User name
            string username = Environment.UserName;
            Console.WriteLine($"Username: {username}");
            
            string domainUsername = WindowsIdentity.GetCurrent().Name;
            Console.WriteLine($"Logged in user: {domainUsername}");

            //Test Folder
            string sourceDirectory = System.IO.Directory.GetCurrentDirectory();
            string excelFileDirectory = @" \Reporting";
            
            List<int> WRNumbers = [];
            
            Console.WriteLine("Automated Work request folder script.");
            
            // Work out if this is an MPL or AHM WR
            string? MPLorAHMWRprefix = "O";
            string? MPLorAHM = "";
            string? confirmationCheck = "";
            
            int currentWR = 0;
            string newWR = "";
            
            // Information to collect
            string? WRName = "";
            string? brand = "";
            string? ProjectOrBAU = "";
            string? GLCode = "";
            string? WBSCode = "";
            string? CGLCode = "";
            string? CWBSCode = "";
            string? costCentre = "";
            string? lodgement = "";
            string brandCode = "MPL";
            
            while (string.IsNullOrEmpty(confirmationCheck) || confirmationCheck.ToString().Equals("n", StringComparison.CurrentCultureIgnoreCase)) {

                MPLorAHM = Utilities.ValidateInput("Is this WR for MPL or AHM [M|A]?", "M", ["M", "A"]);
                if (MPLorAHM.ToUpper() == "A") 
                {
                    sourceDirectory = @"\\mplfiler\Groups\Operational Delivery\Fulfilment\2. ahm\1. ahm Work Requests";
                    MPLorAHMWRprefix = "A";
                }
                else {
                    sourceDirectory = @"\\mplfiler\Groups\Operational Delivery\Fulfilment\3. MPL\1. MPL Work Requests";
                    MPLorAHMWRprefix = "O";
                }
                
                // Get All directories in the WR folder
                // Grab the WR # from any folder that has OWR in it
                var directories = Directory.GetDirectories(sourceDirectory);
                
                foreach (var dir in directories) {
                var match = Regex.Match(dir, @".*?\\(.)WR(\d+)?");
                    if (int.TryParse(match.Groups[2].Value, out int WRNumber)){
                        MPLorAHMWRprefix = match.Groups[1].Value;
                        if (WRNumber < 6000) {
                        WRNumbers.Add(WRNumber);
                        }
                    } 
                    else {
                        //Console.WriteLine("String: \""+ match.Groups[1].Value +"\" could not be parsed.");
                    }
                }

                // Get highest WR Number in the folder and add 1
                currentWR = WRNumbers.Max() + 1;
                string nextWR = currentWR.ToString(); 
                newWR = nextWR.PadLeft(6, '0');
                
                brand = Utilities.ValidateInput("Please enter the Market Brand (MPL[M], AHM[A], MPLOSHC[MO], or AHMOSHC[AO])?", "M", ["M", "A", "MO", "AO"]);
                WRName = Utilities.ValidateInput("Please enter the name of the new WR?");
                ProjectOrBAU = Utilities.ValidateInput("Is this WR for BAU or Campaign[B|C]?", "B", ["B", "C"]);
                
                if (ProjectOrBAU.ToUpper() == "B") {
                    GLCode = Utilities.ValidateInput("Please enter the GL Code?(Default: 534500)", "534500");
                    WBSCode = Utilities.ValidateInput("Please enter the P-WBS Code?[101-006-36]", "101-006-36");
                } else {
                    CGLCode = Utilities.ValidateInput("Please enter the GL Code?", "534500");
                    costCentre = Utilities.ValidateInput("Please enter the Cost Centre?");
                    CWBSCode = Utilities.ValidateInput("Please enter the S-WBS Code (if applicable)?", "");
                }
                if (brand.ToUpper() == "A") {
                    brandCode = "AHM";
                }
                else if (brand.ToUpper() == "MO") {
                    brandCode = "MPLOSHC";
                }
                else if (brand.ToUpper() == "AO") {
                    brandCode = "AHMOSHC";
                }

                // Check if everything is ok
                string message = "\n\nPlease confirm the following details:";
                message += "\nMarket brand: "+brandCode;
                message += "\nWR Directory: "+MPLorAHMWRprefix+"WR" + newWR + " - " + WRName;
                lodgement = MPLorAHMWRprefix+"WR" + newWR + "_";
                if (ProjectOrBAU.ToUpper() == "B") {
                    message += "\nGL Code: "+GLCode;
                    message += "\nP-WBS Code: "+WBSCode;
                    lodgement += string.IsNullOrEmpty(WBSCode) ? GLCode + "_" : GLCode + "/" + WBSCode + "_";
                } else {
                    message += "\nGL Code: "+CGLCode;
                    message += "\nCost Centre: "+costCentre;
                    message += "\nS-WBS Code: "+CWBSCode;
                    lodgement += string.IsNullOrEmpty(CWBSCode) ? CGLCode + "_" : CGLCode + "/" + CWBSCode + "_";
                }
                lodgement += WRName;
                message += "\nLodgement Reference Code: "+lodgement;
                message += "\n\nIs this ok? (Y|N)";
                
                confirmationCheck = Utilities.ValidateInput(message, "Y", ["Y", "N"]);
            }
            
            // Copy WR Templates and then rename any office files with the WR Name
            DirectoryInfo dirInfoSourceDirectory = new DirectoryInfo(sourceDirectory + @"\WR Templates");
            DirectoryInfo dirInfoTargetDirectory = new DirectoryInfo(sourceDirectory + @"\"+MPLorAHMWRprefix+"WR" + newWR + " - " + WRName);
            Utilities.CopyAll(dirInfoSourceDirectory, dirInfoTargetDirectory);
            Console.WriteLine("\nChanging File names:");
            
            var docxFiles = Directory.GetFiles(sourceDirectory + @"\"+MPLorAHMWRprefix+"WR" + newWR + " - " + WRName, "WR*.*x", SearchOption.AllDirectories);

            foreach (var renameFile in docxFiles) {
                System.IO.FileInfo fi = new System.IO.FileInfo(renameFile);
                string newFileName = fi.DirectoryName + @"\WR" + newWR + " - " + WRName + fi.Extension;
                fi.MoveTo(newFileName);
                Console.WriteLine("New File: " + newFileName);
            }

            

            // Update the WR document with the details entered above, ie. WR number, Name, GL Code, WBS Code, etc...
            docxFiles = Directory.GetFiles(sourceDirectory + @"\"+MPLorAHMWRprefix+"WR" + newWR + " - " + WRName, "WR*.docx", SearchOption.AllDirectories);
            foreach (var renameFile in docxFiles) {
                Console.WriteLine("\nUpdating details in: "+renameFile);    
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
                    wordUtil.CheckBox(brandCode, true);
                    wordUtil.CloseandSaveWordDocument();
                }
            }
            
            // Write out the required information to the global WR file
            // Create an instance of the ExcelWriter class
            Console.WriteLine("\nWriting details to Excel file.");
            string filePath = @$"{excelFileDirectory}\WorkRequests.xlsx";
            string[] headerRow = { "Brand", "WR Number", "WR Name", "GL Code", "WBS Code", "Cost Centre", "Date Created" };
            WRExcelReport writer = new WRExcelReport(filePath, headerRow);
            
            if (ProjectOrBAU.ToUpper() == "B") {
                string[] rowData = { brandCode, MPLorAHMWRprefix+"WR" + newWR, WRName, GLCode, "P-"+WBSCode, "", DateTime.Now.ToString() };
                writer.WriteRow(rowData, 1, sourceDirectory + @"\"+MPLorAHMWRprefix+"WR" + newWR + " - " + WRName);
            } else {
                string[] rowData = [brandCode, MPLorAHMWRprefix+"WR" + newWR, WRName, CGLCode, "S-"+CWBSCode, costCentre, DateTime.Now.ToString()];
                writer.WriteRow(rowData, 1, sourceDirectory + @"\"+MPLorAHMWRprefix+"WR" + newWR + " - " + WRName);
            }
            
            //End program
            Console.WriteLine("\nPress any key...");
            Console.ReadLine();
        }
        
    }
}