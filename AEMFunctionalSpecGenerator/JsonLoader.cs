using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XDPToolKit.Models;

namespace AEMFunctionalSpecGenerator
{
    public static class JsonLoader
    {
        public static FormJsonModel LoadSubformFromJson(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Could not find the specified JSON file.", filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FormJsonModel>(json, options)
                ?? new FormJsonModel();
        }
    }
}
