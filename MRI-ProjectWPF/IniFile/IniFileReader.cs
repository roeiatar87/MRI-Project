using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfPostItDetector.IniFile
{
    public class IniFileReader
    {
        private string _filePath;

        private readonly Dictionary<string, Dictionary<string, string>> _sections;

        public IniFileReader(string filePath)
        {
            _filePath = filePath;
            _sections = new Dictionary<string, Dictionary<string, string>>();
            LoadIniFile();
        }


        private void LoadIniFile()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    throw new FileNotFoundException("INI file not found", _filePath);
                }

                string currentSection = "";

                foreach (string line in File.ReadLines(_filePath))
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
            catch (Exception )
            {

                
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
            if (!_sections.ContainsKey(section))
            {
                _sections[section] = new Dictionary<string, string>();
            }

            _sections[section][key] = value;

            WriteToFile();
        }

        private void WriteToFile()
        {
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
