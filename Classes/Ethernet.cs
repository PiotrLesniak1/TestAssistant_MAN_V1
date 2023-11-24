using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;

namespace TestAssistant.Classes
{
    /*
     * File: Ethernet.cs
     * Author: Piotr Lesniak (Raytheon)
     * Date: 16/10/23
     * Description: This file contains the implementation of the Ethernet class, which extends the Test_Template class.
     *              The Ethernet class is responsible for performing tests related to Ethernet connectivity and functionality.
     *              It includes methods for checking various instructions related to Ethernet testing, such as network connection tests and PING tests.
     * Dependencies: This class relies on the Test_Template class and other related classes and components for its functionality.
     * Usage: The Ethernet class is used within the TestAssistant application to conduct Ethernet tests on a hardware device, specifically focusing on network connection and PING test functionalities.
     */

    internal class Ethernet : Test_Template
    {
        // List to store instances of the 'Instruction' class.
        protected internal static List<Instruction> instructions = new List<Instruction>();

        // IP address of the target for PING tests.
        private readonly string TargetIP = "192.168.1.4";
        //private readonly string TargetIP = "127.0.0.1";

     

        // Ping related fields and properties.
        private readonly Ping PingSender = new Ping();

        private PingReply reply;
        private int PingAttempts = 0;
        private bool PingTestRunning = false;

        // Constructor for the 'Ethernet' class.
        public Ethernet(Terminal TerminalMAIN, Terminal TerminalBOOT) : base(TerminalMAIN, TerminalBOOT, instructions)
        {
            // Initialize the number of test attempts.
            TestAttempts = 0;
        }

        // Asynchronously performs a PING test on the target IP address.
        private async Task<bool> PingTestAsync()
        {
            try
            {
                // Delay for 2 seconds before sending the PING request.To allow a connection to be established if done too early PING will fail then pass second time round
                await Task.Delay(2000);

                // Send a PING request to the target IP address.
                reply = await PingSender.SendPingAsync(TargetIP);

                if (reply.Status == IPStatus.Success)
                {
                    // Handle the case when the PING is successful.
                    PingSuccess();
                    PingTestRunning = false;
                    return TestInstructions[CurrentIndex].CheckCompleted;
                }
                else
                {
                    // Handle the case when the PING fails.
                    PingFailed();
                    return TestInstructions[CurrentIndex].CheckCompleted;
                }
            }
            catch (PingException)
            {
                // Handle exceptions related to the PING operation.
                if (reply == null)
                {
                    TestInstructions[CurrentIndex].ErrorText = "Ping Failed";
                }
                else
                {
                    TestInstructions[CurrentIndex].ErrorText = reply.Status.ToString();
                }

                PingFailed();
                return TestInstructions[CurrentIndex].CheckCompleted;
            }
        }

