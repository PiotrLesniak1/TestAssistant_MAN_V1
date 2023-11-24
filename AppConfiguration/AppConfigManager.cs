using System;
using System.Windows;
using System.Xml;
using System.IO;
namespace TestAssistant.AppConfiguration
{
    class AppConfigManager
    {
        private XmlDocument xmlDoc; // XML document container 
        //private string configFile = "AppConfiguration/AppConfig.xml";//path to configFile
        private string configFile =  Path.Combine(AppDomain.CurrentDomain.BaseDirectory,  "AppConfig.xml");//path to configFile
        public  AppConfigManager() // constructor will make a new object Requires configFile string 
        {
            
            xmlDoc = new XmlDocument(); // populates the container with XML doc / stores it in the container 
            xmlDoc.Load(configFile); // loads the content of configFile into our container xmlDoc
            

        }

        public void SetValueByKey(string key , string newValue)
        {
            try
            {
                XmlNode node = xmlDoc.SelectSingleNode($"/configuration/appSetting/terminalConfig/add[@key ='{key}']"); // Points to the add element node within the XML document and its attribute key

                if(node != null)
                {
                    node.Attributes["value"].Value = newValue; // if found then set the value to desired value 
                    xmlDoc.Save("AppConfig.xml");
                }
                else
                {
                    //attribute not found 
                    MessageBox.Show("Could not find Setting"); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); // report any errors 
            }
            
        } 
        
        public string ReturnValueByKey(string key)
        {
            string configValue = null;
            try
            {
                XmlNode node = xmlDoc.SelectSingleNode($"/configuration/appSetting/terminalConfig/add[@key ='{key}']"); // Points to the add element node within the XML document and its attribute key
                if(node != null)
                {
                    configValue = node.Attributes["value"].Value;
                }
                else
                {
                    configValue = null; 
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message); // report any errors
            }
            return configValue;
        }

        public bool IsManualPowerMode() // true if manual 
        {
            bool ManualPowerMode = true; // assume that it is manual 
            try
            {
                string Value = xmlDoc.SelectSingleNode("/configuration/appSetting/PowerMode").InnerText.Replace("\t", "").Trim(); 
                if (Value != null)
                {
                  // string Value = node.Name;
                    if (Value == "MANUAL")
                    {
                        ManualPowerMode = true;
                    }
                }
                else
                {
                    //string Value = null;
                    ManualPowerMode = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); // report any errors
            }
            return ManualPowerMode;
        }
        public bool IsManualCommunicationMode() // true if manual 
        {
            bool Manual = true; // assume that it is manual 
            try
            {
                string Value = xmlDoc.SelectSingleNode("/configuration/appSetting/Communication_Mode").InnerText.Replace("\t", "").Trim();
                if (Value != null)
                {
                    // string Value = node.Name;
                    if (Value == "MANUAL")
                    {
                        Manual = true;
                    }
                    else
                    {
                        Manual = false;
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); // report any errors
            }
            return Manual;
        }

        public void UpdatePortConfig(string Terminal, string port)
        {
            if (Terminal == "MAIN")
            {
                
                SetValueByKey("MainPort",port);
                
            }
            else
            {
      
                SetValueByKey("BootPort", port);
                
            }

        }
    }
}
