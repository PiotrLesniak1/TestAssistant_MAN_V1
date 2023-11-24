using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TestAssistant.AppConfiguration;
using TestAssistant.Classes;
using TestAssistant.HardwareState; 
namespace TestAssistant.HardwareCommunication
{

    class ComManager
    {

        // Terminal instances for BOOT and MAIN.
        public Terminal TerminalBOOT = new Terminal();

        public Terminal TerminalMAIN = new Terminal();

        private string BootPort;
        private string MainPort;

        private int ConnectionAttempts = 0;


        private AppConfigManager configReader = new AppConfigManager();



        public event EventHandler<string> TerminalConnected ;
        public event EventHandler<string> TerminalDisconnected;
        public event EventHandler<string> DisplayErrorUI; // Event for terminal output

        public delegate void BoolStringEvent(bool status, string terminal);
        public event BoolStringEvent ConnectionUpdate; // Event for terminal output



        public event EventHandler manualConnectionUpdate; // Event to user UI update to allow user to manualy select a port and connect 

        public bool ConnectBoot()
        {
            if(BootPort == null)
            {
                BootPort = GetTerminalBootPort();
            }
            return ConnectTerminal(TerminalBOOT, "BOOT");
        }
        public bool ConnectMain()
        {
            if (MainPort == null)
            {
                MainPort = GetTerminalMainPort();
            }
            return ConnectTerminal(TerminalMAIN,"MAIN");
        }
        public bool ConnectTerminal(Terminal terminal, string bootORmain)
        {
            bool connectionAttempt = false;
            switch(bootORmain)
            {
                case "BOOT":
                    
                    if (terminal.Connect($"COM{BootPort}"))
                    {
                        ConnectionUpdate?.Invoke(true, "BOOT");
                        TerminalConnected?.Invoke(null, "BOOT");
                        connectionAttempt = true;
                    }
                    else
                    {
                        ConnectionUpdate?.Invoke(false, "BOOT");
                        SendErrorOrMessageToUIelement($"Failed to Connect {bootORmain} terminal");
                    }
                    break;

                case "MAIN":
                    
                    if (terminal.Connect($"COM{MainPort}"))
                    {
                        ConnectionUpdate?.Invoke(true, "MAIN");

                        TerminalConnected?.Invoke(null, "Main");
                        connectionAttempt = true;
                    }
                    else
                    {
                        ConnectionUpdate?.Invoke(false, "MAIN");
                        SendErrorOrMessageToUIelement($"Failed to Connect {bootORmain} terminal");
                    }
                    break;

                default:
                    SendErrorOrMessageToUIelement("Invalid Terminal ");
                        
                    break;
                    
            }
            return connectionAttempt;
        }
        public void DisconnectBoot()
        {
            DisconnectTerminal(TerminalBOOT, "BOOT");
        }
        public void DisconnectMain()
        {
            DisconnectTerminal(TerminalMAIN, "MAIN");
        }
        public void DisconnectTerminal(Terminal terminal, string bootORmain)
        {

            switch (bootORmain)
            {
                case "BOOT":
                    if (TerminalBOOT.Disconnect())
                    {
                        SendErrorOrMessageToUIelement("Boot Dissconnected");
                        ConnectionUpdate?.Invoke(false, "BOOT");
                        TerminalDisconnected?.Invoke(null, "BOOT");
                    }
                    else
                    {
                        ConnectionUpdate?.Invoke( true , "BOOT");
                        
                        SendErrorOrMessageToUIelement("Error when trying to disconnect BOOT terminal");
                    }
                    break;

                case "MAIN":
                    if (TerminalMAIN.Disconnect())
                    {

                        SendErrorOrMessageToUIelement("Main Dissconnected");
                        ConnectionUpdate?.Invoke(false, "MAIN");
                        TerminalDisconnected?.Invoke(null, "MAIN");
                    }
                    else
                    {
                        ConnectionUpdate?.Invoke(true, "MAIN");
                        
                        SendErrorOrMessageToUIelement("Error when trying to disconnect MAIN terminal");
                    }
                    break;

                default:
                    SendErrorOrMessageToUIelement("Invalid Terminal ");
                    break;
            }

        }

