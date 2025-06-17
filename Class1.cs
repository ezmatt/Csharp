using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace XdpGroupingTool
{
    public class XdpFingerprint
    {
        public string Filename { get; set; }
        public List<string> FieldNames { get; set; }
        public List<string> Scripts { get; set; }
        public List<string> StaticText { get; set; }
        public string Hash { get; set; }
    }

    public static class XdpGrouper
    {
        private static readonly XNamespace xfa = "http://www.xfa.org/schema/xfa-template/2.8/";

        public static XdpFingerprint ExtractFingerprintFromXdp(string path)
        {
            var doc = XDocument.Load(path);
            var template = doc.Descendants(xfa + "template").FirstOrDefault();
            if (template == null) return null;

            var fieldNames = template.Descendants(xfa + "field")
                .Select(f => (string)f.Attribute("name") ?? "")
                .OrderBy(name => name)
                .ToList();

            var scripts = template.Descendants(xfa + "script")
                .Where(s => !string.IsNullOrWhiteSpace(s.Value))
                .Select(s => s.Value.Trim())
                .OrderBy(s => s)
                .ToList();

            var drawValues = template.Descendants(xfa + "draw")
                .Select(d => d.Element(xfa + "value")?.Value?.Trim())
                .Where(val => !string.IsNullOrWhiteSpace(val))
                .OrderBy(val => val)
                .ToList();

            var combined = string.Join("\n", fieldNames.Concat(scripts).Concat(drawValues));
            var hash = ComputeMD5Hash(combined);

            return new XdpFingerprint
            {
                Filename = Path.GetFileName(path),
                FieldNames = fieldNames,
                Scripts = scripts,
                StaticText = drawValues,
                Hash = hash
            };
        }

        public static Dictionary<string, List<XdpFingerprint>> GroupXdpFiles(string directory)
        {
            var groups = new Dictionary<string, List<XdpFingerprint>>();

            foreach (var path in Directory.GetFiles(directory, "*.xdp"))
            {
                var fingerprint = ExtractFingerprintFromXdp(path);
                if (fingerprint == null) continue;

                if (!groups.ContainsKey(fingerprint.Hash))
                    groups[fingerprint.Hash] = new List<XdpFingerprint>();

                groups[fingerprint.Hash].Add(fingerprint);
            }

            return groups;
        }

        private static string ComputeMD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
