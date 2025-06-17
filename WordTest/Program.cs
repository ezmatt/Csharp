using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CommonLibrary;

namespace ConsoleApp1
{
  class Program
    {
        public static void Main()
        {
            

            string brandCode = "MPL";
            // if (brand == "A") {
            brandCode = "AHM";
            // }
            // else if (brand == "MO") {
            //     brandCode = "MPLOSHC";
            // }
            // else if (brand == "AO") {
            //     brandCode = "AHMOSHC";
            // }
            string sourceDirectory = @"C:\Users\608138\DevelopmentGithub\C#\WordTest";
            // Update the WR document with the details entered above, ie. WR number, Name, GL Code, WBS Code, etc...
            DirectoryInfo dirInfoSourceDirectory = new DirectoryInfo(sourceDirectory + @"\WordFile");
            DirectoryInfo dirInfoTargetDirectory = new DirectoryInfo(sourceDirectory);
            CopyAll(dirInfoSourceDirectory, dirInfoTargetDirectory);
            string file = @$"{sourceDirectory}\Test.docx";
            Console.WriteLine("\nUpdating details in: "+file);    
            //WordUtilities wordDoc = new("OWR111111", "Work Request name", "534500", "S-101-006-36", "", brandCode, file );
            //wordDoc.UpdateWordDocument();
            
        }
        
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, diSourceSubDir.Name);
            }
        }

        public static string ValidateInput(string message, string defaultResponse = "", string[]? validResponses = null)
        {
            Console.WriteLine(message);
            string? response = Console.ReadLine();
            
            // If there is a default and the response is blank, then use the default.
            if (!string.IsNullOrEmpty(defaultResponse) && string.IsNullOrEmpty(response))
            {
                response = defaultResponse;
                Console.WriteLine("Default used:"+response+"\n");
            }
            
            // If there is a list of options, then check the response is in the list.
            while (validResponses != null && !validResponses.Contains(response.ToUpper()))
            {
                Console.WriteLine($"Invalid response: {response}. Please try again.");
                Console.WriteLine(message);
                response = Console.ReadLine();
                if (!string.IsNullOrEmpty(defaultResponse) && string.IsNullOrEmpty(response))
                {
                    response = defaultResponse;
                    Console.WriteLine("Default used:"+response+"\n");
                }
            }

            // If the response is still blank, then keep asking for a response.
            while (string.IsNullOrEmpty(response))
            {
                Console.WriteLine($"Invalid response: {response}. Please try again.");
                Console.WriteLine(message);
                response = Console.ReadLine();
            }
            //Console.WriteLine("Selection:"+response);
            return response;
        }
    }
}