using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestAssistant.Classes
{
    /// ADC_Test class handles tests related to the Analog-to-Digital Converter (ADC) functionality.
    internal class ADC_Test : Test_Template
    {
        /*
         * File: ADC_Test.cs
         * Author: Piotr Lesniak (Raytheon)
         * Date: 16/10/23
         * Description: This file contains the implementation of the ADC_Test class, which is responsible for performing tests related to the Analog-to-Digital Converter (ADC) functionality.
         *              The class extends the Test_Template class and includes methods for checking various instructions related to ADC testing. It also defines specific ADC-related checks and instructions for verifying voltage values.
         * Dependencies: This class relies on the Test_Template class and other related classes and components for its functionality.
         * Usage: This class is used within the TestAssistant application to conduct ADC tests on a hardware device, specifically focusing on MicroSD card detection, JTAG cable connections, loading from MicroSD, and verifying ADC voltage values.
         */

        // List to store test instructions
        protected internal static List<Instruction> instructions = new List<Instruction>();

        // Constructor
        public ADC_Test(Terminal TerminalMAIN, Terminal TerminalBOOT) : base(TerminalMAIN, TerminalBOOT, instructions)
        {
            TestAttempts = 0;
        }

        // Method to read numeric value from input string and check if it is within a specified range
        protected bool ReadNum_FromString(string input, int val)
        {
            // Use regular expression to match numeric part
            string numericValue = Regex.Match(input, @"voltage:\s*(\d+)").Groups[1].Value;

            // Convert the numeric value to an integer
            int value = int.Parse(numericValue);

            int minRange = 0;
            int maxRange = 0;
            if (val == 2)
            {
                minRange = 890;
                maxRange = 910;
            }
            else if (val == 1)
            {
                minRange = 1215;
                maxRange = 1235;
            }

            // Set the actual output for the current instruction
            TestInstructions[CurrentIndex].ActualOutput = input;
            // Check if the value is within the specified range
            bool isInRange = value >= minRange && value <= maxRange;
            return isInRange;
        }

        // Method to perform instruction check asynchronously
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

                // Get the current instruction
                var CurrentInstruction = TestInstructions[CurrentIndex];

                // Check if the instruction requires any action
                if (!CurrentInstruction.CheckRequired)
                {
                    // Mark as complete and return true if no check is required
                    CurrentInstruction.CheckCompleted = true;
                    return true;
                }

                if (CurrentInstruction.DesiredOutput != null)
                {
                    // Check desired output based on the terminal type
                    if (CurrentInstruction.TerminalToCheck == Processor.MAIN)
                    {
                        // Check the Main terminal for the desired output
                        CheckMainTerminal(CurrentInstruction.DesiredOutput);
                    }
                    if (CurrentInstruction.TerminalToCheck == Processor.BOOT)
                    {
                        if (CurrentInstruction.DesiredOutput == "MMCSD boot")
                        {
                            // Verify loading from an SD card
                            VerifyDevice();
                        }
                        // Check the Boot terminal for the desired output
                        CheckBootTerminal(CurrentInstruction.DesiredOutput);
                    }
                }
                if(CurrentInstruction.CheckPowerOn)
                {
                    // Check for main and boot connection
                    if (CurrentInstruction.CheckMainConnected)
                    {
                       
                        return IsMainConnected();
                    }
                    if (CurrentInstruction.CheckBootConnected)
                    {
                        

                        return IsBootConnected();
                    }
                

                    await Task.Delay(TimeSpan.FromSeconds(5));
                    LogErrorMessage("CHECK POWER ON !!!");
                    // Wait for 5 seconds and if no input is detected via the serial port the Board is not ON 
                    if (BufferBOOT.Count < 1)
                    {

                        LogErrorMessage("POWER NOT ON !!!");
                    }
                    else
                    {
                        LogErrorMessage("POWER ON !!!");
                    }
                    
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

        // Method to check main terminal for specific desired output
        protected internal override bool CheckMainTerminal(string desiredOutput)
        {
            // This method is unique to the test and performs checks on the two critical ADC values that will indicate the outcome of the test

            if (TestInstructions[CurrentIndex].CheckCompleted != true)
            {
                // If the current check has not been failed, proceed to look for the output
                if (desiredOutput == "ADC0 AIN6 voltage: 1225mV")
                {
                    if (CheckOutPutBuffer(BufferMAIN, "ADC0 AIN6 voltage:"))
                    {
                        if (ReadNum_FromString(OutputToString(BufferMAIN, "ADC0 AIN6 voltage:"), 1))
                        {
                            // Check passed for ADC0 AIN6
                            AddTestPointOutcome("ADC", "PASS");
                            return InstructionPassed();
                        }
                        else
                        {
                            // Check failed for ADC0 AIN6
                            CheckFailed("ADC0 ANI6");
                        }
                    }
                }
                else if (desiredOutput == "ADC1 AIN5 voltage: 900mV")
                {
                    if (CheckOutPutBuffer(BufferMAIN, "ADC1 AIN5 voltage:"))
                    {
                        if (ReadNum_FromString(OutputToString(BufferMAIN, "ADC1 AIN5 voltage:"), 2))
                        {
                            // Check passed for ADC1 AIN5
                            AddTestPointOutcome("ADC", "PASS");
                            return InstructionPassed();
                        }
                        else
                        {
                            // Check failed for ADC1 AIN5
                            CheckFailed("ADC1 ANI5");
                        }
                    }
                }
                else if (desiredOutput == "Enter any key...")
                {
                    // Send Enter key command to Main Terminal
                    _TerminalMAIN.SendData("/r/n");
                    return InstructionPassed();
                }

                // Check for other desired output
                if (CheckOutPutBuffer(BufferMAIN, desiredOutput))
                {
                    return InstructionPassed();
                }
                else
                {
                    return InstructionFailed();
                }
            }
            else
            {
                // Check has been failed previously
                return false;
            }
        }

        // Method to add instruction to the instructions list
        private void AddInstruction(string text, string desiredOutputs, bool actionRequired, bool CheckBootConnected, bool CheckMainConnected, Processor terminal, string checkbox, string errortext, string actualoutput, bool specialcheck, bool userAck = false, bool PowerCheck = false)
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
                userAckRequired = userAck,
                CheckPowerOn = PowerCheck
            });
        }

        protected internal override void InitializeInstructions()
        {
            // Method to Initilize pre defined instructions for this test , it will populate the instructions list


            if (Globals.InhibitConnectionInstructions)
            {
                
                
            }
            else if (!Globals.InhibitConnectionInstructions)
            {
                AddConnectionInstructions();
                
            }
            AddInstruction(
            /* Text*/              "ADC - Load from MicroSD:\n At the front of the panel, press and hold S2. The amber LED will become solid.\n With S2 pressed, press and release S1. A green LED will flash next to the orange one.\n This indicates that the board will load from the MicroSD card.",
            /* Desired Output*/     "MMCSD boot",
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
            /* Text*/              "ADC - Test Running.... \n\n Waiting for Main App to load",
            /* Desired Output*/     "ADC_Example_main application!",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          $"Main app",
            /* ErrorText*/         "Could not find ADC_Example_main application!",
            /* Actual Output*/     null,
            /*Special Check*/      false,
            /*UserAckRequired*/    false);

            AddInstruction(
            /* Text*/              "ADC - Test Running.... \n\n ",
            /* Desired Output*/    "Enter any key...",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          null,
            /* ErrorText*/         "Could Not find Enter any key...",
            /* Actual Output*/     null,
            /*Special Check*/     false,
            /*UserAckRequired*/    false);

            AddInstruction(
            /* Text*/              "ADC - Test Running.... \n\n Verifying ADC0 AIN6 value ",
            /* Desired Output*/    "ADC0 AIN6 voltage: 1225mV",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          $"ADC0 AIN6",
            /* ErrorText*/         "Could Not find ADC0 AIN6 voltage:",
            /* Actual Output*/     null,
            /*Special Check*/     false,
            /*UserAckRequired*/    false);

            AddInstruction(
            /* Text*/              "ADC - Test Running.... \n\n Verifying ADC1 AIN5 value ",
            /* Desired Output*/     "ADC1 AIN5 voltage: 900mV",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          $"ADC1 AIN5",
            /* ErrorText*/         "Could Not find desired ADC1 AIN5 voltage:",
            /* Actual Output*/     null,
            /*Special Check*/     false,
            /*UserAckRequired*/    false);

            AddInstruction(
            /* Text*/              "",
            /* Desired Output*/    null,
            /* ActionRequired?*/   false,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.NONE,
            /* CheckBox*/          null,
            /* ErrorText*/         null,
            /* Actual Output*/     null,
            /*Special Check*/     false,
            /*UserAckRequired*/    false);                           //LAST instruction is here to update the checkboxes , since they are checked off based on their instruction index +1 (only after we move passed the instruction)
        }

        public override void Reset_Test()
        {
            Reset();

            ClearBuffers();
            IncAttempt();
            _ = SaveTerminalLog("ADC");
        }





        //////////////////////////////auto instructions 
        //


        private void AddConnectionInstructions()
        {
            


                AddInstruction(
                /* Text*/              "ADC - JTAG Cable Connection:\n Connect the USB end of your JTAG cable to your PC.",
                /* Desired Output*/    null,
                /* ActionRequired?*/   false,
                /* CheckBoot*/         false,
                /* CheckMain*/         false,
                /* Terminal*/          Processor.NONE,
                /* CheckBox*/          null,
                /* ErrorText*/         null,
                /* Actual Output*/     null,
                /*Special Check*/     false,
                /*UserAckRequired*/    true);

                AddInstruction(
                /* Text*/              "ADC - Connect JTAG to BOOT Processor:\n Connect the JTAG end of the cable to the BOOT Processor Slot (JTAG 1).",
                /* Desired Output*/    null,
                /* ActionRequired?*/   false,
                /* CheckBoot*/         false,
                /* CheckMain*/         false,
                /* Terminal*/          Processor.NONE,
                /* CheckBox*/          null,
                /* ErrorText*/         null,
                /* Actual Output*/     null,
                /*Special Check*/     false,
                /*UserAckRequired*/    true);

                AddInstruction(
                /* Text*/              "ADC - Connect to BOOT:\n Select the COM port and click \"Connect.\"",
                /* Desired Output*/    null,
                /* ActionRequired?*/   true,
                /* CheckBoot*/         true,
                /* CheckMain*/         false,
                /* Terminal*/          Processor.NONE,
                /* CheckBox*/          "BOOT Connected",
                /* ErrorText*/         "BOOT Terminal Not Connected",
                /* Actual Output*/     null,
                /*Special Check*/      false,
                /*UserAckRequired*/    true);

                AddInstruction(
                /* Text*/              "ADC - Repeat for MAIN Processor:\n Repeat for the MAIN Processor Slot (JTAG 2).",
                /* Desired Output*/    null,
                /* ActionRequired?*/   false,
                /* CheckBoot*/         false,
                /* CheckMain*/         false,
                /* Terminal*/          Processor.NONE,
                /* CheckBox*/          null,
                /* ErrorText*/         null,
                /* Actual Output*/     null,
                /*Special Check*/     false,
                /*UserAckRequired*/    true);

                AddInstruction(
                /* Text*/              "ADC - Connect JTAG to MAIN Processor:\n Connect the JTAG end of the cable to the BOOT Processor Slot (JTAG 1).",
                /* Desired Output*/    null,
                /* ActionRequired?*/   false,
                /* CheckBoot*/         false,
                /* CheckMain*/         false,
                /* Terminal*/          Processor.NONE,
                /* CheckBox*/          null,
                /* ErrorText*/         null,
                /* Actual Output*/     null,
                /*Special Check*/     false,
                /*UserAckRequired*/    true);

                AddInstruction(
                /* Text*/              "ADC - Connect to MAIN:\n Select the COM port and click \"Connect.\"",
                /* Desired Output*/    null,
                /* ActionRequired?*/   true,
                /* CheckBoot*/         false,
                /* CheckMain*/         true,
                /* Terminal*/          Processor.NONE,
                /* ErrorText*/         "MAIN Connected",
                /* ErrorText*/         "MAIN Terminal Not Connected",
                /* Actual Output*/     null,
                /*Special Check*/      false,
                /*UserAckRequired*/    true);
            
        }

    }
}