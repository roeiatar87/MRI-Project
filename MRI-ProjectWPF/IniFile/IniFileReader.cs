using System;
using System.Collections.Generic;
using System.IO;

namespace WpfPostItDetector.IniFile
{
    public class IniFileReader
    {
        private string _filePath;
        private readonly Dictionary<string, Dictionary<string, string>> _sections;

        // Constructor for file path
        public IniFileReader(string filePath)
        {
            _filePath = filePath;
            _sections = new Dictionary<string, Dictionary<string, string>>();
            LoadIniFileFromPath();
        }

        // Constructor for stream (e.g., embedded resource)
        public IniFileReader(Stream iniStream)
        {
            _sections = new Dictionary<string, Dictionary<string, string>>();
            LoadIniFileFromStream(iniStream);
        }

        // Load INI file from file path
        private void LoadIniFileFromPath()
        {
            if (!File.Exists(_filePath))
                throw new FileNotFoundException("INI file not found", _filePath);

            LoadIniFile(File.ReadLines(_filePath));
        }

        // Load INI file from a Stream
        private void LoadIniFileFromStream(Stream iniStream)
        {
            using (StreamReader reader = new StreamReader(iniStream))
            {
                LoadIniFile(reader.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None));
            }
        }

        // Core loading logic for any IEnumerable<string> (file lines or stream)
        private void LoadIniFile(IEnumerable<string> lines)
        {
            string currentSection = "";

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    _sections[currentSection] = new Dictionary<string, string>();
                }
                else if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith(";"))
                {
                    int index = trimmedLine.IndexOf('=');
                    if (index != -1)
                    {
                        string key = trimmedLine.Substring(0, index).Trim();
                        string value = trimmedLine.Substring(index + 1).Trim();
                        _sections[currentSection][key] = value;
                    }
                }
            }
        }

        public string GetValue(string section, string key)
        {
            if (_sections.ContainsKey(section) && _sections[section].ContainsKey(key))
            {
                return _sections[section][key];
            }
            return null;
        }

        public Dictionary<string, string> GetSection(string section)
        {
            return _sections.ContainsKey(section) ? _sections[section] : null;
        }

        public void WriteValue(string section, string key, string value)
        {
            if (string.IsNullOrEmpty(_filePath))
                throw new InvalidOperationException("Write operation not supported for INI loaded from stream.");

            if (!_sections.ContainsKey(section))
            {
                _sections[section] = new Dictionary<string, string>();
            }

            _sections[section][key] = value;
            WriteToFile();
        }

        private void WriteToFile()
        {
            if (string.IsNullOrEmpty(_filePath))
                throw new InvalidOperationException("File path is not set for writing.");

            using (StreamWriter writer = new StreamWriter(_filePath))
            {
                foreach (var section in _sections)
                {
                    writer.WriteLine($"[{section.Key}]");

                    foreach (var kvp in section.Value)
                    {
                        writer.WriteLine($"{kvp.Key}={kvp.Value}");
                    }

                    writer.WriteLine();
                }
            }
        }
    }
}
