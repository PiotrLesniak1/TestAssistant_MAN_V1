using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace TestAssistant.Classes
{
    /*
     * File: Commissioning.cs
     * Author: Piotr Lesniak (Raytheon)
     * Date: 16/10/23
     * Description: This file contains the implementation of the Commissioning class, responsible for performing tests related to the commissioning of a hardware device.
     *              The class extends the Test_Template class and includes methods for checking various instructions related to commissioning. It handles terminal interactions,
     *              verifies desired outputs, and performs special checks as needed during the commissioning process.
     * Dependencies: This class relies on the Test_Template class and other related classes and components for its functionality.
     * Usage: The Commissioning class is used within the TestAssistant application to conduct commissioning tests on a hardware device, including tasks such as verifying file existence,
     *        copying files from update media to external RAM, and ensuring proper boot procedures. It provides critical functionality for ensuring the device is correctly configured
     *        and operational after commissioning.
     */

    internal class Commissioning_usb : Test_Template
    {
        // Create a list to store instruction objects.
        protected internal static List<Instruction> instructions = new List<Instruction>();

        // Constructor for the Commissioning class.
        // Parameters:
        //   TerminalMAIN (Terminal): The main terminal for commissioning.
        //   TerminalBOOT (Terminal): The boot terminal for commissioning.
        public Commissioning_usb(Terminal TerminalMAIN, Terminal TerminalBOOT) : base(TerminalMAIN, TerminalBOOT, instructions)
        {
            // Initialize the number of test attempts.
            TestAttempts = 0;
            // Additional initialization can be done here if needed.
        }

        public override async Task<bool> PerformInstructionCheck()
        {
            bool aquiredMutex = false;
            try
            {
                aquiredMutex = instructionCheckMutex.WaitOne(0);
                if (!aquiredMutex)
                {
                    return false;
                }

                // Create a local variable for the current instruction for better code readability.
                var CurrentInstruction = TestInstructions[CurrentIndex];

                // Check if the current instruction requires a check.
                if (!CurrentInstruction.CheckRequired)
                {
                    return CurrentInstruction.CheckCompleted = true; // If no check is required, mark it as complete and return true.
                }

                // Check if a desired output is specified for the current instruction.
                if (CurrentInstruction.DesiredOutput != null)
                {
                    // Update the desired output for specific cases.
                    if (TestInstructions[CurrentIndex].DesiredOutput == "Copied bytes: 0  of  0")
                    {
                        TestInstructions[CurrentIndex].DesiredOutput = $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}";
                    }

                    // Check the terminal to verify the desired output.
                    if (CurrentInstruction.TerminalToCheck == Processor.BOOT)
                    {
                        if (CurrentInstruction.DesiredOutput == "MMCSD boot")
                        {
                            VerifyDevice(); // Verify that we are loading from an SD.
                        }
                        if (CurrentInstruction.DesiredOutput == "Please enter 'y' to proceed with update...")
                        {
                            InitialBufferPurge(); // Perform a unique buffer purge for test initialization.
                        }
                        if (CurrentInstruction.DesiredOutput == $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}")
                        {
                            // Check if the boot terminal matches the desired output.
                            if (CheckBootTerminal(CurrentInstruction.DesiredOutput))
                            {
                                bool successful = await HideBufferInputBool(CurrentInstruction.DesiredOutput, CurrentInstruction.TerminalToCheck);
                                if (successful)
                                {
                                    if (!CurrentInstruction.isSecondLocation)
                                    {
                                        CurrentInstruction.ActualOutput = "Successfully written to Primary location";
                                        AddTestPointOutcome("Commission (USB)", "PASS");
                                    }

                                    if (CurrentInstruction.isSecondLocation)
                                    {
                                        CurrentInstruction.ActualOutput = "Successfully written to Secondary location ";
                                        AddTestPointOutcome("Commission (USB)", "PASS");
                                    }
                                }
                            }
                        }
                        else if (CurrentInstruction.DesiredOutput == "Image update successful")
                        {
                            if (CheckBootTerminal(CurrentInstruction.DesiredOutput))
                            {
                                bool successful = await HideBufferInputBool(CurrentInstruction.DesiredOutput, CurrentInstruction.TerminalToCheck);
                                if (successful)
                                {
                                    CurrentInstruction.ActualOutput = "NO ERRORS";
                                    AddTestPointOutcome("Commission (USB)", "PASS");
                                    bytesToCopy = 0; // Reset the bytes here.
                                }
                            }
                        }

                        if (CurrentInstruction.hasSpecialCheck)
                        {
                            // Check if the terminal output matches specific special checks.
                            if (CheckBootTerminal(CurrentInstruction.DesiredOutput))
                            {
                                switch (CurrentInstruction.DesiredOutput)
                                {
                                    case "Looking for file '/mlo_boot'":
                                        CurrentInstruction.CheckCompleted = HasFileBeenFound(CurrentInstruction.DesiredOutput, "Commission (USB)");
                                        break;

                                    case "Looking for file '/tbl_boot'":
                                        CurrentInstruction.CheckCompleted = HasFileBeenFound(CurrentInstruction.DesiredOutput, "Commission (USB)");
                                        break;

                                    case "Looking for file '/mlo_main'":
                                        CurrentInstruction.CheckCompleted = HasFileBeenFound(CurrentInstruction.DesiredOutput, "Commission (USB)");
                                        break;

                                    case "Looking for file '/upd_usd'":
                                        CurrentInstruction.CheckCompleted = HasFileBeenFound(CurrentInstruction.DesiredOutput, "Commission (USB)");
                                        break;

                                    case "Looking for file '/upd_usb'":
                                        CurrentInstruction.CheckCompleted = HasFileBeenFound(CurrentInstruction.DesiredOutput, "Commission (USB)");
                                        break;

                                    case "Looking for file '/bitfile'":
                                        CurrentInstruction.CheckCompleted = HasFileBeenFound(CurrentInstruction.DesiredOutput, "Commission (USB)");
                                        break;

                                    case "Looking for file '/app_boot'":
                                        CurrentInstruction.CheckCompleted = HasFileBeenFound(CurrentInstruction.DesiredOutput, "Commission (USB)");
                                        break;

                                    case "Looking for file '/app_main'":
                                        CurrentInstruction.CheckCompleted = HasFileBeenFound(CurrentInstruction.DesiredOutput, "Commission (USB)");
                                        break;
                                }

                                if (CurrentInstruction.DesiredOutput == "Copying file from update media to external RAM")
                                {   // We have began coying a file lets retrive the amount of bytes that will be copied
                                    if (TotalBytesCopied()) // Number of bytes will be read and purged here
                                    {
                                        CurrentInstruction.CheckCompleted = await HideBufferInputBool(CurrentInstruction.DesiredOutput, CurrentInstruction.TerminalToCheck); // Hiding Copying File from update media .... as to not mistake this input for another file .
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If no special check is required, perform a regular terminal output check.
                            CheckBootTerminal(CurrentInstruction.DesiredOutput);
                        }
                    }
                }

                // Check for main and boot connection.
                if (CurrentInstruction.CheckMainConnected)
                {
                    return IsMainConnected();
                }
                if (CurrentInstruction.CheckBootConnected)
                {
                    return IsBootConnected();
                }

                IsErrorPresent(); // Check if there are any errors.
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

        private async void InitialBufferPurge()
        {
            try
            {
                // When the Processor board is booted up, it initially loads some bytes.
                // We need to purge this from the buffer so that later when we are checking
                // the size of the file, we are not accidentally reading the wrong value.

                // This method is responsible for conditioning the buffer for the test to begin.
                // If successful, it will send 'y' to proceed with the test.

                // Retrieve the initial bytes copied from USB.
                string tempString = OutputToString(BufferBOOT, "Done! Bytes copied:");

                if (string.IsNullOrEmpty(tempString))
                {
                    // If the retrieved string is null or empty, exit this method.
                    return;
                }

                // Hide the initial bytes copied from the buffer.
                await HideBufferInputBool(tempString, TestInstructions[CurrentIndex].TerminalToCheck);

                if (!CheckBootTerminal(tempString))
                {
                    // If we can no longer see the string in the buffer, mark the current instruction as completed.
                    TestInstructions[CurrentIndex].CheckCompleted = true;

                    // Send 'y' to proceed with the test.
                    _TerminalBOOT.SendData("y");
                }
                else
                {
                    // If the purge failed, mark the current instruction as incomplete.
                    TestInstructions[CurrentIndex].CheckCompleted = false;

                    // Show an error message.

                    LogErrorMessage("Failed to Purge Buffer.");
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message);

                TestInstructions[CurrentIndex].CheckCompleted = false;

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
            /* Text*/                   "Commission (USB) - Load from MicroSD:\n At the front of the panel, press and hold S2. The amber LED will become solid.\n With S2 pressed, press and release S1. A green LED will flash next to the orange one.\n This indicates that the board will load from the MicroSD card.",
            /* Desired Output*/         "MMCSD boot",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "Loading from SD",
            /* ErrorText*/              "NOT LOADING FROM SD",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         true);

            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running.... \n\n  The Test will now run . No input is required the test will automatically run ",
            /* Desired Output*/         "Please enter 'y' to proceed with update...",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "FAILED TO RESPOND",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Looking for mlo_boot ",
            /* Desired Output*/         "Looking for file '/mlo_boot'",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Could not find MLO_BOOT",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Coping Confirmation  ",
            /* Desired Output*/         "Copying file from update media to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Copying Failed",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation of successful image update  ",
            /* Desired Output*/         "Image update successful",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "MLO - BOOT",
            /* ErrorText*/              "Failed to update image",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Looking for tbl_boot",
            /* Desired Output*/         "Looking for file '/tbl_boot'",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Could not find TBL_BOOT",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Coping Confirmation  ",
            /* Desired Output*/         "Copying file from update media to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Copying Failed",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to primary ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to secondary loaction  ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         true,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation of successful image update  ",
            /* Desired Output*/         "Image update successful",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "TBL - BOOT",
            /* ErrorText*/              "Failed to update image",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Looking for mlo_main ",
            /* Desired Output*/         "Looking for file '/mlo_main'",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "could not find MLO_MAIN",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Coping Confirmation  ",
            /* Desired Output*/         "Copying file from update media to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/                Processor.BOOT,
            /* CheckBox*/                null,
            /* ErrorText*/              "Copying Failed",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to primary ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to secondary loaction  ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         true,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation of successful image update  ",
            /* Desired Output*/         "Image update successful",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "MLO - MAIN",
            /* ErrorText*/              "Failed to update image ",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Looking for upd_usd ",
            /* Desired Output*/         "Looking for file '/upd_usd'",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "could not find UPD_USD",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Coping Confirmation  ",
            /* Desired Output*/         "Copying file from update media to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Copying Failed",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to primary ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to secondary loaction  ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         true,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation of successful image update  ",
            /* Desired Output*/         "Image update successful",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "UPD - USD",
            /* ErrorText*/              "Failed to update image ",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Looking for upd_usb ",
            /* Desired Output*/         "Looking for file '/upd_usb'",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "could not find UPD_USB",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Coping Confirmation  ",
            /* Desired Output*/         "Copying file from update media to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Copying Failed",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to primary  ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to secondary loaction  ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         true,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation of successful image update  ",
            /* Desired Output*/         "Image update successful",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "UPD - USB",
            /* ErrorText*/              "Failed to update image ",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Looking for bitfile ",
            /* Desired Output*/         "Looking for file '/bitfile'",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "could not find Bitfile",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
             /* Text*/                  "Commission (USB) - Test Running .... \n\n Waiting for Coping Confirmation  ",
            /* Desired Output*/         "Copying file from update media to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Copying Failed",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to primary  ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to secondary loaction  ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         true,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                  "Commission (USB) - Test Running .... \n\n Waiting for Confirmation of successful image update  ",
            /* Desired Output*/        "Image update successful",
            /* ActionRequired?*/       true,
            /* CheckBoot*/             false,
            /* CheckMain*/             false,
            /* Terminal*/              Processor.BOOT,
            /* CheckBox*/              "Bitfile",
            /* ErrorText*/             "Failed to update image ",
            /* Actual Output*/         null,
            /*Special Check*/          false,
            /*Second Location*/        false,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Looking for app_boot ",
            /* Desired Output*/         "Looking for file '/app_boot'",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "could not find app_boot",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Coping Confirmation  ",
            /* Desired Output*/         "Copying file from update media to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Copying Failed",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to primary ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to secondary loaction  ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         true,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation of successful image update  ",
            /* Desired Output*/         "Image update successful",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "APP - BOOT",
            /* ErrorText*/              "Failed to update image ",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Looking for app_main ",
            /* Desired Output*/         "Looking for file '/app_main'",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "could not find APP_MAIN",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Coping Confirmation  ",
            /* Desired Output*/         "Copying file from update media to external RAM",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Copying Failed",
            /* Actual Output*/          null,
            /*Special Check*/           true,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to primary ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation that all bytes are copied to secondary loaction  ",
            /* Desired Output*/         $"Copied bytes: {bytesToCopy}  of  {bytesToCopy}",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               null,
            /* ErrorText*/              "Cannot find desired output",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         true,
            /*UserAckRequired*/         false);
            AddInstruction(
            /* Text*/                   "Commission (USB) - Test Running .... \n\n Waiting for Confirmation of successful image update  ",
            /* Desired Output*/         "Image update successful",
            /* ActionRequired?*/        true,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.BOOT,
            /* CheckBox*/               "APP - MAIN",
            /* ErrorText*/              "Failed to update image ",
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);

            AddInstruction(
            /* Text*/       "",
            /* Desired Output*/         null,
            /* ActionRequired?*/        false,
            /* CheckBoot*/              false,
            /* CheckMain*/              false,
            /* Terminal*/               Processor.NONE,
            /* CheckBox*/               null,
            /* ErrorText*/              null,
            /* Actual Output*/          null,
            /*Special Check*/           false,
            /*Second Location*/         false,
            /*UserAckRequired*/         false);
            //LAST instruction is here to update the checkboxes , since they are checked off based on their instruction index +1 (only after we move passed the instruction)
        }

        private void AddInstruction(string text, string desiredOutputs, bool actionRequired, bool CheckBootConnected, bool CheckMainConnected, Processor terminal, string checkbox, string errortext, string actualoutput, bool specialcheck, bool secondaryLocation, bool userAck = false)
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
                isSecondLocation = secondaryLocation,
                userAckRequired = userAck
            });
        }

        public override void Reset_Test()
        {
            // Reset the test instructions and current index.
            Reset();

            // Clear any data buffers.
            ClearBuffers();

            // Increment the test attempt counter.
            IncAttempt();

            // Save the terminal log for debugging or record-keeping purposes.
            _ = SaveTerminalLog("Commission (USB)");
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
            /*Second Location*/         false,
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
            /*Second Location*/         false,
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
            /*Second Location*/         false,
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
            /*Second Location*/         false,
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
            /*Second Location*/         false,
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
            /*Second Location*/         false,
            /*UserAckRequired*/         true);
        }
    }
}