public class Samples
{
    public IDictionary<string, List<string>> Records {get; set;}
    public IDictionary<string, string> Headers {get; set;}
    public IDictionary<string, IDictionary<string, List<string>>> CapturedSamples {get; set;}
    public string SampleFileName {get; set;}
    public string SamplesListFileName {get; set;}

    public Samples(string samplesListFileName, string sampleFileName)
    {
        Records = new Dictionary<string, List<string>>();
        Headers = new Dictionary<string, string>();
        CapturedSamples = new Dictionary<string, IDictionary<string, List<string>>>();
        SampleFileName = sampleFileName;
        SamplesListFileName = samplesListFileName;
    }

    public void WriteSampleFile()
    {
        // Create sample file
        using (StreamWriter sampleFileHandle = new StreamWriter(SampleFileName, false)) {

            // check if there is any records first
            if ( Records.Count != 0)
            {
                // Write the header first
                foreach (var header in Headers) {
                    sampleFileHandle.WriteLine(header.Value);
                    // Only need one header. keeping them all just in case.
                    break;
                }
                
                // Write each record from each file.
                // --- Kept them in separate files, just in case for future proofing.
                foreach (var file in Records) {
                    System.Console.WriteLine(file.Key);
                    foreach (var record in file.Value) {
                        sampleFileHandle.WriteLine(record);
                        //System.Console.WriteLine(record);
                    }
                }
            }

            System.Console.WriteLine("File complete...");
        }

        // Create samples list
        using (StreamWriter samplesListFileHandle = new StreamWriter(SamplesListFileName, false)) {

            // check if there is any records first
            if ( Records.Count != 0)
            {
                samplesListFileHandle.WriteLine("Member ID's");
                samplesListFileHandle.WriteLine("");
                
                // WriteWrite out the member ID's of all the sample scenarios to a Samples List file.
                foreach (var MemberID in CapturedSamples) {
                    samplesListFileHandle.WriteLine("*************************");
                    samplesListFileHandle.WriteLine($"{MemberID.Key} :");
                    foreach (var fieldName in MemberID.Value) {
                        foreach (var fieldValue in fieldName.Value) {
                            samplesListFileHandle.WriteLine($"{fieldName.Key}: {fieldValue}");
                        }
                    }
                    samplesListFileHandle.WriteLine("");
                    
                }
                
            }

            System.Console.WriteLine("Sampling List File complete...");
        }
    }

}
