using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using WpfPostItDetector.IniFile;

namespace WpfPostItDetector.GENERAL
{
    public class General
    {
        public static IniFileReader iniFile;
        string filePath = @"C:\MRI-INI\MRI_INIFILE.ini";
        public int Threshold_defVal = 0;
        public int DefaultCamera = 0;

        public General()
        {
            iniFile = new IniFileReader(filePath);
            ReadIniFile();
        }

        public void ReadIniFile()
        {
            try
            {
                Threshold_defVal = int.Parse(iniFile.GetValue("Camera_Params", "Threshold_defVal"));
                DefaultCamera = int.Parse(iniFile.GetValue("Camera_Params", "DefaultCamera"));



            }
            catch (Exception)
            {


            }

        }


        public void SaveThresholdValue(int newValue)
        {
            try
            {
                Threshold_defVal = newValue;
                iniFile.WriteValue("Camera_Params", "Threshold_defVal", newValue.ToString());
            }
            catch (Exception)
            {


            }

        }

        public void SaveCameraValue(int newValue)
        {
            try
            {
                Threshold_defVal = newValue;
                iniFile.WriteValue("Camera_Params", "DefaultCamera", newValue.ToString());
            }
            catch (Exception)
            {


            }

        }

    }
}
