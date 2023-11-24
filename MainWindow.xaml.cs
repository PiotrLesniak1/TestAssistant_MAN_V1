using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TestAssistant.Classes;
using TestAssistant.AppConfiguration;
using TestAssistant.Dialog;
using TestAssistant.HardwareState;
using TestAssistant.HardwareCommunication;

namespace TestAssistant
{
    /*
     * File: MainWindow.cs
     * Author: Piotr Lesniak (Raytheon)
     * Date: 16/10/23
     * Description: This file contains the implementation of the MainWindow class, which represents the main window of the TestAssistant application.
     *              The MainWindow class serves as the user interface for interacting with and controlling the testing process. It handles user input, displays test results,
     *              and manages the overall workflow of the application.
     * Dependencies: This class relies on various other classes, controls, and components for building the user interface and executing testing operations.
     * Usage: The MainWindow class is the entry point of the TestAssistant application and is responsible for initializing the application, handling user interactions,
     *        and orchestrating the execution of tests on a connected hardware device. It provides a graphical user interface for configuring and monitoring tests and displays
     *        the results of those tests to the user.
     */



    public partial class MainWindow : Window
    {
        // Enum to define available test types with descriptions.
        private enum ListOfTests
        {
            [Description("No Test In Progress")]
            NONE,

            [Description("USB Commissioning Test")]
            Commissioning_usb,

            [Description("Firmware Test")]
            Firmware,

            [Description("SSR Test")]
            SSR_Reply,

            [Description("ADC Test")]
            ADC,

            [Description("Ethernet Test")]
            Ethernet,

            [Description("NormalOp Test")]
            NormalOp,

            [Description("Commissioning Test")]
            Commissioning
        }



        // Define class-level connection attempt variables.
        private int bootAttempts;

        private int mainAttempts;

        // Dictionaries to store tests, checkboxes, and buttons.
        private Dictionary<ListOfTests, Test_Template> Tests;

        private Dictionary<int, CheckBox> checkboxDictionary = new Dictionary<int, CheckBox>();
        private Dictionary<ListOfTests, Button> TestButtons = new Dictionary<ListOfTests, Button>();

        // Track the currently in-progress test.
        private ListOfTests TestInProgress;

        // Flags to indicate error acknowledgment and restart request.
        private bool errorAck = false;

        // Terminal instances for BOOT and MAIN.
        private ComManager Terminals = new ComManager();
        // Flag to track if the heartbeat is running.
        private bool HeartBeatRunning = false;

        // Constants for better maintainability.
        private const string BOOT_INITIAL_TEXT = "BOOT \n\r";

        private const int TERMINAL_TEXT_THRESHOLD = 10;

        public delegate void BoolStringEvent(bool status, string terminal);
        public event BoolStringEvent ConnectionUpdate;

        public delegate void StringStringEvent(string terminal , string port);
        public event StringStringEvent UpdateConfigPorts;
        // Instances of specific test classes.


        private Commissioning Commissioning_usb_Test;
        private Firmware Firmware_Test;
        private SSR SSR_Test;
        private ADC_Test adc_Test;
        private Ethernet Ethernet_Test;
        private Commissioning Commissioning_Test;
        private NormalOP NormalOP_Test;

        private DialogManager UI_Message = new DialogManager();
       // private ReplyProccessorState ReplyProccessor = new ReplyProccessorState();
        public MainWindow()
        {
            InitializeComponent();
            InitializeTests();
            InitializeEventHandlers();
            InitComPorts();
            
        }

        private void InitializeTests()
        {
            // Initialize a dictionary to hold available tests.
            Tests = new Dictionary<ListOfTests, Test_Template>();

            // Create instances of various test classes and add them to the dictionary.
            Commissioning_usb_Test = new Commissioning(Terminals.TerminalMAIN, Terminals.TerminalBOOT);

            Firmware_Test = new Firmware(Terminals.TerminalMAIN, Terminals.TerminalBOOT);
            SSR_Test = new SSR(Terminals.TerminalMAIN, Terminals.TerminalBOOT);
            adc_Test = new ADC_Test(Terminals.TerminalMAIN, Terminals.TerminalBOOT);
            Ethernet_Test = new Ethernet(Terminals.TerminalMAIN, Terminals.TerminalBOOT);
            Commissioning_Test = new Commissioning(Terminals.TerminalMAIN, Terminals.TerminalBOOT);
            NormalOP_Test = new NormalOP(Terminals.TerminalMAIN, Terminals.TerminalBOOT);
            // Add the tests to the dictionary with their associated test type.
            Tests.Add(ListOfTests.NONE, null);
            Tests.Add(ListOfTests.Commissioning_usb, Commissioning_usb_Test);
            Tests.Add(ListOfTests.Firmware, Firmware_Test);
            Tests.Add(ListOfTests.SSR_Reply, SSR_Test);
            Tests.Add(ListOfTests.ADC, adc_Test);
            Tests.Add(ListOfTests.Ethernet, Ethernet_Test);
            Tests.Add(ListOfTests.Commissioning, Commissioning_Test);
            Tests.Add(ListOfTests.NormalOp, NormalOP_Test);


            // Initialize a dictionary to hold buttons associated with tests.
            TestButtons.Add(ListOfTests.Commissioning_usb, Commisioning_usb_button);
            TestButtons.Add(ListOfTests.Firmware, Firmware_Button);
            TestButtons.Add(ListOfTests.SSR_Reply, Ssr_Replies_Button);
            TestButtons.Add(ListOfTests.ADC, ADC_Button);
            TestButtons.Add(ListOfTests.Ethernet, Ethernet_Button);
            TestButtons.Add(ListOfTests.Commissioning, Commisioning_button);
            TestButtons.Add(ListOfTests.NormalOp, Normal_Operation_Button);


            // Initialize the currently selected test as NONE.
            TestInProgress = ListOfTests.NONE;

            //  Loop through all initilised tests and initilise their event handler for error Log 
            var testsList = Tests.Values.ToList();
            for (int i = 1; i < testsList.Count; i++)
            {
                Test_Template test = testsList[i];
                // Perform actions on the 'test' object here
                // For example:
                test.LogError += LogErrorEvent;
                test.AttemptToConnect += AttemptConnection;
            }
        }

