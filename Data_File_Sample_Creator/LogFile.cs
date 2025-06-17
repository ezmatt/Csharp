public class LogFile
{
    public string LogFileName {get; set;}
    public IDictionary<string, List<Log>> FileLogs {get; set;}

    public LogFile(string logFileName)
    {
        LogFileName = logFileName;
        FileLogs = new Dictionary<string, List<Log>>();
    }
    
    // Method to log validation messages to a file
    public void LogError(string fileName, string type, List<string> message, int fileLineNo)
    {
        //writer.WriteLine($"Line {fileLineNo}: {message}");
        var log = new Log
        {
            logType = type,
            message = message,
            lineNumber = fileLineNo,
        };

        if (FileLogs.TryGetValue(fileName, out List<Log> logs))
        {
            logs.Add(log);
        }
        else {
            FileLogs[fileName] = [log];
        }
        
        Console.WriteLine($"Line: {log.lineNumber}");
        Console.WriteLine("{0}", string.Join(" - ", log.message));

    }

    public void GenerateLogFile () {
        using (StreamWriter writer = new StreamWriter(LogFileName, false))
        {
            foreach (var fileLog in FileLogs ) {
                writer.WriteLine($"File: {fileLog.Key}");

                if ( fileLog.Value.Count != 0)
                {
                    // Major errors first.
                    // - Probably a better way to do this... but alas
                    writer.WriteLine($"MAJOR ERRORS:");
                    foreach (Log log in fileLog.Value) 
                    {
                        if (log.logType != "MAJOR EXCEPTION" ) {
                            continue;
                        }
                        foreach (string messageLine in log.message) {
                            writer.WriteLine($" - {messageLine}");
                        }
                    }
                    
                    // Naming convention errors first.
                    // - Probably a better way to do this... but alas
                    writer.WriteLine($"NAMING CONVENTION ERRORS:");
                    foreach (Log log in fileLog.Value) 
                    {
                        if (log.logType != "NAMING CONVENTION" ) {
                            continue;
                        }
                        foreach (string messageLine in log.message) {
                            writer.WriteLine($" - {messageLine}");
                        }
                    }
                    
                    // Then field errors.
                    writer.WriteLine($"FIELD ERRORS:");
                    foreach (Log log in fileLog.Value) 
                    {
                        if (log.logType != "FIELD" ) {
                            continue;
                        }
                        writer.WriteLine($"Line: {log.lineNumber}");
                        foreach (string messageLine in log.message) {
                            writer.WriteLine($" - {messageLine}");
                        }
                    }
                }
                else {
                    writer.WriteLine($"*** FILE IS SWEET! ***");
                }
                writer.WriteLine("");
            }
        }
    }
}
