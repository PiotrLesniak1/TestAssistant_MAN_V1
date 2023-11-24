using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestAssistant.Classes
{
    /*
     * File: SSR.cs
     * Author: Piotr Lesniak (Raytheon)
     * Date: 16/10/23
     * Description: Implementation of the SSR class, a subclass of the Test_Template class, responsible for executing SSR  tests on a ReplyProccessor board .
     *              This class defines test behaviors, including result checks in the main and boot terminals, SD card boot verification, test instruction management, and mismatch handling.
     * Dependencies: SSR class relies on the Test_Template base class to establish the test framework.
     * Usage: The SSR class is utilized within the TestAssistant application to perform SSR tests on a connected system.
     *        It also includes mechanisms to identify and address discrepancies in test outcomes, ensuring test reliability.
     */

    internal class SSR : Test_Template
    {
        // List of instructions specific to SSR
        protected internal static List<Instruction> instructions = new List<Instruction>();

        // Constructor for the SSR class
        public SSR(Terminal TerminalMAIN, Terminal TerminalBOOT)
           : base(TerminalMAIN, TerminalBOOT, instructions)
        {
            TestAttempts = 0; // Initialize the test attempts count
        }

        // Method to perform the instruction check for SSR
        public override async Task<bool> PerformInstructionCheck()
        {
            bool aquiredMutex = false;
            try
            {
                // Attempt to acquire the instruction check mutex
                aquiredMutex = instructionCheckMutex.WaitOne(0);

                if (!aquiredMutex)
                {
                    return false; // If mutex cannot be acquired, return false
                }

                var CurrentInstruction = TestInstructions[CurrentIndex]; // Store the current instruction for code readability

                if (!CurrentInstruction.CheckRequired)
                {
                    // If no check is required for this instruction, mark it as complete and return true
                    CurrentInstruction.CheckCompleted = true;
                }

                if (CurrentInstruction.DesiredOutput != null)
                {
                    if (CurrentInstruction.TerminalToCheck == Processor.MAIN)
                    {
                        // Check the main terminal for the desired output
                        if (CheckMainTerminal(CurrentInstruction.DesiredOutput))
                        {
                            if (CurrentInstruction.DesiredOutput == "Formal tests        B")
                            {
                                _TerminalMAIN.SendData("B");
                                _TerminalMAIN.SendData("B"); // Special handling for specific desired output
                            }
                            else if (CurrentInstruction.hasSpecialCheck)
                            {
                                CheckTestStatus(CurrentInstruction.DesiredOutput); // Perform a special check
                            }
                        }
                    }

                    if (CurrentInstruction.TerminalToCheck == Processor.BOOT)
                    {
                        if (CurrentInstruction.DesiredOutput == "MMCSD boot")
                        {
                            VerifyDevice(); // Verify loading from an SD card
                        }
                        // Check the Boot terminal for the desired output
                        CheckBootTerminal(CurrentInstruction.DesiredOutput);
                    }
                }

                // Check for main and boot connection
                if (CurrentInstruction.CheckMainConnected)
                {
                    return IsMainConnected();
                }

                if (CurrentInstruction.CheckBootConnected)
                {
                    return IsBootConnected();
                }

                // Perform any necessary mismatch count check
                MisMatchCount();

                return CurrentInstruction.CheckCompleted; // Return the result of instruction check
            }
            finally
            {
                if (aquiredMutex)
                {
                    instructionCheckMutex.ReleaseMutex(); // Release the mutex if acquired
                }
            }
        }

        // Method to check the test status for SSR
        private void CheckTestStatus(string desiredOutput)
        {
            foreach (var item in BufferMAIN)
            {
                // Check if the text in the buffer contains the desired output
                if (item.Text.Contains(desiredOutput))
                {
                    int index = item.Text.IndexOf(desiredOutput); // Find the index of the desired output in the text
                    string restOfString = item.Text.Substring(index + desiredOutput.Length); // Get the remaining text after the desired output

                    // Check if the remaining text indicates a successful test (PASS or (match))
                    if (restOfString.Contains("PASS") || restOfString.Contains("(match)"))
                    {
                        // Mark the current instruction as completed
                        TestInstructions[CurrentIndex].CheckCompleted = true;

                        // Store the actual output from the remaining text
                        TestInstructions[CurrentIndex].ActualOutput = restOfString;

                        // Add a test point outcome indicating a PASS
                        AddTestPointOutcome("SSR", "PASS");
                    }
                    else
                    {
                        // If the test status is not successful, store the error text
                        TestInstructions[CurrentIndex].ErrorText = desiredOutput + restOfString;

                        // Mark the current check as failed
                        CheckFailed("SSR Test point");
                    }
                }
            }
        }

        // Method to count and check for mismatches in the buffer
        private void MisMatchCount()
        {
            string LookingFor = "(MISMATCH)"; // Define the string pattern to look for mismatches

            // Count the occurrences of the pattern (MISMATCH) in the main buffer's text
            MismatchOccuranceCount = BufferMAIN.Sum(item => item.Text.Split(new[] { LookingFor }, StringSplitOptions.None).Length - 1);

            // Check if the number of mismatch occurrences exceeds a threshold (64 in this case)
            if (MismatchOccuranceCount > 64)
            {
                // If the threshold is exceeded, mark the check as failed with a message
                CheckFailed("MISMATCH");
            }
        }

        protected internal override void InitializeInstructions()
        {

            if (Globals.InhibitConnectionInstructions)
            {


            }
            else if (!Globals.InhibitConnectionInstructions)
            {
                AddConnectionInstructions();

            }

            
            AddInstruction(
            /* Text*/             "SSR REPLY - Ensure that the Azimuth Generator or a source of Azimuth is connected ",
            /* Desired Output*/   null,
            /* ActionRequired?*/  false,
            /* CheckBoot*/        false,
            /* CheckMain*/        false,
            /* Terminal*/         Processor.NONE,
            /* CheckBox*/         null,
            /* ErrorText*/        null,
            /* Actual Output*/    null,
            /*Special Check*/     false,
            /*UserAckRequired*/    true);
            

            
            

            AddInstruction(
            /* Text*/              "SSR REPLY - Load from MicroSD:\n At the front of the panel, press and hold S2. The amber LED will become solid.\n With S2 pressed, press and release S1. A green LED will flash next to the orange one.\n This indicates that the board will load from the MicroSD card.",
            /* Desired Output*/    "MMCSD boot",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.BOOT,
            /* CheckBox*/          "Loading from SD card",
            /* ErrorText*/         "NOT LOADING FROM SD",
            /* Actual Output*/     null,
            /*Special Check*/      false,
            /*UserAckRequired*/    true);
            ;
            AddInstruction(
            /* Text*/               "SSR REPLY - \nThe Test will now run . No input is required the test will automatically run ",
            /* Desired Output*/     "Main test menu:",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "Main Application ",
            /* ErrorText*/         "Main Not Loaded",
            /* Actual Output*/     null,
            /*Special Check*/      false,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/               "SSR REPLY -  Test Running .... \n\n  Choosing Formal Tests ",
            /* Desired Output*/     "Formal tests        B",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          null,
            /* ErrorText*/         "Main Not Loaded",
            /* Actual Output*/     null,
            /*Special Check*/      false,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/               "SSR REPLY -  Test Running .... \n\n  Waiting for Revolution 1 to start",
            /* Desired Output*/     "Passed north: now starting revolution 1",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "Rev 1",
            /* ErrorText*/         "Could not start frist revolution",
            /* Actual Output*/     null,
            /*Special Check*/      false,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/               "SSR REPLY -  Test Running .... \n\n  Waiting for Revolution 2 to start",
            /* Desired Output*/     "Passed north: now starting revolution 2",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "Rev 2",
            /* ErrorText*/         "2nd Revolution failed",
            /* Actual Output*/     null,
            /*Special Check*/      false,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/               "SSR REPLY -  Test Running .... \n\n  Waiting for Revolution 3 to start",
            /* Desired Output*/     "Passed north: now starting revolution 3",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "Rev 3",
            /* ErrorText*/         "Did not find test outcome",
            /* Actual Output*/     null,
            /*Special Check*/      false,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/               "SSR REPLY -  Test Running .... \n\n  Verifying value for 0x0100813e",
            /* Desired Output*/     "Written 0x0000 to 0x0100813e, read back 0x0000",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "REG : 0x0100813e",
            /* ErrorText*/         "Did not find test outcome",
            /* Actual Output*/     null,
            /*Special Check*/      true,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/               "SSR REPLY -  Test Running .... \n\n  Verifying value for 0x0100817e",
            /* Desired Output*/     "Written 0x0000 to 0x0100817e, read back 0x0000",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "REG : 0x0100817e",
            /* ErrorText*/         "Did not find test outcome",
            /* Actual Output*/     null,
            /*Special Check*/      true,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/               "SSR REPLY -  Test Running .... \n\n  Waiting for Confirmation of Successfull test ",
            /* Desired Output*/     "Test complete:",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "Test Complete",
            /* ErrorText*/         "Did not find test outcome",
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

        // Method to reset the SSR (Secondary Surveillance Radar) test
        public override void Reset_Test()
        {
            Reset(); // Call the base class method to reset the test

            ClearBuffers(); // Clear the data buffers to prepare for the next test

            IncAttempt(); // Increment the test attempt count

            _ = SaveTerminalLog("SSR"); // Save the terminal log specific to the SSR test
        }
    }
}