        private void InitializeEventHandlers()
        {
            // Register event handlers for TerminalBOOT and TerminalMAIN to handle terminal output.
            Terminals.TerminalBOOT.TerminalOutputReceived += TerminalBOOT_DataReceived;
            Terminals.TerminalMAIN.TerminalOutputReceived += TerminalMAIN_DataReceived;

            Terminals.TerminalMAIN.DisplayErrorMessage += LogErrorEvent;
            Terminals.TerminalBOOT.DisplayErrorMessage += LogErrorEvent;

            ReplyProccessorState.PowerState += PowerState;

            ConnectionUpdate += Globals.ConnectionUpdate; 
            ConnectionUpdate += ReplyProccessorState.ConnectionUpdate;
            UpdateConfigPorts += Terminals.UpdateConfigPorts;
            Terminals.ConnectionUpdate += Globals.ConnectionUpdate;
            Terminals.ConnectionUpdate += ReplyProccessorState.ConnectionUpdate;

            Terminals.TerminalConnected += TerminalConnected;

            Terminals.TerminalDisconnected += TerminalDisconnected;
            Terminals.DisplayErrorUI += DisplayErrorUI;

            UI_Message.DisplayErrorUI += DisplayErrorUI;
            UI_Message.ConnectionDialog.ConnectionDialogClosed += Manual_ConnectionDialog_Closed;
            Terminals.manualConnectionUpdate += UpdatePortsManual;

            
            
            ReplyProccessorState.manualConnectionUpdate += UpdatePortsManual;
            ReplyProccessorState.autoConnectionUpdate += UpdatePortsAuto;
            ReplyProccessorState.InitiateCommunicationMode();


        }



        ///////////////////////////////////// BUTTONS /////////////////////////////////

        private void Commisioning_button_Click(object sender, RoutedEventArgs e)
        {
            // Initialize the test for Commissioning when the corresponding button is clicked.

                InitiateBoard("Commissioning");
        }
        private void Commisioning_usb_button_Click(object sender, RoutedEventArgs e)
        {
            // Initialize the test for Commissioning when the corresponding button is clicked.
           
           InitiateBoard("Commissioning_usb");
        }

        private void Usb_Button_Click(object sender, RoutedEventArgs e)
        {
            // Initialize the test for USB when the corresponding button is clicked.
            

                InitiateBoard("USB");
        }

        private void Normal_Operation_Button_Click(object sender, RoutedEventArgs e)
        {
            // Initialize the test for Normal Operation when the corresponding button is clicked.

                InitiateBoard("NormalOp");
        }

        private async void ADC_Button_Click(object sender, RoutedEventArgs e)
        {
            InitiateBoard("ADC");
            
        }

        private void Ethernet_Button_Click(object sender, RoutedEventArgs e)
        {
            // Initialize the test for Ethernet when the corresponding button is clicked.
            InitiateBoard("Ethernet");

        }

        private void Firmware_Button_Click(object sender, RoutedEventArgs e)
        {

                
                InitiateBoard("Firmware");

        }

        private void Ssr_Replies_Button_Click(object sender, RoutedEventArgs e)
        {

                InitiateBoard("SSR_Reply");

        }

        private async void InitiateBoard(string test)
        {
            // Initialize the test for ADC when the corresponding button is clicked.
            //ReplyProccessorState.InitilisingTest = true; // flag to prevent multiple calls while a test is being initilised 

            if(!ReplyProccessorState.InitilisingTest)
            {
                ReplyProccessorState.InitilisingTest = true; // flag to prevent multiple calls while a test is being initilised 
                if (ReplyProccessorState.PowerMode == ReplyProccessorState.Modes.Manual && !ReplyProccessorState.Power) // IF NO POWER AND MANUAL MODE 
                {
                    
                    ReplyProccessorState.InitiatePower(test);
                }
                else if (ReplyProccessorState.PowerMode == ReplyProccessorState.Modes.Auto && !ReplyProccessorState.Power)
                {
                    // do some automatic power initiation 
                }
            }

        }
        
        /////////////////////////TERMINAL METHODS //////////////////////////////////////////////

