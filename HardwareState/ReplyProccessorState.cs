using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAssistant.Dialog;
using TestAssistant.AppConfiguration;

namespace TestAssistant.HardwareState
{
    public static class ReplyProccessorState
    {
        public static bool Power
        {
            
            get
            {
                return power; // return value 
            }
            set
            {
                //check if the value is different from the current valeu 
                if(power != value)
                {
                    power = value; 

                    PowerStateChanged(); 
                }
            }
        }

        public static bool Auto_Connect
        {

            get
            {
                return auto_Connect; // return value 
            }
            set
            {
                //check if the value is different from the current valeu 
                if (auto_Connect != value)
                {
                    auto_Connect = value;

                    ConnectionModeChanged();
                }
            }
        }
        public enum Modes
        {
            Manual,
            Auto
        }
        private static bool power;
        private static bool auto_Connect;

        public static bool Boot_Terminal_Connected = false;
        public static bool Main_Terminal_Connected = false;

        public static bool USB_Application_Loaded = false;

        public static bool InitilisingTest = false;

        public static Modes PowerMode;
        public static Modes ConnectionMode;


        public static EventHandler<string> PowerState;
        public static EventHandler manualConnectionUpdate; // Event to user UI update to allow user to manualy select a port and connect 
        public static EventHandler autoConnectionUpdate; // Event to user UI update to allow user to manualy select a port and connect 

        private static DialogManager UI_Dialog = new DialogManager();
      
        private static string tempTestHolder;
        private static AppConfigManager configReader = new AppConfigManager();

        public static void InitiatePower(string test)
        {
            try
            {
                if (PowerMode != Modes.Manual && PowerMode != Modes.Auto)
                {
                    if (configReader.IsManualPowerMode())
                    {
                        PowerMode = Modes.Manual;
                        UI_Dialog.DisplayPowerOptions();
                    }
                    else
                    {
                        PowerMode = Modes.Auto;
                        UI_Dialog.DisplayPowerOptions();
                    }

                }
                else
                {
                    if (PowerMode == Modes.Manual)
                    {
                        UI_Dialog.DisplayPowerOptions();
                    }
                    else
                    {
                        // Automatically initiate power to the board 
                    }
                }


                tempTestHolder = test;
            }
            catch(Exception ex)
            {
                UI_Dialog.DiplayErrorUI(ex.Message);
            }

        }
        public static void InitiateCommunicationMode()
        {
            try
            {
                if(configReader.IsManualCommunicationMode())
                {
                    Auto_Connect = false;
                }
                else
                {
                    Auto_Connect = true; 
                }
                
            }
            catch (Exception ex)
            {
                UI_Dialog.DiplayErrorUI(ex.Message);
            }

        }
        private static void PowerStateChanged()
        {
            try
            {
                
                PowerState?.Invoke(null, tempTestHolder);

                if (PowerMode == Modes.Manual) // IF we are on manual mode then turn power flag off after initial setup .
                {
                    power = false;
                }
            }
            catch (Exception ex)
            {
                UI_Dialog.DiplayErrorUI(ex.Message);
            }

            
        }
        private static void ConnectionModeChanged()
        {
            try
            {
                if(Auto_Connect != true)
                {
                    ConnectionMode = Modes.Manual;
                    manualConnectionUpdate.Invoke(null, EventArgs.Empty);
                }

                else if(Auto_Connect == true)
                {
                    ConnectionMode = Modes.Auto;
                    autoConnectionUpdate.Invoke(null, EventArgs.Empty);
                    
                }
            }
            catch (Exception ex)
            {
                UI_Dialog.DiplayErrorUI(ex.Message);
            }


        }
        public static void ConnectionUpdate(bool succesful, string terminal)
        {
            try
            {
                if (terminal == "MAIN")
                {
                    Main_Terminal_Connected = succesful;
                    
                }
                else
                {
                    Boot_Terminal_Connected = succesful;
                    
                }
            }
            catch (Exception ex)
            {
                UI_Dialog.DiplayErrorUI(ex.Message);
            }

        }
        public static bool AreBothTerminalsConnected()
        {
            if(Boot_Terminal_Connected && Main_Terminal_Connected)
            {
                return true;
            }
            else
            {
                return false; 
            }
        }

        

    }


}
