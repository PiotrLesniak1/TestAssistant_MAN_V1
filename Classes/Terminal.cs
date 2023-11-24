using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;
using TestAssistant.HardwareState;

namespace TestAssistant.Classes
{
    /*
     * File: Terminal.cs
     * Author: Piotr Lesniak (Raytheon)
     * Date: 16/10/23
     * Description: Implementation of the Terminal class, used for managing serial port connections, data reception, and formatting incoming Data from the Serial Connection .
     * Dependencies: This class relies on external libraries and components for serial communication.
     * Usage: The Terminal class is employed within the TestAssistant application to connect to and manage serial communication with an RP Board. It also processes received data, formats it, and provides event handling for terminal output.
     */

    public class Terminal
    {
        private SerialPort serialPort;

        // Connection specifications
        private readonly int BaudRate = 115200;

        private readonly int DataBits = 8;
        private readonly int Parity = 0;
        private readonly int StopBit = 1;
        public string portNumber;
        public string PortNumber
        { get { return portNumber; } }
        public bool IsConnected { get; private set; }

        public string receivedData;

        public event EventHandler<string> TerminalOutputReceived; // Event for terminal output
        public event EventHandler<string> DisplayErrorMessage; // Event for terminal output


        //AUTO Connection Variables 
        public bool HasPreviouslyBeenConnected = false; // bool used to automatically connect to ports if they have been succesfully connected 
        public string PreviouslyConnectedPort; 
        // Will hold the terminal output (Each instance of this class will have its own)
        public List<string> Terminal_Log { get; } = new List<string>();

        private Queue<string> incompletePart = new Queue<string>(); // Holds the incomplete parts of received data (Global as methods reset the queue if defined locally)
        private Queue<string> dataBuffer = new Queue<string>(); // Buffer for incoming data, so that no data is lost
        private Queue<string> dataOutput = new Queue<string>(); // Queue to hold formatted data

        private List<string> tempUSBbuffer = new List<string>(); // used to track input from terminal after issuing a command 
        public Terminal()
        {
        }

        // Connect to the specified serial port
        public bool Connect(string SelectedPort)
        {
            bool connectedSuccessfully = false;
            
            try
            {
                serialPort = new SerialPort(SelectedPort, BaudRate, (Parity)Parity, DataBits, (StopBits)StopBit);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();
                IsConnected = serialPort.IsOpen;
                connectedSuccessfully = serialPort.IsOpen;
                portNumber = serialPort.PortName;
                if(!HasPreviouslyBeenConnected) // if this is the first time connecting to the ports 
                {
                    HasPreviouslyBeenConnected = true; // set to true as connecton was successfull 
                    PreviouslyConnectedPort = SelectedPort;
                    if(!Globals.InhibitConnectionInstructions)
                    {
                        Globals.InhibitConnectionInstructions = true;
                    }

                }
            }
            catch (Exception ex)
            {
                if(ex.Message == $"Access to the port '{PreviouslyConnectedPort}' is denied.")
                {
                    Disconnect();
                    Connect(PreviouslyConnectedPort);
                }
                //"Access to the port 'COM11' is denied." close and reopen 


                // Display an error message if the connection fails
                DisplayErrorMessage.Invoke(this, ex.Message);
                

            }
            return connectedSuccessfully;
        }

        // Reconnect to the specified serial port
        public bool ReConnect(string SelectedPort)
        {
            try
            {
                serialPort = new SerialPort(SelectedPort, BaudRate, (Parity)Parity, DataBits, (StopBits)StopBit);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();

                portNumber = serialPort.PortName;
            }
            catch (Exception ex)
            {
                // Return the connection status (true if connected, false if not)
                DisplayErrorMessage.Invoke(this, ex.Message);
            }
            // Return the connection status (true if connected, false if not)
            return serialPort.IsOpen;
        }

        // Returns the current connection status
        public bool ReturnConnectionStatus()
        {
            return serialPort.IsOpen;
        }