        private async void InitComPorts()
        {
            if(ReplyProccessorState.ConnectionMode == ReplyProccessorState.Modes.Auto)
            {
                BootPort.Visibility = Visibility.Visible;
                MainPort.Visibility = Visibility.Visible;

                BootPort.IsEnabled = true;
                MainPort.IsEnabled = true;

                BootPort.Text = Terminals.GetTerminalBootPort();
                MainPort.Text = Terminals.GetTerminalMainPort();
            }
            else
            {


                UpdatePortsManual(null,EventArgs.Empty);
            }



        }
        private async void UpdatePortsManual(object sender, EventArgs e)
        {
            BootPort.Visibility = Visibility.Hidden;
            MainPort.Visibility = Visibility.Hidden;

            BootPort.IsEnabled = false;
            MainPort.IsEnabled = false;

            MainPort_manual.Visibility = Visibility.Visible;
            BootPort_manual.Visibility = Visibility.Visible;

            MainPort_manual.IsEnabled = true;
            BootPort_manual.IsEnabled = true;




        }
        private async void ComPort_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                // Handle the drop-down opened event for Port A ComboBox.
                // This method asynchronously populates the Port A ComboBox with available serial ports.
                BootPort_manual.ItemsSource = await Terminals.TerminalBOOT.ReturnSerialPortsAsync();
                MainPort_manual.ItemsSource = await Terminals.TerminalMAIN.ReturnSerialPortsAsync();
            }
            catch (Exception ex)
            {
                // In case of an exception, inform the user about the error.
                MessageBox.Show($"Error populating ports: {ex.Message}");
            }
        }
        private async void UpdatePortsAuto(object sender, EventArgs e)
        {

            InitComPorts();
        }

        private void TerminalConnected(object sender , string terminal)
        {
            UpdateUI_Terminal_Connected(terminal);
        }

        private void TerminalDisconnected(object sender, string terminal)
        {
            UpdateUI_Terminal_Disconnected(terminal);

        }
        private void ConnectBOOT_Button_Click(object sender, RoutedEventArgs e)
        {
            if(ReplyProccessorState.ConnectionMode == ReplyProccessorState.Modes.Auto)
            {
                Terminals.ConnectBoot();
            }
            else
            {
                if(Terminals.TerminalBOOT.Connect((string)BootPort_manual.SelectedItem))
                {
                    UpdateUI_Terminal_Connected("BOOT");
                    ConnectionUpdate?.Invoke(true, "BOOT");
                    string temp = (string)BootPort_manual.SelectedItem;
                    UpdateConfigPorts?.Invoke("BOOT", temp.Replace("COM", ""));

                    BootPort_manual.Text = (string)BootPort_manual.SelectedItem;
                }


            }
           
            
        }

        private void ConnectMAIN_Button_Click(object sender, RoutedEventArgs e)
        {
           
            if (ReplyProccessorState.ConnectionMode == ReplyProccessorState.Modes.Auto)
            {
                Terminals.ConnectMain();
            }
            else
            {
                if(Terminals.TerminalMAIN.Connect((string)MainPort_manual.SelectedItem))
                {
                    UpdateUI_Terminal_Connected("MAIN");
                    ConnectionUpdate?.Invoke(true, "MAIN" );
                    string temp = (string)MainPort_manual.SelectedItem;
                    UpdateConfigPorts?.Invoke("MAIN", temp.Replace("COM", ""));

                    MainPort_manual.Text = (string)MainPort_manual.SelectedItem; 
                }
                
            }
        }

        private void DisconnectBoot_Button_Click(object sender, RoutedEventArgs e)
        {
            // Disconnect the BOOT terminal.
            Terminals.DisconnectBoot( );

        }


        private void DisconnectMain_Button_Click(object sender, RoutedEventArgs e)
        {

            // Disconnect the Main terminal.
            Terminals.DisconnectMain();
        }

        private void DisconnectTerminals()
        {
            // This method disconnects both the main and boot terminals and updates their statuses and related UI elements.
            
            Terminals.DisconnectMain();
            Terminals.DisconnectBoot();
        }

        private void Previous_instruction_button_Click(object sender, RoutedEventArgs e)
        {
            // Handle the click event for the "Previous Instruction" button.
            // This method moves to the previous instruction for the test in progress.
            PreviousInstruction();
        }

        private void Next_instruction_button_Click(object sender, RoutedEventArgs e)
        {
            // Handle the click event for the "Next Instruction" button.
            // This method progresses to the next instruction for the test in progress.
            NextInstruction();
        }

        private void UpdateUI_Terminal_Disconnected(string terminal)
        {
            if (terminal == "BOOT")
            {
                TerminalBOOT_status.Text = "BOOT : Disconnected";
                TerminalBOOT_status.Foreground = Brushes.Red;

            }
            else // must be main
            {
                // Update UI to indicate unsuccessful connection to the MAIN terminal.
                TerminalMAIN_status.Text = "MAIN : Disconnected";
                TerminalMAIN_status.Foreground = Brushes.Red;

            }
        }
        private void UpdateUI_Terminal_Connected(string terminal)
        {
            if (terminal == "BOOT")
            {
                // Update UI to indicate successful connection to the BOOT terminal.
                TerminalBOOT_status.Text = "BOOT : Connected";
                TerminalBOOT_status.Foreground = Brushes.Green;

                // Start the SerialPortHeart method to monitor the connection status.
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    SerialPortHeart(); // Activate heartbeat to alert the user and attempt reconnection if the connection is lost.
                });
            }
            else
            {
                // Update UI to indicate successful connection to the MAIN terminal.
                TerminalMAIN_status.Text = "MAIN : Connected";
                TerminalMAIN_status.Foreground = Brushes.Green;

                // Start the SerialPortHeart method to monitor the connection status.
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    SerialPortHeart(); // Activate heartbeat to alert the user and attempt reconnection if the connection is lost.
                });
            }
        }
        ////////////////////////////////////////////////////////////////// Terminal Event Handlers ///////////////////////////////////////////////////////////////////////////////////////////////////
        //

        private async void SerialPortHeart()
        {
            // This method monitors and maintains the connection status for either the BOOT or MAIN terminal,
            // depending on which one is connected. If the connected terminal loses connection,
            // attempts to reconnect are made. If multiple reconnection attempts fail,
            // the terminal is considered disconnected.
            bootAttempts = 0;
            mainAttempts = 0;
            if (HeartBeatRunning) { return; }// Avoid race conditions

            HeartBeatRunning = true;
            while (Terminals.TerminalBOOT.IsConnected || Terminals.TerminalMAIN.IsConnected)
            {
                if (Terminals.TerminalBOOT.IsConnected)
                {
                    await MonitorConnection(Terminals.TerminalBOOT, "BOOT", Terminals.DisconnectBoot, bootAttempts);
                }
                else if (Terminals.TerminalMAIN.IsConnected)
                {
                    await MonitorConnection(Terminals.TerminalMAIN, "MAIN", Terminals.DisconnectMain, mainAttempts);
                }


                if(!Terminals.TerminalBOOT.IsConnected)
                {
                    Terminals.TerminalBOOT.Connect(Terminals.TerminalBOOT.PreviouslyConnectedPort);
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            HeartBeatRunning = false;
        }

        private async Task MonitorConnection(Terminal terminal, string terminalName, Action disconnectMethod, int ConnectionAttempts)
        {
            // This method checks the connection status of a given terminal. If the terminal is not connected, it tries to reconnect.
            // If multiple reconnection attempts fail, it calls the provided disconnection method for the terminal.

            if (terminal.IsConnected && !terminal.ReturnConnectionStatus())
            {
                try
                {
                    // Try to reconnect the terminal.
                    if (!ReconnectTerminal(terminal))
                    {
                        ConnectionAttempts++;
                    }
                }
                catch (Exception ex)
                {
                    // Handle the case when the terminal connection is lost.
                    LogError(ex.Message);
                    await Dispatcher.InvokeAsync(disconnectMethod);
                }
                finally
                {
                    // If multiple reconnection attempts fail, reset the attempt counter.
                    if (ConnectionAttempts >= 3)
                    {
                        LogError($"Lost Connection to {terminalName}");
                        await Dispatcher.InvokeAsync(disconnectMethod);
                    }
                }
            }
        }

        private bool ReconnectTerminal(Terminal terminal)
        {
            // This method attempts to reconnect to the provided terminal.
            // Returns true if the reconnection is successful, and false otherwise.

            string connection = terminal.PortNumber;
            return terminal.ReConnect(connection);
        }



        private void TerminalBOOT_DataReceived(object sender, string data)
        {
            // Handle data reception for the BOOT terminal.
            Dispatcher.Invoke(() =>
            {
                AppendDataAndScrollToEnd(Terminal_BOOT, data);

                /*
            if (IsTestInProgress())
            
                {
                    if(Tests[TestInProgress].StartDisplayingTerminal())
                    {
                        AppendDataAndScrollToEnd(Terminal_BOOT, data);
                    }
                    
                }*/
            });
        }

        private void TerminalMAIN_DataReceived(object sender, string data)
        {
            // Handle data reception for the MAIN terminal.
            Dispatcher.Invoke(() =>
            {
                //if (IsTestInProgress())
                //{
                    if (Terminal_MAIN.Text.Contains("BOOT processor") && Terminals.TerminalMAIN.Terminal_Log.Count < 5)
                    {
                        TerminalsReversed();
                    }
                                            
                    AppendDataAndScrollToEnd(Terminal_MAIN, data);
                //}
            });
        }

        // Helper method to append data and scroll to the end of a terminal.
        private void AppendDataAndScrollToEnd(TextBox textbox, string data)
        {
            Dispatcher.Invoke(() =>
            {
                textbox.AppendText('\n' + data);
                textbox.ScrollToEnd();
            });
        }

        private void TerminalsReversed()
        {
            // This method checks if the terminals are reversed by inspecting the text in the 'Terminal_MAIN' box.
            // If the terminals are detected to be reversed, it prompts the user with a message box and updates
            // the status of the current test to "Restarting".

            // Check if the 'Terminal_MAIN' text contains a specific string indicating reversed terminals.

                // Prompt the user with a warning message.
                MessageBoxResult Ack = MessageBox.Show("Terminals have been connected backwards , attempting to swap ", "WARNING ", MessageBoxButton.OK);

            // If the user acknowledges the warning.
            if (Ack == MessageBoxResult.OK)
            {
                DisconnectTerminals();
                if (Terminal_MAIN.Text.Contains("BOOT processor"))
                {
                    Terminal_BOOT.Text = Terminal_MAIN.Text;
                    Terminal_MAIN.Clear();

                }
                Terminals.ReverseTerminalSettings();
                //BootPort.Text = Terminals.GetTerminalBootPort();
                //MainPort.Text = Terminals.GetTerminalMainPort();
            }
                    


                
            
        }
        private void AttemptConnection(object sender, string terminalToConnect)
        {
            //  Log and Display Errors and events
            Dispatcher.Invoke(() =>
            {
                if(terminalToConnect == "MAIN")
                {
                    Terminals.ConnectMain();
                }
                else
                {
                    Terminals.ConnectBoot();
                }
            });
        }
        ////////////////////////////////////////////////////////////////// Instruction Methods ///////////////////////////////////////////////////////////////////////////////////////////////////
        private bool IsTestInProgress()
        {
            if(TestInProgress != ListOfTests.NONE)
            {
                return true;
            }
            else
            {
                return false;
            }
           
        }

        private void NextInstruction()
        {
            // If there's a valid test in progress.
            if (IsTestInProgress())
            {
                if (Tests[TestInProgress].PerformInstructionCheck().Result)
                {
                    
                    Tests[TestInProgress].MoveToNextInstruction(); // attempt to move to next instruction 
                    // Update the instruction box with the current instruction.
                    instructions_box.Text = Tests[TestInProgress].CurrentInstruction;
                    //if(currentInstruction.userAckRequired && !NextInstruction.CheckCompleted || !currentInstruction.CheckRequired)
                    var currentInstruction = Tests[TestInProgress].TestInstructions[Tests[TestInProgress].PublicIndex]; // declare current instruction after checks , otherwise it will use the previous instruction 
                    if (currentInstruction.userAckRequired)
                    {
                        ShowCurrentInstruction();
                    }
                    
                    

                    // Update any associated checkboxes for the instruction.
                    UpdateCheckBoxes();
                }
            }
        }

        private void PreviousInstruction()
        {
            var currentInstruction = Tests[TestInProgress].TestInstructions[Tests[TestInProgress].PublicIndex];
            // If there's a valid test in progress.
            if (IsTestInProgress())
            {
                // Move to the previous instruction in the test.
                Tests[TestInProgress].MoveToPreviousInstruction();

                // Update the instruction box with the current instruction.
                instructions_box.Text = Tests[TestInProgress].CurrentInstruction;
                if(!currentInstruction.CheckRequired && !currentInstruction.CheckCompleted )
                {
                    ShowCurrentInstruction(true);
                }
                
            }
        }

        private void ClearInstructions()
        {
            // This method clears the instructions box and resets terminal boxes.
            // It ensures all UI updates related to instructions and terminals are done safely.

            Dispatcher.Invoke(() =>
            {
                instructions_box.Clear();
                //Terminal_BOOT.Clear();
                Terminal_MAIN.Clear();

                // Set default texts for the terminals.
                Terminal_BOOT.Text = "BOOT \n\r";
                Terminal_MAIN.Text = "MAIN \n\r";
            });
        }

        private async void UpdateUI()
        {
            // This method asynchronously updates the UI elements based on the status and progress
            // of the current test. It handles different stages of the test and manages checkbox displays.

            try
            {
                var Test = Tests[TestInProgress];
                var Button = TestButtons[TestInProgress];

                // If a test is currently in progress.
                if (IsTestInProgress())
                {
                    Globals.IsTestInProgress = true;

                    // While the test is actively in progress, manage UI updates.
                    while (Test.Status == Test_Template.CurrentStatus.InProgress)
                    {
                        await Dispatcher.InvokeAsync(async () =>
                        {
                            Button.Background = Brushes.Yellow;

                            // Check for test instruction progress.
                            if (await Task.Run(() => Test.PerformInstructionCheck()))
                            {
                                // If not all instructions are complete.
                                if (!Test.AllInstructionsComplete())
                                {
                                    // Update UI based on the current instruction's requirements.
                                    if (!Test.TestInstructions[Test.PublicIndex].CheckRequired)
                                    {
                                        next_instruction_button.Background = Brushes.Green;
                                    }
                                    else if (Test.TestInstructions[Test.PublicIndex].CheckRequired && Test.TestInstructions[Test.PublicIndex].CheckCompleted)
                                    {
                                        NextInstruction();
                                        next_instruction_button.Background = Brushes.White;
                                    }
                                }
                                else
                                {
                                    Test.Status = Test_Template.CurrentStatus.Completed;
                                }
                            }
                        });

                        UpdateCheckBoxes();
                        await Task.Delay(TimeSpan.FromSeconds(1)); // Delay to prevent overloading the UI thread.
                    }

                    // Handle scenarios when the test is no longer in progress (either completed or failed).
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (Test.Status == Test_Template.CurrentStatus.Failed)
                        {
                            TestFailed(); //invoke the failed test logic if the status is changed to failed
                            Globals.IsTestInProgress = false; // it was failed so not in progress
                            DisconnectTerminals(); //Disconnect terminals as test will need to be restarted
                            ClearCheckBoxes();// Clear checkboxes
                        }
                        else if (Test.Status == Test_Template.CurrentStatus.Completed)
                        {
                            UpdateCheckBoxes(); // update check boxes before completeing test as last one wont be updated
                            TestComplete(); // Update UI and other elements as the test has passed
                            Globals.IsTestInProgress = false; // it was succesfull so not in progress
                            DisconnectTerminals(); //Disconnect terminals as test will need to be restarted
                            ClearCheckBoxes();// Clear checkboxes
                        }
                        else if (Test.Status == Test_Template.CurrentStatus.Restarting)
                        {
                            TestFailedRestartReq(); //invoke the failed test logic if the status is changed to failed
                            Globals.IsTestInProgress = false; // it was failed so not in progress
                            DisconnectTerminals(); //Disconnect terminals as test will need to be restarted
                            ClearCheckBoxes();// Clear checkboxes
                        }
                    });
                }
                else
                {
                    ClearCheckBoxes();
                }
            }
            catch (Exception ex)
            {
                // Display the error message to the user.
                LogError(ex.Message);
                
            }
        }

        private void PopulateCheckboxesFromInstructions()
        {
            // This method populates the UI containers with checkboxes based on the instructions
            // for the test in progress. The checkboxes provide visual feedback to the user.
            try
            {
                if(TestInProgress != ListOfTests.NONE)
                {
                    // Retrieve checkbox information for the test in progress.
                    Dictionary<int, string> CheckBoxes = Tests[TestInProgress].ReturnCheckBoxInfo();

                    int checkBoxCount = 0;

                    // Clear existing checkboxes from the UI containers.
                    CheckboxesContainer1.Children.Clear();
                    CheckboxesContainer2.Children.Clear();

                    // Fetch the last key value once before entering the loop to optimize performance.
                    int lastKeyValue = CheckBoxes.Keys.LastOrDefault();

                    // Iterate over checkbox information to create and position checkboxes in the UI.
                    foreach (var kvp in CheckBoxes)
                    {

                        int index = kvp.Key - 1; // Adjust index to 0-based

                        if (index <= lastKeyValue)
                        {
                            string checkBoxContent = kvp.Value;

                            // Initialize a new checkbox with provided content.
                            CheckBox checkBox = new CheckBox
                            {
                                Name = $"check_{index + 1}",
                                Content = checkBoxContent,
                                VerticalAlignment = VerticalAlignment.Center,
                                Visibility = Visibility.Visible,
                                IsEnabled = false
                            };

                            checkBoxCount++;

                            // Decide which container to add the checkbox to based on current count.
                            if (checkBoxCount < 6)
                            {
                                CheckboxesContainer1.Children.Add(checkBox);
                            }
                            else
                            {
                                CheckboxesContainer2.Children.Add(checkBox);
                            }

                            // Add the checkbox to the main dictionary for future reference.
                            checkboxDictionary[index] = checkBox;
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                
            }
            
            
        }

        private void ClearCheckBoxes()
        {
            // This method clears all checkboxes from the UI and removes
            // the associated entries from the checkbox dictionary.

            // Clear the dictionary containing checkbox references.
            checkboxDictionary.Clear();

            // Clear checkboxes from the UI containers.
            CheckboxesContainer1.Children.Clear();
            CheckboxesContainer2.Children.Clear();
        }

        private void UpdateCheckBoxes()
        {
            // This method updates checkboxes based on the test's progress. Specifically,
            // it checks a checkbox that corresponds to a particular instruction once
            // the test progresses beyond a certain point.

            // Ensure there's a test in progress and the test exists in the 'Tests' dictionary.
            if (TestInProgress == ListOfTests.NONE || !Tests.ContainsKey(TestInProgress))
            {
                return; // Exit if no valid test is in progress.
            }

            // Wait until the test is at least 2 instructions in before checking a checkbox.
            if (Tests[TestInProgress].PublicIndex > 2)
            {
                int requiredKey = Tests[TestInProgress].PublicIndex - 2;

                // If the dictionary contains the required key, update the corresponding checkbox.
                if (checkboxDictionary.ContainsKey(requiredKey))
                {
                    CheckBox checkbox = checkboxDictionary[requiredKey];

                    if (checkbox != null)
                    {
                        checkbox.IsChecked = true;
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////// Test Methods ///////////////////////////////////////////////////////////////////////////////////////////////////
        private void IsTestRunning()
        {
            // This method updates the instruction box based on the current test's progress status.

            // If a test is in progress, notify the user about the specific test running.
            if (IsTestInProgress())
            {
                instructions_box.Clear();
                instructions_box.AppendText($"! TEST IN PROGRESS ! \n Test In Progress {Enum.GetName(typeof(ListOfTests), TestInProgress)}");
            }
            // If no test is in progress, prompt the user to select a test.
            else
            {
                instructions_box.Clear();
                instructions_box.AppendText("Select A test");
            }
        }



        private void Restart_Req(object sender, RoutedEventArgs e)
        {
            {
                // This method handles the request to restart a test. It updates the test status and logs the restart request.

                try
                {
                    // Only process if a test is in progress and it hasn't been completed.
                    if (IsTestInProgress() && Tests[TestInProgress].Status != Test_Template.CurrentStatus.Completed)
                    {
                        // Mark the test status as failed.
                        Tests[TestInProgress].Status = Test_Template.CurrentStatus.Failed;

                        // Log the restart request for the current test.
                        Login_Page.CDataLog.RestartRequested(TestInProgress.ToString());

                        // Note: Once the test status has been changed, the UpdateUI method is responsible for ushering
                        // an update based on the status of the test.
                    }
                }
                catch (Exception ex)
                {
                    // Notify the user if there's an issue (e.g., no test selected).
                    LogError(ex.Message);
                }
            }
        }

        

        private void AbortTest_Req(object sender, RoutedEventArgs e)
        {
            //This Method handles the Abortion of a test that is in progress , it will incrememt the attempts for the current test 
            try
            {
                if(IsTestInProgress() && Tests[TestInProgress].Status != Test_Template.CurrentStatus.Completed)
                {
                    TestFailedRestartReq();
                    // Log the Abort request for the current test.
                    Login_Page.CDataLog.AbortRequest(TestInProgress.ToString());
                }
            }
            catch(Exception ex)
            {
                LogError(ex.Message); // Log Any errors 
            }
        }
        private void TestComplete()
        {
            // This method handles the tasks required after a test is successfully completed.
            // It updates the UI, logs the outcome, and resets test progress indicators.

            var Test = Tests[TestInProgress];
            var Button = TestButtons[TestInProgress];

            // Update the test button to green indicating successful completion.
            Button.Background = Brushes.Green;

            // Log and mark the test as passed, and complete any associated tasks.
            Test.AddTestPointOutcome(TestInProgress.ToString(), "PASS");
            Test.TestComplete(TestInProgress.ToString());

            // Clear any instructions displayed to the user.
            ClearInstructions();

            // Reset the current test in progress indicators.
            TestInProgress = ListOfTests.NONE;
            Globals.TestInProgressGlobal = TestInProgress.ToString();
        }

        private void TestFailed()
        {
            // This method handles the tasks required after a test failure.
            // It updates the UI, logs the outcome, provides the user with feedback,
            // and adjusts the test state based on the number of attempts made.

            var Test = Tests[TestInProgress];
            var Button = TestButtons[TestInProgress];

            // Log the current status of the test.
            Login_Page.CDataLog.AddTestOutcome(Tests[TestInProgress].Status.ToString());

            // Reset the test for a new attempt.
            Test.Reset_Test();

            // Clear any instructions and notify the user about remaining attempts.
            ClearInstructions();
            MessageBox.Show($"{3 - Test.TestAttempt} Attempts Left for:  {TestInProgress} Test");

            // Adjust UI and test state based on the number of attempts made.
            if (Test.TestAttempt <= 2)
            {
                Button.Background = Brushes.Orange;
                Test.Status = Test_Template.CurrentStatus.Not_Started;
                Test.testStateChanged = false;
            }
            else if (Test.TestAttempt == 3)
            {
                Button.Background = Brushes.Red;
            }

            // Reset the current test in progress indicators.
            TestInProgress = ListOfTests.NONE;
            Globals.TestInProgressGlobal = TestInProgress.ToString();
        }

        private void TestFailedRestartReq()
        {
            // This method handles scenarios requiring a test restart, which doesn't necessarily mean the test has failed.
            // For example, it may be invoked if the terminals are connected the wrong way around.
            // It adjusts the UI and internal state based on the number of test attempts.

            // Retrieve the current test and its associated button.
            var Test = Tests[TestInProgress];
            var Button = TestButtons[TestInProgress];
            ReplyProccessorState.InitilisingTest = false;
            // Log the current status of the test.
            Login_Page.CDataLog.AddTestOutcome(Tests[TestInProgress].Status.ToString());

            // Reset the test and clear instructions.
            Test.Reset_Test_Restart();
            ClearInstructions();

            // Update UI and status based on the number of attempts for the test.
            if (Test.TestAttempt == 0)
            {
                Button.Background = Brushes.White;
                Test.Status = Test_Template.CurrentStatus.Not_Started;
                Test.testStateChanged = false;
            }
            else if (Test.TestAttempt <= 2)
            {
                Button.Background = Brushes.Orange;
                Test.Status = Test_Template.CurrentStatus.Not_Started;
                Test.testStateChanged = false;
            }
            else if (Test.TestAttempt == 3)
            {
                Button.Background = Brushes.Red;
            }

            // Reset global and local test in progress indicators.
            TestInProgress = ListOfTests.NONE;
            Globals.TestInProgressGlobal = TestInProgress.ToString();
            DisconnectTerminals();
            errorAck = false; // Reset the error acknowledgment.
        }
        private async void PowerState(object sender, string test)
        {
            ReplyProccessorState.InitilisingTest = true;
            await Terminals.AttemptToConnectTerminals(); // attempt to connect terminals together 

            if (ReplyProccessorState.AreBothTerminalsConnected())
            {
                if (test != "NormalOp") // nothing to transfer for normal operation 
                {
                    if (await Terminals.TransferTestFromUsb(test))
                    {
                        ListOfTests enumOfTest = (ListOfTests)Enum.Parse(typeof(ListOfTests), test);
                        InitiliseTest(enumOfTest);
                    }
                    else
                    {
                        UI_Message.DisplayErrorMessage("Failed To Transfer from USB ");
                        ReplyProccessorState.InitilisingTest = false;
                    }

                }
            }
            else //issue with automatic connections 
            {
                UI_Message.DisplayErrorMessage("Failed To Connect Automatically ");

                UI_Message.DisplayManualConnection(test); //display connection update 
            }
        }
        private async void Manual_ConnectionDialog_Closed(object sender, string test)
        {
            if (ReplyProccessorState.AreBothTerminalsConnected())
            {
                if (await Terminals.TransferTestFromUsb(test))
                {
                    
                    ListOfTests enumOfTest = (ListOfTests)Enum.Parse(typeof(ListOfTests), test);
                    InitiliseTest(enumOfTest);
                }
                else
                {
                    UI_Message.DisplayErrorMessage("Failed To Transfer from USB ");
                    DisconnectTerminals(); 
                    ReplyProccessorState.InitilisingTest = false;
                }
            }
            else
            {
                UI_Message.DisplayErrorMessage("Failed to connect to Terminals ");
                DisconnectTerminals();
                ReplyProccessorState.InitilisingTest = false;
            }
        }
        private void InitiliseTest(ListOfTests Test)
        {
            // This method initializes a selected test if no other test is currently in progress.
            // It adjusts the UI based on the test status and provides necessary instructions to the user.

            // Return if a test is already in progress.
            if (IsTestInProgress())
            {
                MessageBox.Show("   Test Running     " , "  Error   ",MessageBoxButton.OK);
                return;
            }

            // Clear any prior instructions.
            ClearInstructions();

            // Notify the user if the selected test is completed.
            if (Tests[Test].Status == Test_Template.CurrentStatus.Completed)
            {
                instructions_box.Text = "Test Completed \n Please Select Another Test ";
            }
            // If the test hasn't been started, start it.
            else if (Tests[Test].Status == Test_Template.CurrentStatus.Not_Started)
            {
                TestInProgress = Test;
                Globals.TestInProgressGlobal = TestInProgress.ToString();
                Tests[Test].InitializeInstructions();
                Tests[Test].Status = Test_Template.CurrentStatus.InProgress;
                instructions_box.AppendText(Tests[Test].CurrentInstruction); 
                TestButtons[Test].Background = Brushes.Yellow;
                PopulateCheckboxesFromInstructions();
                
                UpdateUI();
                ShowCurrentInstruction();
            }


            ReplyProccessorState.InitilisingTest = false;
        }



        private async void ShowCurrentInstruction(bool previousInstructionReq = false )
        {
            bool moveToNextInstruction = false; 
            try
            {
               
                MessageBoxResult userRespoce =  MessageBox.Show($"{Tests[TestInProgress].CurrentInstruction}" , "Instruction",MessageBoxButton.OKCancel,MessageBoxImage.Information);
                if(userRespoce == MessageBoxResult.Cancel)
                {
                    TestFailedRestartReq();
                     moveToNextInstruction = false ;
                }
                else 
                {
                    if(!previousInstructionReq) // not a request for a previous instruction 
                    {

                        if(Tests[TestInProgress].CurrentInstruction.Contains("Manual Power Set-UP 6") && Terminals.TerminalBOOT.HasPreviouslyBeenConnected && Terminals.TerminalMAIN.HasPreviouslyBeenConnected ) // if the current instruction is power on the board
                        {
                            // once aknowlaged then while both terminals remain disconnected 
                       
                           while(!Globals.IsTerminal_B_Connected && !Globals.IsTerminal_M_Connected )
                            {
                                await Task.Delay(TimeSpan.FromSeconds(20)); // wait 20 seconds for the board to power up 
                                
                                Terminals.ConnectBoot(); // connect 
                                Terminals.ConnectMain(); // connect 

                            }
                        }
                        
                         moveToNextInstruction = true;
                    
                    }
                    else
                    {
                        MessageBox.Show($"To go to next Instruction press\n\n 'NEXT' ", "Instruction", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                         moveToNextInstruction = false;
                    }
                    
                }
                if(moveToNextInstruction)
                {
                    NextInstruction(); // move to next instruction 
                }
            }
            catch(Exception ex)
            {
                LogError(ex.Message);
              
            }   
        }


        /////////ERROR LOGGING ///////////////////////////
        //
        private void LogError(string ErrorToLog)
        {
            AppendDataAndScrollToEnd(ApplicationLog, ErrorToLog);
        }
        private void LogErrorEvent(object sender, string errorMessage)
        {
            //  Log and Display Errors and events
            Dispatcher.Invoke(() =>
            {
                AppendDataAndScrollToEnd(ApplicationLog,errorMessage);
            });
        }
        private void DisplayErrorUI(object sender, string errorMessage)
        {
            //  Log and Display Errors and events
            Dispatcher.Invoke(() =>
            {
                AppendDataAndScrollToEnd(ApplicationLog, errorMessage);
            });
        }

    }
}