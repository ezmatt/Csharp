
namespace CommonLibrary;

public class Utilities
{
    public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
	{
		Directory.CreateDirectory(target.FullName);
		foreach (FileInfo fi in source.GetFiles())
		{
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
		}
		foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
		{
			DirectoryInfo nextTargetSubDir =
			target.CreateSubdirectory(diSourceSubDir.Name);
			CopyAll(diSourceSubDir, nextTargetSubDir);
            
		}
	}

	public static string ValidateInput(string message, string defaultResponse = "", string[]? validResponses = null)
	{

        Console.WriteLine(message);
		string? response = Console.ReadLine();
		
		// If there is a default and the response is blank, then use the default.
		if (defaultResponse != null && string.IsNullOrEmpty(response))
		{
			response = defaultResponse;
		}
		
		// If there is a list of options, then check the response is in the list.
		while (validResponses != null && !validResponses.Contains(response.ToUpper()))
		{
			response = Console.ReadLine();
			if (!string.IsNullOrEmpty(defaultResponse) && string.IsNullOrEmpty(response))
			{
				response = defaultResponse;
			}
		}

		// If the response is still blank, then keep asking for a response.
		while (string.IsNullOrEmpty(response) && defaultResponse == null)
		{
			response = Console.ReadLine();
		}
		//StatusHelper.AppendStatus("Selection:"+response);
		return response;
	}
}