        // Handles the case when a PING test fails.
        private void PingFailed()
        {
            // Increment the number of PING attempts.
            PingAttempts++;

            // Append "Ping Failed" to the actual output.
            TestInstructions[CurrentIndex].ActualOutput += "Ping Failed";

            // Update the error text to show the number of remaining PING attempts.
            TestInstructions[CurrentIndex].ErrorText = $"Ping Failed: {3 - PingAttempts} / 3 attempts left \n\n Retry Ping ?";

            // Mark the current check as not completed.
            TestInstructions[CurrentIndex].CheckCompleted = false;

            // Check if the maximum number of PING attempts (3) has been reached.
            if (PingAttempts >= 3)
            {
                // If maximum attempts reached, fail the test check.
                CheckFailed("Ping Failed");
            }
            else
            {
                // If not at maximum attempts, prompt the user for further actions.
                MessageBoxResult result = MessageBox.Show(TestInstructions[CurrentIndex].ErrorText, "! NO PING RESPONSE !", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    // If the user chooses not to retry, fail the test check.
                    CheckFailed("Ping Failed");
                }
                else
                {
                    // If the user chooses to retry, and this is the first attempt, offer diagnostic options.
                    if (PingAttempts == 1)
                    {
                        MessageBoxResult diagnosisRequested = MessageBox.Show("Run Diagnostic ? ", "PING FAILED", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (diagnosisRequested == MessageBoxResult.Yes)
                        {
                            // If the user requests diagnostics, check the device's IP address and display relevant information.
                            if (GetLocalIPAddress() != "192.168.1.1")
                            {
                                MessageBox.Show($"Your Device IP address is Incorrect \n Please change it from {GetLocalIPAddress()} to 192.168.1.1", "Invalid IP", MessageBoxButton.OK);
                            }
                            else
                            {
                                MessageBox.Show($"Reason For failure {reply.Status}", "ERROR", MessageBoxButton.OK);
                            }
                        }
                    }

                    // Initiate another PING test asynchronously.
                    _ = PingTestAsync();
                }
            }
        }

        // Handles the case when a PING test succeeds.
        private void PingSuccess()
        {
            // Reset the number of PING attempts.
            PingAttempts = 0;

            // Mark the current check as completed.
            TestInstructions[CurrentIndex].CheckCompleted = true;

            // Append "Ping Successful" to the actual output.
            TestInstructions[CurrentIndex].ActualOutput += "Ping Successful";

            // Update the desired output for the current instruction based on the previous instruction's desired output.
            TestInstructions[CurrentIndex].DesiredOutput = TestInstructions[CurrentIndex - 1].DesiredOutput;

            // Add a test point outcome indicating a pass.
            AddTestPointOutcome("ETH", "PASS");

            // Mark the instruction as passed.
            InstructionPassed();
        }

        // Retrieves the local machine's IP address.
        // Returns:
        //   string: The local IP address.
        private  string GetLocalIPAddress()
        {
            string localIP = string.Empty;

            try
            {
                // Retrieve the host name of the local machine.
                string hostName = Dns.GetHostName();

                // Get IP addresses associated with the hostName.
                IPAddress[] localIPs = Dns.GetHostAddresses(hostName);

                // Loop through the found IP addresses to find the first IPv4 address.
                foreach (IPAddress ip in localIPs)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {

                LogException(ex.Message);
            }


            return localIP;
        }


        //////////////////////////INSTRUCTIONS ////////////////////////////////////////
        //
        // Performs the check for the current instruction.
        // Returns:
        //   bool: True if the check is completed successfully; otherwise, false.
        public override async Task<bool> PerformInstructionCheck()
        {
            bool acquiredMutex = false;
            try
            {
                acquiredMutex = instructionCheckMutex.WaitOne(0);
                if (!acquiredMutex)
                {
                    return false;
                }

                var CurrentInstruction = TestInstructions[CurrentIndex]; // Added a local variable for code readability

                if (!CurrentInstruction.CheckRequired)
                {
                    // If no check is required, mark as complete and return true.
                    CurrentInstruction.CheckCompleted = true;
                }

                if (CurrentInstruction.DesiredOutput != null)
                {
                    string desiredOutput = CurrentInstruction.DesiredOutput;
                    if (CurrentInstruction.TerminalToCheck == Processor.MAIN)
                    {
                        if (CheckMainTerminal(desiredOutput))
                        {
                            // Check the Main terminal for the desired output.
                            if (desiredOutput == "Negotiated connection: FullDuplex 1000 Mbs")
                            {
                                CurrentInstruction.CheckCompleted = await HideBufferInputBool(CurrentInstruction.DesiredOutput, CurrentInstruction.TerminalToCheck);
                            }
                        }
                    }
                    else if (CurrentInstruction.TerminalToCheck == Processor.BOOT)
                    {
                        if (CurrentInstruction.DesiredOutput == "MMCSD boot")
                        {
                            VerifyDevice(); // Verify we are loading from an SD card.
                        }
                        if (CheckBootTerminal(desiredOutput))
                        {
                            // Check the Boot terminal for the desired output.
                            if (desiredOutput == "Negotiated connection: FullDuplex 1000 Mbs")
                            {
                                CurrentInstruction.CheckCompleted = await HideBufferInputBool(CurrentInstruction.DesiredOutput, CurrentInstruction.TerminalToCheck);
                            }
                        }
                    }
                }
                else if (CurrentInstruction.hasSpecialCheck && !PingTestRunning)
                {
                    PingTestRunning = true;

                    Task<bool> asyncTask = Task.Run(async () => await PingTestAsync());

                    return asyncTask.Result;
                }

                // Check for Main and Boot connection.
                if (CurrentInstruction.CheckMainConnected)
                {
                    return IsMainConnected();
                }
                if (CurrentInstruction.CheckBootConnected)
                {
                    return IsBootConnected();
                }

                return CurrentInstruction.CheckCompleted;
            }
            finally
            {
                if (acquiredMutex)
                {
                    instructionCheckMutex.ReleaseMutex();
                }
            }
        }

        protected internal override void InitializeInstructions()
        {
            //AddInstruction(string text, List<string> desiredOutputs, bool actionRequired , bool CheckBootConnected , bool CheckMainConnected)

            if (Globals.InhibitConnectionInstructions)
            {


            }
            else if (!Globals.InhibitConnectionInstructions)
            {
                AddConnectionInstructions();

            }



            AddInstruction(
            /* Text*/             "ETH - Load from MicroSD:\n At the front of the panel, press and hold S2. The amber LED will become solid.\n With S2 pressed, press and release S1. A green LED will flash next to the orange one.\n This indicates that the board will load from the MicroSD card.",
            /* Desired Output*/      "MMCSD boot",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.BOOT,
            /* CheckBox*/          "Loading from SD card",
            /* ErrorText*/         "NOT LOADING FROM SD",
            /* Actual Output*/     null,
            /*Special Check*/     false,
            /*UserAckRequired*/    true);
            AddInstruction(
            /* Text*/             "ETH - All further Instructions will automatically change \n Once the desired output has been detected & tests have been carried out\n \n\n Please Connect an ethernet cable only to your PC !\n\n ",
            /* Desired Output*/    "NWAY Advertising: FullDuplex-1000 FullDuplex-100 HalfDuplex-100 FullDuplex-10 HalfDuplex-10 ",
            /* ActionRequired?*/  true,
            /* CheckBoot*/        false,
            /* CheckMain*/        false,
            /* Terminal*/         Processor.BOOT,
            /* CheckBox*/         null,
            /* ErrorText*/        null,
            /* Actual Output*/    null,
            /*Special Check*/     false);

            AddInstruction(
            /* Text*/             "ETH - Test Running.... \n\n Connect Ethernet cable end to the first BOOT Ethernet slot \n\n The other end should already be connected to your PC\n\n ",
            /* Desired Output*/    "Negotiated connection: FullDuplex 1000 Mbs",
            /* ActionRequired?*/  true,
            /* CheckBoot*/        false,
            /* CheckMain*/        false,
            /* Terminal*/         Processor.BOOT,
            /* CheckBox*/         null,
            /* ErrorText*/        "Ethernet Port not detected",
            /* Actual Output*/    null,
            /*Special Check*/     false,
            /*UserAckRequired*/    true);
            AddInstruction(
           /* Text*/             "ETH - Test Running.... \n\n Running Ping Test",
           /* Desired Output*/   null,
           /* ActionRequired?*/  true,
           /* CheckBoot*/        false,
           /* CheckMain*/        false,
           /* Terminal*/         Processor.BOOT,
           /* CheckBox*/         "Ethernet Port 1 (B)",
           /* ErrorText*/        "Ping Failed",
           /* Actual Output*/    null,
           /*Special Check*/     true);

            AddInstruction(
            /* Text*/             "ETH - Test Running.... \n\n Connect Ethernet cable end to the second BOOT Ethernet slot ",
            /* Desired Output*/    "Negotiated connection: FullDuplex 1000 Mbs",
            /* ActionRequired?*/  true,
            /* CheckBoot*/        false,
            /* CheckMain*/        false,
            /* Terminal*/         Processor.BOOT,
            /* CheckBox*/         null,
            /* ErrorText*/        "Ethernet Port not detected",
            /* Actual Output*/    null,
            /*Special Check*/     false,
            /*UserAckRequired*/    true);
            AddInstruction(
           /* Text*/             "ETH - Test Running.... \n\n Running Ping Test",
           /* Desired Output*/   null,
           /* ActionRequired?*/  true,
           /* CheckBoot*/        false,
           /* CheckMain*/        false,
           /* Terminal*/         Processor.BOOT,
           /* CheckBox*/         "Ethernet Port 2 (B)",
           /* ErrorText*/        "Ping Failed",
           /* Actual Output*/    null,
           /*Special Check*/     true);
            AddInstruction(
            /* Text*/             "ETH - Test Running.... \n\n Connect Ethernet cable end to the first MAIN Ethernet slot ",
            /* Desired Output*/    "Negotiated connection: FullDuplex 1000 Mbs",
            /* ActionRequired?*/  true,
            /* CheckBoot*/        false,
            /* CheckMain*/        false,
            /* Terminal*/         Processor.MAIN,
            /* CheckBox*/         null,
            /* ErrorText*/        "Ethernet Port not detected",
            /* Actual Output*/    null,
            /*Special Check*/     false,
            /*UserAckRequired*/    true);
            AddInstruction(
            /* Text*/             "ETH - Test Running.... \n\n Running Ping Test ",
           /* Desired Output*/       null,
           /* ActionRequired?*/     true,
           /* CheckBoot*/           false,
           /* CheckMain*/           false,
           /* Terminal*/            Processor.MAIN,
           /* CheckBox*/            "Ethernet Port 1 (M)",
           /* ErrorText*/           "Ping Failed",
           /* Actual Output*/       null,
           /*Special Check*/        true,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/             "ETH - Test Running.... \n\n Connect Ethernet cable end to the second MAIN Ethernet slot ",
            /* Desired Output*/    "Negotiated connection: FullDuplex 1000 Mbs",
            /* ActionRequired?*/  true,
            /* CheckBoot*/        false,
            /* CheckMain*/        false,
            /* Terminal*/         Processor.MAIN,
            /* CheckBox*/         null,
            /* ErrorText*/        "Ethernet Port not detected",
            /* Actual Output*/    null,
            /*Special Check*/     false,
            /*UserAckRequired*/    true);
            AddInstruction(
            /* Text*/              "ETH - Test Running.... \n\n Running Ping Test ",
           /* Desired Output*/    null,
           /* ActionRequired?*/   true,
           /* CheckBoot*/         false,
           /* CheckMain*/         false,
           /* Terminal*/          Processor.BOOT,
           /* CheckBox*/          "Ethernet Port 2 (M)",
           /* ErrorText*/         "Ping Failed",
           /* Actual Output*/     null,
           /*Special Check*/      true,
           /*UserAckRequired*/    false);

            AddInstruction(
            /* Text*/       "",
            /* Desired Output*/    null,
            /* ActionRequired?*/   false,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.NONE,
            /* CheckBox*/          null,
            /* ErrorText*/         null,
            /* Actual Output*/     null,
            /*Special Check*/      false,
            /*UserAckRequired*/    false);                           //LAST instruction is here to update the checkboxes , since they are checked off based on their instruction index +1 (only after we move passed the instruction)
        }

        private void AddInstruction(string text, string desiredOutputs, bool actionRequired, bool CheckBootConnected, bool CheckMainConnected, Processor terminal, string checkbox, string errortext, string actualoutput, bool specialcheck , bool userAck = false)
        {
            instructions.Add(new Instruction
            {
                InstructionText = text,
                DesiredOutput = desiredOutputs,
                CheckRequired = actionRequired,
                CheckBootConnected = CheckBootConnected,
                CheckMainConnected = CheckMainConnected,
                TerminalToCheck = terminal,
                CheckBoxText = checkbox,
                ErrorText = errortext,
                ActualOutput = actualoutput,
                hasSpecialCheck = specialcheck,
                userAckRequired = userAck
            });

        }

        // Resets the test to its initial state.
        public override void Reset_Test()
        {
            // Reset the test instructions.
            Reset();

            // Clear any buffers.
            ClearBuffers();

            // Increment the test attempt count.
            IncAttempt();

            // Save a terminal log for debugging.
            _ = SaveTerminalLog("ETH");

            // Reset the Ping-related variables.
            PingAttempts = 0;
            PingTestRunning = false;
        }

        private void AddConnectionInstructions()
        {
            AddInstruction(
            /* Text*/                   "JTAG Cable Connection:\n Connect the USB end of your JTAG cable to your PC.",
            /* Desired Output*/         null,
            /* ActionRequired?*/        false,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.NONE,
            /* CheckBox*/               null,
            /* ErrorText*/              null,
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/        // false,
            /*UserAckRequired*/         true);

            AddInstruction(
            /* Text*/                   "Connect JTAG to BOOT Processor:\n Connect the JTAG end of the cable to the BOOT Processor Slot (JTAG 1).",
            /* Desired Output*/         null,
            /* ActionRequired?*/        false,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.NONE,
            /* CheckBox*/               null,
            /* ErrorText*/              null,
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         //false,
            /*UserAckRequired*/         true);

            AddInstruction(
            /* Text*/                   "Connect to BOOT:\n Select the COM port and click \"Connect.\"",
            /* Desired Output*/         null,
            /* ActionRequired?*/        true,
            /* CheckBoot*/              true,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.NONE,
            /* CheckBox*/               "BOOT Connected",
            /* ErrorText*/              "BOOT Terminal Not Connected",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/        // false,
            /*UserAckRequired*/         true);
            AddInstruction(
            /* Text*/                   "Repeat for MAIN Processor:\n Repeat for the MAIN Processor Slot (JTAG 2).",
            /* Desired Output*/         null,
            /* ActionRequired?*/        false,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.NONE,
            /* CheckBox*/               null,
            /* ErrorText*/              null,
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/        // false,
            /*UserAckRequired*/         true);

            AddInstruction(
            /* Text*/                   "Connect JTAG to MAIN Processor:\n Connect the JTAG end of the cable to the MAIN Processor Slot (JTAG 2).",
            /* Desired Output*/         null,
            /* ActionRequired?*/        false,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.NONE,
            /* CheckBox*/               null,
            /* ErrorText*/              null,
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/        // false,
            /*UserAckRequired*/         true);


            AddInstruction(
            /* Text*/                   "Connect to MAIN:\n Select the COM port and click \"Connect.\"",
            /* Desired Output*/         null,
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              true,
            /* Terminal*/               Processor.NONE,
            /* CheckBox*/               "MAIN Connected",
            /* ErrorText*/              "MAIN Terminal Not Connected",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/        // false,
            /*UserAckRequired*/         true);
        }


    }
}