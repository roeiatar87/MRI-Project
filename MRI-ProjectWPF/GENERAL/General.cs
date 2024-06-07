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
        public int DefaוltCamera = 0;

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
                DefaוltCamera = int.Parse(iniFile.GetValue("Camera_Params", " DefaוltCamera"));



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

    }
}
