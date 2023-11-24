using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace TestAssistant.Classes
{
    /*
     * File: NormalOP.cs
     * Author: Piotr Lesniak (Raytheon)
     * Date: 16/10/23
     * Description: Implementation of the NormalOP class, a subclass of the Test_Template class, responsible for executing normal operation tests on an RP Board .
     *              This class defines test behaviors, including result checks in the main and boot terminals, timing measurements, and error handling.
     * Dependencies: The NormalOP class relies on the Test_Template base class to establish the test framework.
     * Usage: The NormalOP class is utilized within the TestAssistant application to perform normal operation tests on an RP Board.
     *        It includes mechanisms to measure execution time and other checks to enssure that the Normal Operation follows a string set of instructions .
     */

    internal class NormalOP : Test_Template
    {
        // Define a list of instructions for this test class
        protected internal static List<Instruction> instructions = new List<Instruction>();

        // Declare private fields to track time
        private DateTime startTime;

        private DateTime FPGAtime;
        private DateTime endTime;

        // Constructor for the NormalOP class
        public NormalOP(Terminal TerminalMAIN, Terminal TerminalBOOT) : base(TerminalMAIN, TerminalBOOT, instructions)
        {
            // Initialize the number of test attempts to 0
            TestAttempts = 0;
        }

        public override async Task<bool> PerformInstructionCheck()
        {
            bool aquiredMutex = false;
            try
            {
                // Attempt to acquire the instructionCheckMutex with a timeout of 0 milliseconds
                aquiredMutex = instructionCheckMutex.WaitOne(0);
                if (!aquiredMutex)
                {
                    return false; // Return false if the mutex could not be acquired
                }

                var CurrentInstruction = TestInstructions[CurrentIndex]; // Retrieve the current instruction for better code readability

                if (!CurrentInstruction.CheckRequired)
                {
                    // If no check is required, mark the instruction as completed and return true
                    return CurrentInstruction.CheckCompleted = true;
                }

                if (CurrentInstruction.DesiredOutput != null)
                {
                    if (CurrentInstruction.TerminalToCheck == Processor.MAIN)
                    {
                        // Check the MAIN terminal for the desired output
                        CheckMainTerminal(CurrentInstruction.DesiredOutput);
                    }

                    if (CurrentInstruction.TerminalToCheck == Processor.BOOT)
                    {
                        if (CurrentInstruction.DesiredOutput == "QSPI boot")
                        {
                            // If QSPI boot string is found, the test has begun; save the system time
                            VerifyDevice();
                            startTime = DateTime.Now;
                        }

                        if (CheckBootTerminal(CurrentInstruction.DesiredOutput))
                        {
                            if (CurrentInstruction.DesiredOutput == "FPGA programming complete")
                            {
                                // Measure time elapsed to FPGA programming completion
                                FPGAtime = DateTime.Now;
                                TimeSpan executionTimeToFPGA = FPGAtime - startTime;

                                CurrentInstruction.ActualOutput = $"Execution Time : {executionTimeToFPGA.Seconds} s";
                                AddTestPointOutcome("NORMAL_OP(A)", "Additional");
                            }
                            else if (CurrentInstruction.DesiredOutput == "Passing execution control to application...")
                            {
                                // Measure time elapsed to passing execution control to application
                                endTime = DateTime.Now;
                                TimeSpan executionTime = endTime - startTime;

                                CurrentInstruction.ActualOutput = $"Execution Time : {executionTime.Seconds} s";
                                AddTestPointOutcome("NORMAL_OP(A)", "Additional");
                            }
                        }

                        if (CurrentInstruction.hasSpecialCheck)
                        {
                            if (CurrentInstruction.DesiredOutput == "Done! Bytes copied:")
                            {
                                // Purge bytes copied to prevent misinterpretation in later instructions
                                string BytesCopied = OutputToString(BufferBOOT, "Done! Bytes copied:");

                                if (string.IsNullOrEmpty(BytesCopied))
                                {
                                    // Report failure if unable to hide bytes
                                    InstructionFailed();
                                    return CurrentInstruction.CheckCompleted;
                                }

                                try
                                {
                                    bool successful = await HideBufferInputBool(BytesCopied, CurrentInstruction.TerminalToCheck);

                                    if (successful)
                                    {
                                        // Save the bytes copied to actual output and retrieve previous instruction's desired output
                                        CurrentInstruction.ActualOutput = BytesCopied;
                                        CurrentInstruction.DesiredOutput = instructions[CurrentIndex - 1].DesiredOutput;
                                        CurrentInstruction.CheckCompleted = true;

                                        // Save the test point outcome
                                        AddTestPointOutcome("NORMAL_OP", "PASS");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Handle an error if hiding the bytes fails
                                    
                                    LogErrorMessage($"Failed to conceal total bytes in Buffer. \n {ex.Message}");
                                }
                            }
                        }
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

                // Check if there are any errors
                IsErrorPresent();

                return CurrentInstruction.CheckCompleted;
            }
            finally
            {
                if (aquiredMutex)
                {
                    instructionCheckMutex.ReleaseMutex();
                }
            }
        }

        protected internal override void VerifyDevice()
        {
            // Check if the desired output of the current instruction is not found in the BOOT terminal
            if (!CheckBootTerminal(TestInstructions[CurrentIndex].DesiredOutput))
            {
                // Check if either "Initialising MMC/SD device" or "USBMSC boot" strings are found in the BOOT terminal
                if (CheckBootTerminal("Initialising MMC/SD device") || CheckBootTerminal("USBMSC boot"))
                {
                    CheckFailed("LOAD FILE ERROR");
                }
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
            /* Text*/                   "NORMAL OP - With a Pin Press the reset button",
            /* Desired Output*/         "QSPI boot",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "Loading from QSPI BOOT",
            /* ErrorText*/              "Loading from QSPI",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*UserAckRequired*/         true);
            AddInstruction(
            /* Text*/                   "NORMAL OP - Test Running.... \n\n Waiting for Copying confirmation ",
            /* Desired Output*/         "Copying BOOT processor TBL from QSPI flash device to internal RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "ERROR COPYING BOOT TBL",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "NORMAL OP - Test Running.... \n\n Obtaining the total Bytes ",
            /* Desired Output*/         "Done! Bytes copied:",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               " BOOT TBL",
            /* ErrorText*/              " ERROR COPYING BOOT TBL ",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "NORMAL OP - Test Running.... \n\n Waiting for Copying confirmation ",
            /* Desired Output*/         "Copying FPGA bitfile from QSPI flash device to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "ERROR COPYING FPGA",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "NORMAL OP - Test Running.... \n\n Waiting for FPGA programming to complete",
            /* Desired Output*/         "FPGA programming complete",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "FPGA",
            /* ErrorText*/              "ERROR COPYING FPGA",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "NORMAL OP - Test Running.... \n\n Waiting for Copying confirmation ",
            /* Desired Output*/         "Copying MAIN processor SBL from QSPI flash device to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               " MAIN SBL",
            /* ErrorText*/              "NOT LOADING FROM SD",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "NORMAL OP - Test Running.... \n\n Obtaining the total Bytes ",
            /* Desired Output*/         "Done! Bytes copied:",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              " Failed ",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "NORMAL OP - Test Running.... \n\n Waiting for Copying confirmation ",
            /* Desired Output*/         "Copying MAIN processor application from QSPI flash device to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "Copying MAIN APP",
            /* ErrorText*/              "NOT LOADING FROM SD",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*UserAckRequired*/         false);
            AddInstruction(
           /* Text*/                   "NORMAL OP - Test Running.... \n\n Obtaining the total Bytes ",
           /* Desired Output*/         "Done! Bytes copied:",
           /* ActionRequired?*/        true,
           /* CheckBoot*/              false,
           /* CheckMain*/              false,
           /* Terminal*/               Processor.BOOT,
           /* CheckBox*/               null,
           /* ErrorText*/              " Failed ",
           /* Actual Output*/          null,
           /*Special Check*/           true,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "NORMAL OP - Test Running.... \n\n Waiting for Copying confirmation ",
            /* Desired Output*/         "Copying BOOT processor application from QSPI flash device to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "Copying BOOT APP",
            /* ErrorText*/              "NOT LOADING FROM SD",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "NORMAL OP - Test Running.... \n\n Obtaining the total Bytes ",
            /* Desired Output*/         "Done! Bytes copied:",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              " Failed ",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "NORMAL OP - Test Running.... \n\n  Waiting for BOOT to hand over control to main ",
            /* Desired Output*/         "Passing execution control to application...",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "Passing to MAIN",
            /* ErrorText*/              "NOT LOADING FROM SD",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "",
            /* Desired Output*/         null,
            /* ActionRequired?*/        false,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.NONE,
            /* CheckBox*/               null,
            /* ErrorText*/              null,
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*UserAckRequired*/         false);                           //LAST instruction is here to update the checkboxes , since they are checked off based on their instruction index +1 (only after we move passed the instruction)
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
        public override void Reset_Test()
        {
            // Reset internal state of the test
            Reset();

            // Clear any internal buffers
            ClearBuffers();

            // Increment the test attempt count
            IncAttempt();

            // Save the terminal log for the "NORMAL_OP" test
            _ = SaveTerminalLog("NORMAL_OP");
        }
    }
}