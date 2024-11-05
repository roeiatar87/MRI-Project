using System;
using System.IO;
using System.Reflection;
using WpfPostItDetector.IniFile;

namespace WpfPostItDetector.GENERAL
{
    public class General
    {
        public static IniFileReader iniFile;
        private readonly string localFilePath;
        public int Threshold_defVal = 100;
        public int DefaultCamera = 0;
        public double ThresholdMotionTime = 0.1;

        public General()
        {
            localFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WpfPostItDetector", "MRI_INIFILE.ini");

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));

            if (!File.Exists(localFilePath))
            {
                // If the local copy doesn't exist, create it from the embedded resource
                ExtractEmbeddedIniFile();
            }

            iniFile = new IniFileReader(localFilePath);
            ReadIniFile();
        }

        private void ExtractEmbeddedIniFile()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WpfPostItDetector.MRI_INIFILE.ini"; // Adjust to match actual namespace and filename

            using (Stream iniStream = assembly.GetManifestResourceStream(resourceName))
            using (FileStream fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
            {
                iniStream?.CopyTo(fileStream);
            }
        }

        public void ReadIniFile()
        {
            try
            {
                Threshold_defVal = int.Parse(iniFile.GetValue("Camera_Params", "Threshold_defVal") ?? "100");
                DefaultCamera = int.Parse(iniFile.GetValue("Camera_Params", "DefaultCamera") ?? "0");
                ThresholdMotionTime = double.Parse(iniFile.GetValue("Camera_Params", "ThresholdMotionTime") ?? "0.1");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading INI file: " + ex.Message);
            }
        }

        public void SaveThresholdValue(int newValue)
        {
            try
            {
                Threshold_defVal = newValue;
                iniFile.WriteValue("Camera_Params", "Threshold_defVal", newValue.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving Threshold_defVal: " + ex.Message);
            }
        }

        public void SaveCameraValue(int newValue)
        {
            try
            {
                DefaultCamera = newValue;
                iniFile.WriteValue("Camera_Params", "DefaultCamera", newValue.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving DefaultCamera: " + ex.Message);
            }
        }

        public void SaveMotionTimeValue(double newValue)
        {
            try
            {
                ThresholdMotionTime = newValue;
                iniFile.WriteValue("Camera_Params", "ThresholdMotionTime", newValue.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving ThresholdMotionTime: " + ex.Message);
            }
        }
    }
}
