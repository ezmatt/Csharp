using System;
using System.IO;
using System.Reflection;

public class Logger
{
    private readonly string _filePath;

    public Logger(string fileName)
    {
        _filePath = fileName;
        try
        {
            File.WriteAllText(_filePath, string.Empty); // Create or overwrite the file
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating log file: {ex.Message}");
        }
    }

    public void Log(string message, bool writeToFile = true)
    {
        WriteToScreen(message);
        if (writeToFile)
        {
            WriteToFile(message);
        }
    }

    private static void WriteToScreen(string message)
    {
        Console.WriteLine(message);
    }

    private void WriteToFile(string message)
    {
        try
        {
            File.AppendAllText(_filePath, message + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to file: {ex.Message}");
        }
    }

    public void LogObject(object obj, int indentLevel = 0)
    {
        if (obj == null)
        {
            WriteIndented("null", indentLevel);
            return;
        }

        Type type = obj.GetType();

        // Handle simple types and strings
        if (type.IsPrimitive || obj is string || obj is DateTime || obj is decimal)
        {
            WriteIndented(obj.ToString(), indentLevel);
            return;
        }

        // Handle collections
        if (obj is IEnumerable<object> list)
        {
            foreach (var item in list)
            {
                LogObject(item, indentLevel + 1);
            }
            return;
        }

        // Handle dictionaries
        if (obj is System.Collections.IDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                WriteIndented($"Key: {key}", indentLevel + 1);
                LogObject(dict[key], indentLevel + 2);
            }
            return;
        }

        // Complex objects: use reflection to log properties
        WriteIndented($"[{type.Name}]", indentLevel);

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            try
            {
                object value = prop.GetValue(obj);

                if (value == null)
                {
                    WriteIndented($"{prop.Name}: null", indentLevel + 1);
                }
                else if (value is string || prop.PropertyType.IsPrimitive || value is DateTime || value is decimal)
                {
                    WriteIndented($"{prop.Name}: {value}", indentLevel + 1);
                }
                else if (value is IEnumerable<object> enumerable)
                {
                    WriteIndented($"{prop.Name}:", indentLevel + 1);
                    foreach (var item in enumerable)
                    {
                        LogObject(item, indentLevel + 2);
                    }
                }
                else if (value is System.Collections.IEnumerable nonGenericEnumerable)
                {
                    WriteIndented($"{prop.Name}:", indentLevel + 1);
                    foreach (var item in nonGenericEnumerable)
                    {
                        LogObject(item, indentLevel + 2);
                    }
                }
                else
                {
                    WriteIndented($"{prop.Name}:", indentLevel + 1);
                    LogObject(value, indentLevel + 2);
                }
            }
            catch
            {
                WriteIndented($"{prop.Name}: [Error reading property]", indentLevel + 1);
            }
        }
    }

    private void WriteIndented(string message, int indentLevel)
    {
        Log($"{new string(' ', indentLevel * 2)}{message}");
    }
}
