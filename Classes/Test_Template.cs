using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TestAssistant.Classes
{
    /*
    * File: Test_Template.cs
    * Author: Piotr Lesniak (Raytheon)
    * Date: 16/10/23
    * Description: This file contains the implementation of the Test_Template class, which serves as the base class for various test classes within the TestAssistant application.
    *               The Test_Template class provides a framework for performing tests, managing test instructions, and handling common testing functionalities.
    * Dependencies: This class may be extended by other test-specific classes and relies on components such as Terminals, mutexes, and logging utilities.
    * Usage: The Test_Template class is designed to be inherited by specialized test classes, allowing them to define and execute specific test instructions while utilizing common testing methods and properties.
    */

    public abstract class Test_Template
    {
        /// Nested Instruction Class
        /// Function:
        /// This class serves as a template for instructions, allowing each instruction to have unique properties and methods.
        /// This enables the desired test class to manage and execute each instruction based on its properties and requirements.
        public class Instruction
        {
            public string InstructionText { get; set; }             // The text of the instruction.
            public string DesiredOutput { get; set; }               // The expected output.
            public string ActualOutput { get; set; }                // The actual output received.
            public string ErrorText { get; set; }                   // Any error message associated with the instruction.
            public bool CheckRequired { get; set; }                 // Indicates if this instruction requires checking.
            public bool CheckCompleted { get; set; }                // Indicates if this instruction has been successfully completed.
            public bool CheckBootConnected { get; set; }            // Indicates if the BOOT terminal needs to be connected for this instruction.
            public bool CheckMainConnected { get; set; }            // Indicates if the MAIN terminal needs to be connected for this instruction.

            public bool CheckPowerOn { get; set; }                  // Indicated if we need to check for power 
            public string CheckBoxText { get; set; }                // Text for associated checkboxes.
            public Processor TerminalToCheck { get; set; }          // Specifies which terminal to check (BOOT or MAIN).
            public int AttemptsLeft { get; set; }                   // Number of attempts remaining for this instruction.

            public bool hasSpecialCheck;                            // Indicates if this instruction requires a special check.
            public bool addedTestPoint = false;                      // Specifies if a test point has been added for this instruction.
            public bool isSecondLocation = false;                   // Indicates if this instruction is for a second location.
            public bool userAckRequired = false;
        }

        public enum Processor
        {
            NONE,
            BOOT,
            MAIN
        }

        public enum CurrentStatus
        {
            Not_Started,
            InProgress,
            Completed,
            Failed,
            Restarting
        }

        // Test Properties
        protected internal CurrentStatus Status { get; set; }

        protected int TestAttempts { get; set; }

        public int TestAttempt
        {
            get { return TestAttempts; }
            set
            {
                // Check if Attempts reaches 3, and if so, set InhibitTest to true
                if (TestAttempts == 3)
                {
                    InhibitTest = true;
                }
            }
        }

        public bool InhibitTest { get; private set; }
        public bool testStateChanged;
        protected int bytesToCopy;

        // Instruction properties / fields
        public List<Instruction> TestInstructions { get; set; }

        protected int CurrentIndex { get; set; }
        public int PublicIndex => CurrentIndex;

        // Instructions
        protected internal string CurrentInstruction => TestInstructions[CurrentIndex].InstructionText;


        // Locks for thread-safe access
        protected readonly SemaphoreSlim bufferLock = new SemaphoreSlim(1, 1); // Semaphore lock for buffer available to all classes

        protected readonly Mutex instructionCheckMutex = new Mutex();
        protected internal int MismatchOccuranceCount = 0;

        // Reference to terminal class (Required to communicate with terminals from within the test_template instances)
        protected Terminal _TerminalMAIN;
        protected Terminal _TerminalBOOT;

        public event EventHandler<string> AttemptToConnect; // Event to usher a connection reattempt

        // Concurrent queues for buffering text
        protected ConcurrentQueue<BufferStringProperties> BufferMAIN = new ConcurrentQueue<BufferStringProperties>(); // Used for MAIN terminal

        protected ConcurrentQueue<BufferStringProperties> BufferBOOT = new ConcurrentQueue<BufferStringProperties>(); // Used for BOOT terminal

        public class BufferStringProperties
        {
            public string Text { get; set; }
            public bool IsHidden { get; set; }
        }

        public event EventHandler<string> LogError; // Event for terminal output 

        // Constructor for initializing a test template.
        // Parameters:
        // - TerminalMAIN: The MAIN terminal to use for the test.
        // - TerminalBOOT: The BOOT terminal to use for the test.
        // - instructions: The list of instructions for the test.
        protected Test_Template(Terminal TerminalMAIN, Terminal TerminalBOOT, List<Instruction> instructions)
        {
            // Initialize the terminals and event handlers.
            _TerminalMAIN = TerminalMAIN;
            _TerminalBOOT = TerminalBOOT;
            _TerminalBOOT.TerminalOutputReceived += TerminalOutputReceivedHandler_B;
            _TerminalMAIN.TerminalOutputReceived += TerminalOutputReceivedHandler_M;

            // Set the list of test instructions.
            TestInstructions = instructions;

            // Initialize the current instruction index and test status.
            CurrentIndex = 0;
            Status = CurrentStatus.Not_Started;
        }

        ////////////////////////////////////////////////////////////////// Terminal EventHandlers ///////////////////////////////////////////////////////////////////////////////////////////////////

        // Handles terminal output received from the BOOT terminal.
        public void TerminalOutputReceivedHandler_B(object sender, string output)
        {
            // Create a buffer input object to hold the received output.
            BufferStringProperties bufferInput = new BufferStringProperties
            {
                Text = output,
                IsHidden = false
            };

            // Check if the test is in progress before enqueuing the output to the BOOT buffer.
            if (Status == CurrentStatus.InProgress)
            {
                BufferBOOT.Enqueue(bufferInput);
            }

            const int maxBufferSize = 256; // Define the maximum buffer size.

            // Check if the BOOT buffer size exceeds the maximum limit and dequeue excess items.
            if (BufferBOOT.Count > maxBufferSize)
            {
                _ = BufferBOOT.TryDequeue(out _); // Introduced a concurrent queue to allow multiple threads to access it.
            }
        }

        // Handles terminal output received from the MAIN terminal.
        public void TerminalOutputReceivedHandler_M(object sender, string output)
        {
            // Create a buffer input object to hold the received output.
            BufferStringProperties bufferInput = new BufferStringProperties
            {
                Text = output,
                IsHidden = false
            };

            // Check if the test is in progress before enqueuing the output to the MAIN buffer.
            if (Status == CurrentStatus.InProgress)
            {
                BufferMAIN.Enqueue(bufferInput);
            }

            const int maxBufferSize = 256; // Define the maximum buffer size.

            // Check if the MAIN buffer size exceeds the maximum limit and dequeue excess items.
            if (BufferMAIN.Count > maxBufferSize)
            {
                _ = BufferMAIN.TryDequeue(out _); // Introduced a concurrent queue to allow multiple threads to access it.
            }
        }

        ////////////////////////////////////////////////////////////////// Terminal Methods ///////////////////////////////////////////////////////////////////////////////////////////////////

        // Check the BOOT terminal for the desired output.
        protected internal virtual bool CheckBootTerminal(string desiredOutput)
        {
            if (CheckOutPutBuffer(BufferBOOT, desiredOutput))
            {
                return InstructionPassed();
            }
            else
            {
                return InstructionFailed();
            }
        }

        // Check the MAIN terminal for the desired output.
        protected internal virtual bool CheckMainTerminal(string desiredOutput)
        {
            if (CheckOutPutBuffer(BufferMAIN, desiredOutput))
            {
                return InstructionPassed();
            }
            else
            {
                return InstructionFailed();
            }
        }

        // Asynchronously save the terminal log for the current test.
        protected internal async Task SaveTerminalLog(string test)
        {
            await Task.Run(() =>
            {
                _TerminalBOOT.OutputLog(test, "BOOT", Login_Page.CDataLog.RP_NUM);
                _TerminalMAIN.OutputLog(test, "MAIN", Login_Page.CDataLog.RP_NUM);
            });
            _TerminalBOOT.ResetTerminalData();
            _TerminalMAIN.ResetTerminalData();
        }

        // Clear both BOOT and MAIN terminal buffers.
        public void ClearBuffers()
        {
            BufferBOOT = new ConcurrentQueue<BufferStringProperties>();
            BufferMAIN = new ConcurrentQueue<BufferStringProperties>();
        }

        // Returns the next string in the queue after the specified stringToCheck is found.
        // If the specified stringToCheck is not found in the queue, it returns the original stringToCheck.
        protected string ReturnNextStringInQueue(string stringToCheck, ConcurrentQueue<BufferStringProperties> queue)
        {
            bool CurrentStringFound = false; // Flag to indicate whether the current string has been found in the queue.
            string NextString = null; // Stores the next string in the queue.

            try
            {
                foreach (var item in queue)
                {
                    if (CurrentStringFound)
                    {
                        NextString = item.Text;

                        // If the NextString is not null or empty, return it immediately.
                        if (!string.IsNullOrEmpty(NextString))
                        {
                            return NextString;
                        }
                    }

                    // Check if the item's text contains the stringToCheck and it's the first occurrence.
                    if (item.Text.Contains(stringToCheck) && !CurrentStringFound)
                    {
                        CurrentStringFound = true; // String found in the queue.
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message);
            }

            // Ensure that null or empty values are not returned; instead, return the original stringToCheck.
            return string.IsNullOrEmpty(NextString) ? stringToCheck : NextString;
        }

        protected internal virtual bool CheckOutPutBuffer(ConcurrentQueue<BufferStringProperties> queue, string targetString)
        {
            // Iterate through the queue to search for the target string.
            foreach (var item in queue)
            {
                // Check if the item contains the target string and is not hidden.
                if (item.Text.Contains(targetString) && !item.IsHidden)
                {
                    // Return true if the string is found in the queue.
                    return true;
                }
            }

            // If the target string is not found in the queue, return false.
            return false;
        }

        // Hides occurrences of the specified string in the buffer for the specified terminal.
        // Parameters:
        //   stringToHide (string): The string to hide in the buffer.
        //   terminalBuffer (Processor): The terminal buffer to operate on (BOOT or MAIN).
        protected internal virtual void HideBufferInput(string stringToHide, Processor terminalBuffer)
        {
            ConcurrentQueue<BufferStringProperties> queue;

            // Determine which queue to use based on the terminalBuffer parameter.
            if (terminalBuffer == Processor.BOOT)
            {
                queue = BufferBOOT;
            }
            else
            {
                queue = BufferMAIN;
            }

            try
            {
                // Check if the specified string exists in the buffer before entering a loop to avoid race conditions.
                if (CheckOutPutBuffer(queue, stringToHide))
                {
                    foreach (var item in queue)
                    {
                        // Check if the item contains the string to hide and is not already hidden.
                        if (item.Text.Contains(stringToHide) && !item.IsHidden)
                        {
                            // Mark the item as hidden.
                            item.IsHidden = true;
                        }
                    }
                }
            }
            catch (Exception ex )

            {
                LogException(ex.Message);
            }
        }

        // Hides the first occurrence of the specified string in the buffer for the specified terminal.
        // Parameters:
        //   stringToHide (string): The string to hide in the buffer.
        //   terminalBuffer (Processor): The terminal buffer to operate on (BOOT or MAIN).
        // Returns:
        //   Task<bool>: A task that represents the asynchronous operation and returns true if the string was hidden; otherwise, false.
        protected internal async Task<bool> HideBufferInputBool(string stringToHide, Processor terminalBuffer)
        {
            ConcurrentQueue<BufferStringProperties> queue;
            bool hasBeenHidden = false;

            // Determine which queue to use based on the terminalBuffer parameter.
            if (terminalBuffer == Processor.BOOT)
            {
                queue = BufferBOOT;
            }
            else
            {
                queue = BufferMAIN;
            }

            try
            {
                // Check if the specified string exists in the buffer before entering a loop to avoid race conditions.
                if (CheckOutPutBuffer(queue, stringToHide))
                {
                    foreach (var item in queue)
                    {
                        // Check if the item contains the string to hide and is not already hidden.
                        if (item.Text.Contains(stringToHide) && !item.IsHidden)
                        {
                            // Mark the item as hidden.
                            item.IsHidden = true;
                            hasBeenHidden = true;
                            return hasBeenHidden;
                        }
                    }
                }
            }
            catch (Exception ex )
            {
                LogException(ex.Message);
            }

            return hasBeenHidden;
        }

        // Retrieves the first occurrence of the specified string in the buffer for the specified terminal.
        // Parameters:
        //   queue (ConcurrentQueue<BufferStringProperties>): The buffer queue to search in.
        //   targetString (string): The string to search for.
        // Returns:
        //   string: The first occurrence of the target string in the buffer, or null if not found or if an exception occurs.
        protected internal string OutputToString(ConcurrentQueue<BufferStringProperties> queue, string targetString)
        {
            try
            {
                if (queue == null || string.IsNullOrEmpty(targetString))
                {
                    // Parameter validation: Ensure that the queue is not null and the targetString is not empty.
                    return null;
                }

                foreach (var item in queue)
                {
                    // Check if the item contains the target string and is not hidden.
                    if (item.Text.Contains(targetString) && !item.IsHidden)
                    {
                        // Return the text of the item where the string was found.
                        return item.Text;
                    }
                }

                // The target string was not found in the queue.
                return null;
            }
            catch (Exception ex)
            {
                LogException(ex.Message);
            }

            // If an exception occurs or the target string is not found, return null.
            return null;
        }

        // Check if a file with the desired output has been found.
        // Parameters:
        //   desiredOutput (string): The desired output to search for in the buffer.
        //   test (string): The test description.
        // Returns:
        //   bool: True if the file has been found; otherwise, false.
        protected virtual bool HasFileBeenFound(string desiredOutput, string test)
        {
            bool fileFound = false;

            // Retrieve the next string in the buffer related to the desired output.
            string nextStringInBuffer = ReturnNextStringInQueue(desiredOutput, BufferBOOT);

            // Check if the next string in the buffer is "File found!".
            if (nextStringInBuffer != "File found!")
            {
                // Report that the file was not found as an error.
                TestInstructions[CurrentIndex].ErrorText = "File Not Found!";
                CheckFailed("FILE NOT FOUND");
            }
            else
            {
                // Set fileFound to true, indicating that the file has been found.
                fileFound = true;

                // Set the actual output for this instruction to "File Found!".
                TestInstructions[CurrentIndex].ActualOutput = "File Found!";

                // Add a test point outcome indicating a PASS.
                AddTestPointOutcome(test, "PASS");
            }

            return fileFound;
        }

        // Check if the input string contains the total bytes to copy using a regular expression.
        // Parameters:
        //   input (string): The input string to search for the numeric value.
        // Returns:
        //   bool: True if a valid numeric value was found and converted; otherwise, false.
        // Attempt to extract and parse a numeric value from the input string.
        // Parameters:
        //   input (string): The input string containing the numeric value.
        // Returns:
        //   bool: True if the numeric value is successfully parsed and greater than zero; otherwise, false.
        protected virtual bool ReadNumFromStringBytes(string input)
        {
            try
            {
                // Use regular expression to match the numeric part of the input string.
                string numericValue = Regex.Match(input, @"copied:\s*(\d+)").Groups[1].Value;

                // Convert the extracted numeric value to an integer.
                bytesToCopy = int.Parse(numericValue);

                // Check if the parsed numeric value is greater than zero.
                if (bytesToCopy > 0)
                {
                    // If the numeric value is valid (greater than zero), return true.
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message);
            }

            // If any error occurs or the numeric value is not valid, return false.
            return false;
        }

        // Check if the total bytes have been successfully copied.
        //  Returns:
        //   bool: True if the total bytes have been copied successfully; otherwise, false.
        protected virtual bool TotalBytesCopied()
        {
            bool BytesCopiedSuccessful = false;

            try
            {
                // Retrieve the string containing the total bytes copied from the BOOT terminal's buffer.
                string bytesToCopy = OutputToString(BufferBOOT, "Done! Bytes copied:");

                // Check if the retrieved string is null or empty.
                if (string.IsNullOrEmpty(bytesToCopy))
                {
                    // If the string is null or empty, return false indicating failure.
                    return BytesCopiedSuccessful;
                }

                // If the string is not null or empty, attempt to read the numeric value of bytes copied.
                if (ReadNumFromStringBytes(bytesToCopy))
                {
                    // If the numeric value is successfully read, set BytesCopiedSuccessful to true, hide the string in the buffer, and pass the instruction.
                    BytesCopiedSuccessful = true;
                    HideBufferInput(bytesToCopy, Processor.BOOT);
                }
                else
                {
                    // If the numeric value cannot be read, set BytesCopiedSuccessful to false and fail the instruction.
                    BytesCopiedSuccessful = false;
                }
            }
            catch (Exception ex )
            {
                LogException(ex.Message);
                BytesCopiedSuccessful = false;
            }

            // Return the final result indicating whether bytes were copied successfully.
            return BytesCopiedSuccessful;
        }

        // Check if the MAIN terminal is connected based on the instruction requirements.
        // Returns:
        //   bool: True if the MAIN terminal is connected as required by the instruction; otherwise, false.
        protected bool IsMainConnected()
        {
            // Check if the instruction requires MAIN terminal connection and if it is connected.
            if (TestInstructions[CurrentIndex].CheckMainConnected && Globals.IsTerminal_M_Connected)
            {
                // If the conditions are met, return true indicating the instruction is passed.
                return InstructionPassed();
            }
            else if(TestInstructions[CurrentIndex].CheckMainConnected && !Globals.IsTerminal_M_Connected && _TerminalMAIN.HasPreviouslyBeenConnected)
            {
                try
                {
                    AttemptToConnect?.Invoke(this, "MAIN");
                    if (Globals.IsTerminal_M_Connected)
                    {
                        return InstructionPassed();
                    }

                }
                catch (Exception ex)
                {
                    LogException(ex.Message);
                }
            }

            // Return false if the MAIN terminal is not connected as required.
            return false;
        }

        // Check if the BOOT terminal is connected based on the instruction requirements.
        // Returns:
        //   bool: True if the BOOT terminal is connected as required by the instruction; otherwise, false.
        protected bool IsBootConnected()
        {
            
            // Check if the instruction requires BOOT terminal connection and if it is connected.
            if (TestInstructions[CurrentIndex].CheckBootConnected && Globals.IsTerminal_B_Connected)
            {
                // If the conditions are met, return true indicating the instruction is passed.
                return InstructionPassed();
            }
            else if (TestInstructions[CurrentIndex].CheckBootConnected && !Globals.IsTerminal_B_Connected && _TerminalBOOT.HasPreviouslyBeenConnected)
            {
                try
                {
                    AttemptToConnect?.Invoke(this, "BOOT");
                    if(Globals.IsTerminal_B_Connected)
                    {
                        return InstructionPassed();
                    }
                    
                }
                catch (Exception ex)
                {
                    LogException(ex.Message);
                }

            }
            else
            {
                 
            }

            // Return false if the BOOT terminal is not connected as required.
            return false;
        }

        // Reads a numeric value from a string using regular expression.
        // Parameters:
        //   input (string): The input string to extract the numeric value from.
        // Returns:
        //   int: The extracted numeric value. Returns 0 if no valid numeric value is found.
        protected internal int ReturnNum_FromStringErrorCheck(string input)
        {
            try
            {
                // Use regular expression to match the numeric part of the input string.
                string numericValue = Regex.Match(input, @"voltage:\s*(\d+)").Groups[1].Value;

                // Convert the numeric value to an integer.
                int value = int.Parse(numericValue);

                return value;
            }
            catch (FormatException)
            {
                // Handle the case where the numeric value cannot be parsed due to a format error.
                // Return 0 to indicate that no valid numeric value was found.
                return 0;
            }
            catch (Exception ex )
            {
                LogException(ex.Message);
                // Return 0 as a default value in case of any error.
                return 0;
            }
        }

        // Adds a test point outcome if the desired output is not null.
        // Parameters:
        //   Test (string): The test description.
        //   outcome (string): The outcome of the test (e.g., "PASS" or "FAIL").
        protected internal void AddTestPointOutcome(string Test, string outcome)
        {
            if (TestInstructions[CurrentIndex].DesiredOutput != null)
            {
                if (!TestInstructions[CurrentIndex].addedTestPoint)
                {
                    // Add a test point outcome to the data log.
                    Login_Page.CDataLog.AddTestPointOutcome(Test, TestInstructions[CurrentIndex].DesiredOutput, TestInstructions[CurrentIndex].ActualOutput, outcome, TestAttempts);

                    // Set addedTestPoint to true to ensure that the test point is only saved once.
                    TestInstructions[CurrentIndex].addedTestPoint = true;
                }
            }
        }

        // Adds a failed test point outcome if the desired output is not null.
        // Parameters:
        //   Test (string): The test description.
        //   outcome (string): The outcome of the test (e.g., "FAIL").
        protected internal void FailedTestPoint(string Test, string outcome)
        {
            if (TestInstructions[CurrentIndex].DesiredOutput != null)
            {
                if (!TestInstructions[CurrentIndex].addedTestPoint)
                {
                    // Add a failed test point outcome to the data log, including the error text.
                    Login_Page.CDataLog.AddTestPointOutcome(Test, TestInstructions[CurrentIndex].DesiredOutput, TestInstructions[CurrentIndex].ErrorText, outcome, TestAttempts);

                    // Set addedTestPoint to true to ensure that the test point is only saved once.
                    TestInstructions[CurrentIndex].addedTestPoint = true;
                }
            }
        }

        // Marks the test as completed and performs cleanup actions.
        // Parameters:
        //   test (string): The description of the completed test.
        protected internal void TestComplete(string test)
        {
            // Set the test status to "Completed" and inhibit further testing.
            Status = CurrentStatus.Completed;
            InhibitTest = true;

            // Add the test outcome to the data log.
            Login_Page.CDataLog.AddTestOutcome(Status.ToString());

            // Clear the terminal buffers.
            ClearBuffers();

            // Save the terminal log asynchronously.
            _ = SaveTerminalLog(test);

            // Display a message to indicate that the test is complete.
            MessageBox.Show("Test Complete \n\r Pick Another Test", "WELL DONE !", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Verifies the device status based on the test instructions.
        protected internal virtual void VerifyDevice()
        {
            // Check if the BOOT terminal output matches the desired output.
            if (!CheckBootTerminal(TestInstructions[CurrentIndex].DesiredOutput))
            {
                // If the desired output is not found, check for specific boot messages.
                if (CheckBootTerminal("QSPI boot") || CheckBootTerminal("USBMSC boot"))
                {
                    // If the boot messages indicate an error, mark the check as failed.
                    if(BufferBOOT.Count > 50) // if we continue to load and cannot verify that we are loading from SD then fail the test 
                    {
                        CheckFailed("LOAD FILE ERROR");
                    }
                    
                }
            }
        }

        // Checks for the presence of error messages in both BOOT and MAIN terminals.
        protected internal void IsErrorPresent()
        {
            // Check for the presence of "Error" or "ERROR" messages in BOOT and MAIN terminals.
            if (CheckBootTerminal("Error") || CheckMainTerminal("Error") || CheckBootTerminal("ERROR") || CheckMainTerminal("ERROR"))
            {
                // If any error messages are found, mark the check as failed.
                CheckFailed("ERROR");
            }
        }

        protected virtual void CheckFailed(string CheckIdentifier)
        {
            if (testStateChanged != true)
            {
                testStateChanged = true; // Lock this so that errors are not repeatedly called; it should only be called once, but the check interval is too quick.

                // Handle different types of check failures and take appropriate actions.
                switch (CheckIdentifier)
                {
                    case "ADC1 ANI5":
                        // Set the error text to the actual output for this check.
                        TestInstructions[CurrentIndex].ErrorText = TestInstructions[CurrentIndex].ActualOutput;

                        // Mark the test point as failed and display a warning message.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        MessageBox.Show($"ADC1 ANI5 voltage : Too Low - {ReturnNum_FromStringErrorCheck(OutputToString(BufferMAIN, "ADC1 AIN5 voltage:"))}", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;

                    case "ADC0 ANI6":
                        // Set the error text to the actual output for this check.
                        TestInstructions[CurrentIndex].ErrorText = TestInstructions[CurrentIndex].ActualOutput;

                        // Mark the test point as failed and display a warning message.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        MessageBox.Show($"ADC1 ANI6 voltage : Too Low - {ReturnNum_FromStringErrorCheck(OutputToString(BufferMAIN, "ADC1 AIN6 voltage:"))}", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;

                    case "Ping Failed":
                        // Display a message indicating that the test has failed due to a lack of ping response.
                        MessageBox.Show("Test Failed : No Ping Response", null, MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Update the desired output and error text for this check.
                        TestInstructions[CurrentIndex].DesiredOutput = "Ping Successful";
                        TestInstructions[CurrentIndex].ErrorText = "Test Failed : No Ping Response";

                        // Mark the test point as failed.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        break;

                    // Handle the failure case when the check identifier is "Firmware Test point ".
                    case "Firmware Test point ":
                        // Mark the test point as failed.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        break;

                    // Handle the failure case when the check identifier is "SSR Test point ".
                    case "SSR Test point ":
                        // Mark the test point as failed.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        break;

                    // Handle the failure case when the check identifier is "MISMATCH".
                    case "MISMATCH":
                        // Set the error text to indicate too many mismatches.
                        TestInstructions[CurrentIndex].ErrorText = "ERROR : Too many MISMATCH";

                        // Mark the test point as failed.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        break;

                    // Handle the failure case when the check identifier is "USB BYTES".
                    case "USB BYTES":
                        // Mark the test point as failed.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        break;

                    // Handle the failure case when the check identifier is "USB FILE NOT FOUND".
                    case "USB FILE NOT FOUND":
                        // Display a warning message indicating that a file was not found.
                        MessageBox.Show("Failed to Find File", null, MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Set the error text to indicate file not found.
                        TestInstructions[CurrentIndex].ErrorText = "ERROR: File not found ! ";

                        // Mark the test point as failed.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        break;

                    // Handle the failure case when the check identifier is "LOAD FILE ERROR".
                    case "LOAD FILE ERROR":
                        // Set the error text to indicate loading from an unknown/wrong device.
                        TestInstructions[CurrentIndex].ErrorText = "ERROR: Loading from unknown/wrong device";

                        // Display a warning message indicating loading from an unknown device.
                        MessageBox.Show("Loading from UNKNOWN device", null, MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Mark the test point as failed.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        break;

                    // Handle the failure case when the check identifier is "FILE NOT FOUND".
                    case "FILE NOT FOUND":
                        // Set the error text to indicate file not found.
                        TestInstructions[CurrentIndex].ErrorText = "File not found ! ";

                        // Display a warning message indicating that a file was not found.
                        MessageBox.Show("Failed to Find File", null, MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Mark the test point as failed.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        break;

                    // Handle the failure case when the check identifier is "ERROR".
                    case "ERROR":
                        // Set the error text to indicate that the test was aborted due to the presence of an error.
                        TestInstructions[CurrentIndex].ErrorText = "Test Aborted ERROR was present (Check log for more info)";

                        // Display a warning message indicating an error during the test.
                        MessageBox.Show("ERROR while running test ", null, MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Mark the test point as failed.
                        FailedTestPoint(Globals.TestInProgressGlobal.ToUpper(), " FAIL");
                        break;

                    case null:
                        break;
                }

                // Update the test status to indicate failure.
                Status = CurrentStatus.Failed; // Keep this last to avoid status changes after a failure.
            }
        }

        ////////////////////////////////////////////////////////////////// Instructions Methods ///////////////////////////////////////////////////////////////////////////////////////////////////
        // Performs the check for the current test instruction.
        // Returns:
        //   bool: True if the check was successfully performed; otherwise, false.
        public virtual async Task<bool> PerformInstructionCheck()
        {
            bool aquiredMutex = false;
            try
            {
                // Attempt to acquire the instruction check mutex.
                aquiredMutex = instructionCheckMutex.WaitOne(0);

                // If the mutex cannot be acquired, return false.
                if (!aquiredMutex)
                {
                    return false;
                }

                // If no check is required for the current instruction, mark it as complete and return true.
                if (!TestInstructions[CurrentIndex].CheckRequired)
                {
                    TestInstructions[CurrentIndex].CheckCompleted = true;
                    return true;
                }

                // Check if a desired output is specified for the current instruction.
                if (TestInstructions[CurrentIndex].DesiredOutput != null)
                {
                    string desiredOutput = TestInstructions[CurrentIndex].DesiredOutput;

                    // Check the specified terminal (MAIN or BOOT) for the desired output.
                    if (TestInstructions[CurrentIndex].TerminalToCheck == Processor.MAIN)
                    {
                        // Check the MAIN terminal for the desired output.
                        CheckMainTerminal(desiredOutput);
                    }
                    if (TestInstructions[CurrentIndex].TerminalToCheck == Processor.BOOT)
                    {
                        // Check the BOOT terminal for the desired output.
                        CheckBootTerminal(desiredOutput);
                    }
                }

                // Check for MAIN and BOOT connection if specified.
                if (TestInstructions[CurrentIndex].CheckMainConnected)
                {
                    return IsMainConnected();
                }
                if (TestInstructions[CurrentIndex].CheckBootConnected)
                {
                    return IsBootConnected();
                }

                // If none of the above conditions are met, return the completion status of the current instruction.
                return TestInstructions[CurrentIndex].CheckCompleted;
            }
            finally
            {
                // Release the instruction check mutex if it was acquired.
                if (aquiredMutex)
                {
                    instructionCheckMutex.ReleaseMutex();
                }
            }
        }

        // Moves to the next test instruction in the list, considering completion and check requirements.
        protected internal void MoveToNextInstruction()
        {
            try
            {
                // Check if a check is required for the current instruction.
                if (TestInstructions[CurrentIndex].CheckRequired)
                {
                    // Check if the current instruction is marked as completed.
                    if (TestInstructions[CurrentIndex].CheckCompleted)
                    {
                        // Check if the current index is within the valid range of instructions.
                        if (CurrentIndex >= TestInstructions.Count - 1)
                        {
                            // Limit the current index to the last instruction.
                            CurrentIndex = TestInstructions.Count - 1;
                        }
                        else
                        {
                            // Move to the next instruction.
                            CurrentIndex++;
                        }
                    }
                }
                else
                {
                    // Check if the current index is within the valid range of instructions.
                    if (CurrentIndex >= TestInstructions.Count - 1)
                    {
                        // Limit the current index to the last instruction.
                        CurrentIndex = TestInstructions.Count - 1;
                    }
                    else
                    {
                        // Move to the next instruction.
                        CurrentIndex++;
                    }
                }
            }
            catch (Exception ex )
            {
                LogException(ex.Message);
            }
        }

        //Move to the previous instruction by changing the index
        // Error checking added incase index is negative
        protected internal void MoveToPreviousInstruction()
        {
            try
            {
                if(CurrentIndex != 0 ) // if we are not on the first instruction 
                {
                    if ( TestInstructions[CurrentIndex - 1].CheckCompleted && CurrentIndex > 0) // check that previous instruction was complete
                    { // If it was then the test has begun and we can allow the user to go back to the previous instruction
                        if (CurrentIndex <= 0)
                        {
                            CurrentIndex = 0;
                        }
                        else
                        {
                            CurrentIndex--;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogException(ex.Message);
            }
        }

        // Checks if all test instructions are complete.
        // Returns:
        //   bool: True if all instructions are complete; otherwise, false.
        public bool AllInstructionsComplete()
        {
            bool allInstructionsComplete = true; // Assume all instructions are complete initially

            foreach (var instruction in TestInstructions)
            {
                if (!instruction.CheckCompleted)
                {
                    allInstructionsComplete = false;
                    break; // No need to continue checking, since we found an incomplete instruction
                }
            }

            return allInstructionsComplete;
        }

        // Marks the current instruction as passed.
        // Returns:
        //   bool: True if the instruction passed; otherwise, false.
        // Handles the successful completion of an instruction.
        // Returns:
        //   bool: True if the instruction passed or is a special check; otherwise, false.
        protected bool InstructionPassed()
        {
            try
            {
                // Check if the instruction is not a special check.
                if (!TestInstructions[CurrentIndex].hasSpecialCheck)
                {
                    // Set the CheckCompleted property to true to indicate automatic completion.
                    return TestInstructions[CurrentIndex].CheckCompleted = true;
                }

                // If it's a special check, return true.
                return true;
            }
            catch (Exception ex)
            {
                LogException(ex.Message);
            
                return false;
            }
        }

        // This method increments the AttemptsLeft counter, and if it reaches a threshold (256),
        // it displays an error message.
        // Returns:
        //   bool: Always returns false to indicate failure.
        protected bool InstructionFailed()
        {
            try
            {
                // Increment the AttemptsLeft counter for the current instruction.
                TestInstructions[CurrentIndex].AttemptsLeft++;

                // Check if the AttemptsLeft counter has reached the threshold (256).
                if (TestInstructions[CurrentIndex].AttemptsLeft >= 256)
                {
                    // Display an error message with the error text from the current instruction.
                    MessageBox.Show(TestInstructions[CurrentIndex].ErrorText, null, MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Reset the AttemptsLeft counter to 0.
                TestInstructions[CurrentIndex].AttemptsLeft = 0;

                // Always return false to indicate failure.
                return false;
            }
            catch (Exception ex)
            {
                LogException(ex.Message);

                // If an exception occurs during execution, return false to indicate failure.
                return false;
            }
        }

        // Returns a dictionary containing checkbox information from test instructions.
        // The dictionary maps the index of instructions with non-null CheckBoxText to their corresponding text.
        // Returns:
        //   Dictionary<int, string>: A dictionary of checkbox information.
        // Returns a dictionary containing checkbox information from test instructions.
        // The dictionary maps the index of instructions with non-null CheckBoxText to their corresponding text.
        // Returns:
        //   Dictionary<int, string>: A dictionary of checkbox information.
        protected internal Dictionary<int, string> ReturnCheckBoxInfo()
        {
            try
            {
                // Create a dictionary to store checkbox information.
                Dictionary<int, string> checkboxInfo = new Dictionary<int, string>();

                // Iterate through the test instructions.
                for (int index = 1; index < TestInstructions.Count; index++)
                {
                    // Check if the current instruction has non-null CheckBoxText.
                    if (TestInstructions[index].CheckBoxText != null)
                    {
                        // Add the index and CheckBoxText to the dictionary.
                        checkboxInfo.Add(index, TestInstructions[index].CheckBoxText);
                    }
                }

                // Return the populated checkbox information dictionary.
                return checkboxInfo;
            }
            catch (Exception ex)
            {
                LogException(ex.Message);

                // If an exception occurs, return an empty dictionary to indicate an error.
                return new Dictionary<int, string>();
            }
        }

        ////////////////////////////////////////////////////////////////// Method Bodies ///////////////////////////////////////////////////////////////////////////////////////////////////
        public virtual void Reset_Test()
        {
        }

        protected internal void IncAttempt()
        {
            TestAttempts++;
            // We only inc when a restart is requested
            //Reset the current Index
            CurrentIndex = 0;
        }

        // Resets the state of the testing class.
        public void Reset()
        {
            // Set the current index to the beginning of the instructions.
            CurrentIndex = 0;

            // Clear the list of test instructions.
            TestInstructions.Clear();
        }

        protected internal virtual void InitializeInstructions() // just the body actual initilization takes place in tests classes
        {
            //
        }


        public bool StartDisplayingTerminal()
        {
            bool startDisplay = false;
            try
            {
                if(CurrentIndex >= 0)
                {
                    if (TestInstructions[CurrentIndex ].TerminalToCheck != Processor.NONE) // If previous instruction requires a terminal to be checked start displaying data to terminal screen 
                    {
                        startDisplay = true;
                    }
                    else
                    {
                        startDisplay = false;
                    }
                }

            }
            catch (Exception ex)
            {
                LogErrorMessage(ex.Message);
                startDisplay = false;
            }
            return startDisplay;



        }


        public void Reset_Test_Restart()
        {
            Reset();

            ClearBuffers();
        }

        //This method is responsible for waking up any subscribers to the event handler inorder to exceptions cought in methods or during operations 
        protected internal void LogException(string ExceptionMessage)
        {
            LogError?.Invoke(this, ExceptionMessage);
        }
        protected internal void LogErrorMessage(string ExceptionMessage)
        {
            LogError?.Invoke(this, ExceptionMessage);
        }










        

    }
}