        public string GetTerminalBootPort()
        {
            
            return configReader.ReturnValueByKey("BootPort");
        }
        public string GetTerminalMainPort()
        {
            return configReader.ReturnValueByKey("MainPort");
        }

        private void SendErrorOrMessageToUIelement(string message)
        {

            if(message != null)
            {
                DisplayErrorUI.Invoke(this, message);
            }
            
        }

        public async Task<bool> AttemptToConnectTerminals()
        {

            if (Globals.IsTerminal_B_Connected && Globals.IsTerminal_M_Connected)
            {
                Globals.InhibitConnectionInstructions = true;
            }


            while ((!Globals.IsTerminal_B_Connected || !Globals.IsTerminal_M_Connected) && ConnectionAttempts < 6)
            {
                ConnectionAttempts++;
                if(ConnectionAttempts < 6)
                {
                    ConnectBoot();
                    ConnectMain();

                    if(Globals.IsTerminal_B_Connected && Globals.IsTerminal_M_Connected)
                    {

                        ConnectionAttempts = 0;
                        return Globals.InhibitConnectionInstructions = true;
                    }
                    else
                    {

                        await Task.Delay(TimeSpan.FromSeconds(15));
                    }
                    
                }
                else
                {
                    //ADD - Ask user if they would like to see available ports 
                    //IF YES then show avaialble ports and ask user to select them
                    // add condition to see if we have already tried this 

                    MessageBox.Show("Failed to Automatically Connect \n  Switching to manual mode \n Follow the Onscreen instructions ");
                    if (ReplyProccessorState.ConnectionMode == ReplyProccessorState.Modes.Auto)
                    {
                        ReplyProccessorState.ConnectionMode = ReplyProccessorState.Modes.Manual;
                       
                        manualConnectionUpdate?.Invoke(null,EventArgs.Empty);
                    }
                    


                    ConnectionAttempts = 0;

                    //If user selects new ports break here and reattempt the connections 
                    return Globals.InhibitConnectionInstructions = false ;
                }
                
            }


            return Globals.InhibitConnectionInstructions;
        }
        public async Task<bool> TransferTestFromUsb(string test)
        {

            int attempts = 0; 

            while(!ReplyProccessorState.USB_Application_Loaded && attempts < 5)
            {
                attempts++;
                DisplayErrorUI?.Invoke(this,"USB application not loaded ");
                await Task.Delay(TimeSpan.FromSeconds(10));
            }

            if(ReplyProccessorState.USB_Application_Loaded)
            {
                await TerminalBOOT.ReplaceSDcontent(test);

            }
            else if (!ReplyProccessorState.USB_Application_Loaded && attempts >= 5 )
            {
                SendErrorOrMessageToUIelement("Failed to Load USB application !");
                DisconnectBoot();
                DisconnectMain(); 
                return false; 
            }

            return true;
        }

        public void ReverseTerminalSettings()
        {
            configReader.SetValueByKey("BootPort",MainPort);
            configReader.SetValueByKey("MainPort",BootPort);

            BootPort = GetTerminalBootPort();
            MainPort = GetTerminalMainPort();
            ConnectBoot();
            ConnectMain();

        }

        public  void UpdateConfigPorts( string terminal , string Port)
        {
            if (terminal == "MAIN")
            {
                if(ReplyProccessorState.Main_Terminal_Connected)
                {
                    configReader.UpdatePortConfig(terminal, Port);
                }
            }
            else
            {
                if (ReplyProccessorState.Boot_Terminal_Connected)
                {
                    configReader.UpdatePortConfig(terminal, Port);
                }
            }

        }

    }
}