        // Disconnect from the serial port
        public bool Disconnect()
        {
            bool disconnected = false;
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Close();
                    serialPort.Dispose();
                    IsConnected = false;
                    disconnected = true;
                    
                }
                else
                {
                    // If already disconnected, set the connection status to false
                    IsConnected = false;
                    disconnected = true;
                }
            }
            catch (Exception ex)
            {
                // Display an error message if disconnection fails
                DisplayErrorMessage.Invoke(this, $"Failed to disconnect: {ex.Message}");
                IsConnected = true;
                disconnected = false;
            }
            ReplyProccessorState.USB_Application_Loaded = false; 
            return disconnected;
        }

        // Event handler for handling data received on the serial port
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                _ = Task.Run(() =>
                {

                    if (Globals.IsTestInProgress || Globals.Debugging || ReplyProccessorState.InitilisingTest)
                    {
                        dataBuffer.Enqueue(serialPort.ReadExisting()); // Enqueue data to a temp buffer
                        ProcessDataOutput(); // Process it to make it readable
                    }
                });
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        // Process and format the received data
        private void ProcessDataOutput()
        {
            lock (dataBuffer)
            {
                while (dataBuffer.Count > 0)
                {
                    string data = dataBuffer.Dequeue();
                    FormatData(data); // Process and format the data
                }
            }
        }

        // Format the received data
        public void FormatData(string data)
        {
            string[] delimiters = { "\n\r", "\r\n\r" }; // Delimiters for splitting data

            if (incompletePart.Count > 0)
            {
                // Combine any previously incomplete data with the current data
                data = incompletePart.Dequeue() + data;
            }

            // Split the data into segments using specified delimiters
            string[] formattedData = data.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            int last_index = formattedData.Length - 1;

            foreach (string item in formattedData)
            {
                if (!StringIntegrity(data))
                {
                    dataOutput.Enqueue(item); // Add complete data to the output queue
                }

                if (StringIntegrity(data)) // Check if the current string is incomplete
                {
                    if (item == formattedData[last_index])
                    {
                        incompletePart.Enqueue(item); // Add incomplete part to the queue
                    }
                    else
                    {
                        dataOutput.Enqueue(item); // Add complete data to the output queue
                    }
                }
            }

            // Process and return complete data from the output queue
            while (dataOutput.Count > 0)
            {
                if (string.IsNullOrEmpty(dataOutput.Peek())) // If the first element in the dataOutput is null or empty, then discard it
                {
                    dataOutput.Dequeue();
                }
                else
                {
                    string line = dataOutput.Dequeue();
                    line = FurtherDataProcessing(line); // Replace tab characters with spaces
                    TerminalOutputReceived?.Invoke(this, line); // Invoke the event with the formatted line
                    Terminal_Log.Add(line);

                    //USB

                    

                    if (line.Contains("*** USB Utility Application"))
                    {
                        
                        ReplyProccessorState.USB_Application_Loaded = true;
                        
                    }
                    if (ReplyProccessorState.USB_Application_Loaded)
                    {
                        tempUSBbuffer.Add(line);
                    }

                }
            }
        }
        public async Task<bool> ReplaceSDcontent(string test)
        {
            // LOGIC : we only send one line of command so if during the waiting period we receive something 
            //then the usb application has began work 
            //e.g tempUSBbuffer contains 2 strings and we send a command so now it holds 3 if during waiting we receive an extra string that means 
            // the application has began the transfer so wait until DONE is present 
            int tempBufSize = tempUSBbuffer.Count;
            while (!tempUSBbuffer.Contains("DONE!"))
            {
                
                if (tempUSBbuffer.Count < tempBufSize + 1) 
                {
                    if(!tempUSBbuffer.Contains("DONE!"))
                    {
                        switch (test)
                        {
                    
                            case "Commissioning":
                                SendData("rc /COM \r");
                                break;
                            case "Commissioning_usb":
                                SendData("rc /COMUSB");
                                break;
                            case "Firmware":
                                SendData("rc /FIRM");
                                break;
                            case "SSR_Reply":
                                SendData("rc /SSR");
                                break;
                            case "Ethernet":
                                SendData("rc /ETH");
                                break;
                            default:
                                SendData($"rc /{test}\r");
                                break;
                        }
                    }

                    tempBufSize++;
                }


                
                
                await Task.Delay(TimeSpan.FromSeconds(3));

                
               
               
            }
            tempUSBbuffer = new List<string>(); // reset buffer and return 
            return true;
        }
        // Further process data to handle special characters
        private string FurtherDataProcessing(string stringToProcess)
        {
            // Check for special characters and replace them in case they have been missed during string formatting
            if (stringToProcess.Contains("\t"))
            {
                stringToProcess = stringToProcess.Replace("\t", "  ");
            }
            if (stringToProcess.StartsWith("\r"))
            {
                stringToProcess = stringToProcess.Replace("\r", "");
            }
            stringToProcess = stringToProcess.TrimStart();
            return stringToProcess;
        }

        // Check if the received data is incomplete
        private bool StringIntegrity(string data)
        {
            // Check if the string ends with specific characters
            bool endsInNewLineCarrigeReturn = data.EndsWith("\n\r");
            bool endsInCarrigeNewLine = data.EndsWith("\r\n");
            bool endsWithTab = data.EndsWith("\t");
            bool endsInNewLine = data.EndsWith("\n");

            // If none of the expected endings are present, the string is incomplete
            return !(endsInNewLineCarrigeReturn || endsWithTab || endsInCarrigeNewLine || endsInNewLine);
        }

        // Send data to the connected terminal
        public void SendData(string DataToSend)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.BaseStream.Flush();
                    serialPort.WriteLine(DataToSend);
                    //TerminalOutputReceived?.Invoke(this, DataToSend); // Invoke the event with the formatted line
                }
            }
            catch (Exception ex)
            {
                DisplayErrorMessage.Invoke(this, ex.Message);
            }
        }

        // Reset terminal data
        public void ResetTerminalData()
        {
            Terminal_Log.Clear();
            dataOutput.Clear();
        }

        // Output the terminal log to a file
        public void OutputLog(string test, string terminal, string rpNumber)
        {
            string DataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // MyDocuments directory
            string testFolder = Path.Combine(DataDirectory, "Test_Assistant"); // Should already exist
            string LogFolderPath = Path.Combine(testFolder, $"{rpNumber}_TestLogs"); // Create a subfolder named "TestLogs" if it does not exist

            Directory.CreateDirectory(LogFolderPath); // Create the folder if it doesn't exist

            string FilePath = Path.Combine(LogFolderPath, $"{test}_{terminal}.txt"); // Use an underscore to separate test and terminal

            try
            {
                using (StreamWriter writer = new StreamWriter(FilePath))
                {
                    foreach (string line in Terminal_Log)
                    {
                        writer.WriteLine(line); // Write the line
                    }
                }
            }
            catch (Exception ex)
            {
                // Display an error message if log generation fails
                DisplayErrorMessage.Invoke(this, ex.Message);
            }
        }

        public async Task<List<string>> ReturnSerialPortsAsync()
        {
            // This asynchronous method retrieves a list of all available serial ports
            // and checks their accessibility concurrently. The method returns a list
            // of all the valid (accessible) ports.

            // Retrieve all available port names.
            string[] ports = SerialPort.GetPortNames();

            // List to store the valid ports.
            List<string> ValidPorts = new List<string>();

            // List of tasks that will run concurrently to check port accessibility.
            List<Task<bool>> tasks = new List<Task<bool>>();

            // For each port, initiate an asynchronous task to check its accessibility.
            foreach (string port in ports)
            {
                tasks.Add(CheckPortAccessibilityAsync(port));
            }

            bool[] results;

            try
            {
                // Await all tasks to complete and collect the results.
                results = await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                // Display an error message in case of any exceptions during task execution.
                MessageBox.Show($"Error checking ports: {ex.Message}");
                return new List<string>(); // Return an empty list in case of errors.
            }

            // Populate the list of valid ports based on the task results.
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i])
                {
                    ValidPorts.Add(ports[i]);
                }
            }

            // Return the list of validated ports.

            return ValidPorts;
        }

        private async Task<bool> CheckPortAccessibilityAsync(string port)
        {
            // This method checks the accessibility of a given serial port.
            // It tries to open and then immediately close the port to verify its accessibility.
            // Returns true if the port is accessible, false otherwise.

            using (SerialPort testport = new SerialPort(port))
            {
                try
                {
                    // Try to open and then close the port.
                    testport.Open();
                    testport.Close();
                    return true; // Port is accessible.
                }
                catch (Exception)
                {
                    return false; // Port is not accessible.
                }
            }
        }

    }
}