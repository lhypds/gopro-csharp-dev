using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoProCSharpDev.Utils
{
    class ConfigUtils
    {
        public static string Read(string key)
        {
            if (!File.Exists(Const.CONFIG_FILE)) File.Create(Const.CONFIG_FILE).Close();
            string value = string.Empty;

            // Read key bind list
            try
            {
                // Create an instance of StreamReader to read from a file.
                // The using statement also closes the StreamReader.
                using (StreamReader sr = new StreamReader(Const.CONFIG_FILE))
                {
                    string line;
                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] lineStrs = line.Split(' ');
                        if (lineStrs[0].Equals(key))
                            return lineStrs[1];
                    }
                }
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                MessageBox.Show("The file could not be read: " + e.Message, "Message");
            }
            return value;
        }

        public static bool Save(string key, string value)
        {
            if (!File.Exists(Const.CONFIG_FILE)) File.Create(Const.CONFIG_FILE).Close();
            List<string[]> configStringList = new List<string[]>();

            // Read key bind list
            try
            {
                // Create an instance of StreamReader to read from a file.
                // The using statement also closes the StreamReader.
                using (StreamReader sr = new StreamReader(Const.CONFIG_FILE))
                {
                    string line;
                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] lineStrs = line.Split(' ');
                        configStringList.Add(lineStrs);
                    }
                }
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                MessageBox.Show("The file could not be read: " + e.Message, "Message");
                return false;
            }

            // Modify
            bool isFound = false;
            foreach (string[] config in configStringList)
            {
                if (config[0].Equals(key))
                {
                    isFound = true;
                    config[1] = value;
                }
            }

            // If not found config, add one
            if (!isFound)
            {
                configStringList.Add(new string[] { key, value });
            }

            // Write key bind list
            using (StreamWriter sw = new StreamWriter(Const.CONFIG_FILE))
            {
                foreach (string[] config in configStringList)
                {
                    sw.WriteLine(config[0] + ' ' + config[1]);
                }
            }
            return true;
        }
    }
}
