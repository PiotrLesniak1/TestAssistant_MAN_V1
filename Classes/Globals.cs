using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAssistant.HardwareState;
using TestAssistant.AppConfiguration;

namespace TestAssistant.Classes
{
    public static class Globals
    {
        // Global variables to keep an eye on connection status.
        public static bool IsTerminal_M_Connected { get; set; } = false; // Initialize with default value

        public static bool IsTerminal_B_Connected { get; set; } = false; // Initialize with default value

        public static bool IsTestInProgress { get; set; } // A global variable to indicate if a test is in progress.
        public static string TestInProgressGlobal { get; set; }

        public static bool InhibitConnectionInstructions = false; // false by default

        public static bool Debugging = true;



        public static void ConnectionUpdate(bool succesful , string terminal)
        {
            if (terminal == "MAIN")
            {
                IsTerminal_M_Connected = succesful;
            }
            else
            {
                IsTerminal_B_Connected = succesful;
            }

        }
    }
}
