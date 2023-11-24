using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestAssistant.Classes
{
    /*
     * File: Firmware.cs
     * Author: Piotr Lesniak (Raytheon)
     * Date: 16/10/23
     * Description: This file contains the implementation of the Firmware class, which is a subclass of the Test_Template class.
     *              The Firmware class is responsible for performing firmware-related tests on a ReplyProccessor board . It defines the behavior of these tests,
     *               including checking the output in both the main and boot terminals, verifying device booting from an SD card, managing test instructions, and handling
     *              mismatches in test results.
     * Dependencies: This class depends on the Test_Template base class, which provides a framework for defining and executing tests.
     * Usage: The Firmware class is used within the TestAssistant application to execute firmware-related tests on a connected system.
     *        The class also includes logic to detect and handle mismatches in test results to ensure test integrity.
     */

    internal class Firmware : Test_Template
    {
        protected internal static List<Instruction> instructions = new List<Instruction>();

        public Firmware(Terminal TerminalMAIN, Terminal TerminalBOOT) : base(TerminalMAIN, TerminalBOOT, instructions)
        {
            TestAttempts = 0;
        }

        public override async Task<bool> PerformInstructionCheck()
        {
            bool aquiredMutex = false;
            try
            {
                aquiredMutex = instructionCheckMutex.WaitOne(0);
                if (!aquiredMutex)
                {
                    return false; // Return false if unable to acquire the mutex
                }

                var CurrentInstruction = TestInstructions[CurrentIndex]; // Store the current instruction for code readability

                if (!CurrentInstruction.CheckRequired)
                {
                    // If no check is required, mark as complete and return true
                    return CurrentInstruction.CheckCompleted = true;
                }

                if (CurrentInstruction.DesiredOutput != null)
                {
                    if (CurrentInstruction.TerminalToCheck == Processor.MAIN)
                    {
                        // Check the main terminal for the desired output
                        if (CheckMainTerminal(CurrentInstruction.DesiredOutput))
                        {
                            if (CurrentInstruction.DesiredOutput == "Formal tests        B" && CheckMainTerminal(CurrentInstruction.DesiredOutput))
                            {
                                _TerminalMAIN.SendData("B");
                                _TerminalMAIN.SendData("A");
                            }
                            else if (CurrentInstruction.hasSpecialCheck)
                            {
                                CheckTestStatus(CurrentInstruction.DesiredOutput);
                            }
                        }
                    }
                    if (CurrentInstruction.TerminalToCheck == Processor.BOOT)
                    {
                        if (CurrentInstruction.DesiredOutput == "MMCSD boot")
                        {
                            VerifyDevice(); // Verify we are loading from an SD
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

                MisMatchCount(); // Check for mismatches
                return CurrentInstruction.CheckCompleted;
            }
            finally
            {
                if (aquiredMutex)
                {
                    instructionCheckMutex.ReleaseMutex(); // Release the mutex if acquired
                }
            }
        }

        private void MisMatchCount()
        {
            // If the test rig is not reset properly, then the test may fail and produce mismatches for each memory location.
            // Therefore, keep a count and fail the test if we are getting a lot of mismatches.

            string LookingFor = "(MISMATCH)"; // The string to look for in the buffer

            // Count the occurrences of "(MISMATCH)" in the buffer text
            MismatchOccuranceCount = BufferMAIN.Sum(item => item.Text.Split(new[] { LookingFor }, StringSplitOptions.None).Length - 1);

            // If the mismatch count exceeds a threshold (e.g., 64), fail the test
            if (MismatchOccuranceCount > 64)
            {
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
            /* Text*/              "FirmWare - Load from MicroSD:\n At the front of the panel, press and hold S2. The amber LED will become solid.\n With S2 pressed, press and release S1. A green LED will flash next to the orange one.\n This indicates that the board will load from the MicroSD card.",
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
            /* Text*/              "FirmWare - Test Running.... \n\n Waiting for Main App to load",
            /* Desired Output*/     "Main test menu:",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "Main Application ",
            /* ErrorText*/         "Main Not Loaded",
            /* Actual Output*/     null,
            /*Special Check*/     false,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/              "FirmWare - Test Running.... \n\n   Choosing Formal Tests ",
            /* Desired Output*/    "Formal tests        B",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          null,
            /* ErrorText*/         "Main Not Loaded",
            /* Actual Output*/     null,
            /*Special Check*/     false,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/              "FirmWare - Test Running.... \n\n   Waiting for Trigger ROM Data outcome",
            /* Desired Output*/    "Trigger ROM Data",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "Trigger ROM",
            /* ErrorText*/         "Did not find test outcome",
            /* Actual Output*/     null,
            /*Special Check*/     true,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/              "FirmWare - Test Running.... \n\n   Waiting for Mode S STC Data outcome",
            /* Desired Output*/     "Mode S STC Data",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "Mode S STC Data",
            /* ErrorText*/         "Did not find test outcome",
            /* Actual Output*/     null,
            /*Special Check*/     true,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/              "FirmWare - Test Running.... \n\n   Waiting for SSR STC Data outcome",
            /* Desired Output*/     "SSR STC Data ",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "SSR STC Data",
            /* ErrorText*/         "Did not find test outcome",
            /* Actual Output*/     null,
            /*Special Check*/     true,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/              "FirmWare - Test Running.... \n\n   Waiting for Trigger RAM 1 Data outcome",
            /* Desired Output*/    "Trigger RAM 1 Data ",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "Trigger RAM 1",
            /* ErrorText*/         "Did not find test outcome",
            /* Actual Output*/     null,
            /*Special Check*/     true,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/              "FirmWare - Test Running.... \n\n   Waiting for Trigger RAM 2 Data outcome",
            /* Desired Output*/     "Trigger RAM 2 Data ",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          "Trigger RAM 2",
            /* ErrorText*/         "Did not find test outcome",
            /* Actual Output*/     null,
            /*Special Check*/     true,
            /*UserAckRequired*/    false);
            AddInstruction(
            /* Text*/              "FirmWare - Test Running.... \n\n   Waiting for Confirmation of Successfull test ",
            /* Desired Output*/    "Test Result: Success",
            /* ActionRequired?*/   true,
            /* CheckBoot*/         false,
            /* CheckMain*/         false,
            /* Terminal*/          Processor.MAIN,
            /* CheckBox*/          null,
            /* ErrorText*/         "Did not find test outcome",
            /* Actual Output*/     null,
            /*Special Check*/     false,
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
            /*Special Check*/     false,
            /*UserAckRequired*/    false);                            //LAST instruction is here to update the checkboxes , since they are checked off based on their instruction index +1 (only after we move passed the instruction)
        }

        private void AddInstruction(string text, string desiredOutputs, bool actionRequired, bool CheckBootConnected, bool CheckMainConnected, Processor terminal, string checkbox, string errortext, string actualoutput, bool specialcheck, bool userAck = false)
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

        private void CheckTestStatus(string desiredOutput)
        {
            foreach (var item in BufferMAIN)
            {
                // Check if the current buffer item contains the desired output
                if (item.Text.Contains(desiredOutput))
                {
                    // Find the index of the desired output within the text
                    int index = item.Text.IndexOf(desiredOutput);

                    // Get the part of the text after the desired output
                    string restOfString = item.Text.Substring(index + desiredOutput.Length);

                    // Check if the rest of the string contains "Correct" or "Success"
                    if (restOfString.Contains("Correct") || restOfString.Contains("Success"))
                    {
                        // If "Correct" or "Success" is found, mark the test point as passed
                        TestInstructions[CurrentIndex].ActualOutput = restOfString;
                        TestInstructions[CurrentIndex].CheckCompleted = true;
                        AddTestPointOutcome("Firmware", "PASS");
                    }
                    else
                    {
                        // If "Correct" or "Success" is not found, mark the test point as failed
                        TestInstructions[CurrentIndex].ErrorText = desiredOutput + restOfString;
                        CheckFailed("Firmware Test point ");
                    }
                }
            }
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
        public override void Reset_Test()
        {
            Reset(); // Reset test instructions

            ClearBuffers(); // Clear data buffers
            IncAttempt(); // Increment the test attempt counter
            _ = SaveTerminalLog("FirmWare"); // Save a log for firmware testing
        }
    }
}