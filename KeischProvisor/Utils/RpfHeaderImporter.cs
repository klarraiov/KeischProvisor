using CodeWalker.GameFiles;
using Respectre.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KeischProvisor.Utils
{
    public static class RpfHeaderImporter
    {
        public static HSHRReplacementData Load(string rpfPath, string gameDirectory)
        {
            if (string.IsNullOrWhiteSpace(rpfPath))
                throw new ArgumentException("An RPF file path is required.", nameof(rpfPath));
            if (!File.Exists(rpfPath)) throw new FileNotFoundException("The selected RPF file was not found.", rpfPath);
            if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
                throw new DirectoryNotFoundException("Set a valid GTA V game directory in Settings before importing an RPF.");

            GTA5Keys.LoadFromPath(gameDirectory, gen9: true);

            var errors = new List<string>();
            var rpf = new RpfFile(rpfPath, string.Empty);
            rpf.ScanStructure(null, errors.Add);
            if (rpf.LastException != null || rpf.raw_header == null || rpf.Children == null || errors.Count > 0)
                throw new InvalidDataException(errors.Count > 0
                    ? string.Join(Environment.NewLine, errors)
                    : rpf.LastError ?? "The selected RPF could not be scanned.");

            var children = new List<HSHRReplacementSubheader>();
            foreach (var child in rpf.Children)
            {
                if (child.raw_header == null || child.raw_header.Length < 16)
                    throw new InvalidDataException($"The child RPF '{child.Name}' has no valid raw header.");
                uint hash = new JenkHash(child.Name.ToLowerInvariant(), JenkHashInputEncoding.ASCII).HashUint;
                children.Add(new HSHRReplacementSubheader(child.Name, hash, child.raw_header));
            }

            return new HSHRReplacementData(rpf.raw_header, children);
        }

        public static string NormalizeTargetName(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                throw new ArgumentException("A top-header name is required.", nameof(targetName));

            string normalized = targetName.Trim().Replace('\\', '/').ToLowerInvariant();
            if (!normalized.EndsWith(".rpf", StringComparison.Ordinal))
                throw new ArgumentException("The top-header name must end with .rpf.", nameof(targetName));
            if (normalized.StartsWith("/", StringComparison.Ordinal)
                || normalized.Split('/').Any(part => part.Length == 0 || part == "." || part == ".."))
                throw new ArgumentException("Enter a relative RPF name or folder path.", nameof(targetName));
            if (normalized.Any(character => character > 0x7F))
                throw new ArgumentException("Top-header names must contain ASCII characters.", nameof(targetName));
            return normalized;
        }

        public static uint CalculateNameHash(string targetName)
        {
            string normalized = NormalizeTargetName(targetName);
            return new JenkHash(normalized, JenkHashInputEncoding.ASCII).HashUint;
        }
    }
}
