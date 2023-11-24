using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TestAssistant.Classes;
using TestAssistant.HardwareState;

namespace TestAssistant.Dialog
{

    class PowerOption
    {
        private string Title = "        POWER OPTION :          ";

        private List<string> ManualInstructions = new List<string>();
        private CustomMessageBox manualPowerInstruction;
        // private List<string> ManualInstruction = new List<string>();



        private CustomMessageBox PowerMessage;

        private int InstructionIndex = 0;
        public PowerOption()
        {
             

        }
        public void ShowPowerDialog()
        {
            InstructionIndex = 0;
            //Start the power dialog with the user 
            InitilisePowerInstructions();

            

        }
        public void InitilisePowerInstructions()
        {
            
            try
            {
                PowerMessage = new CustomMessageBox();
                PowerMessage.ButtonClick += Button_Click;

                PowerMessage.Title = $"{Title}";
                PowerMessage.MainMessage.Text = "\t\t PICK A POWER OPTION \t\t ";
                 SetButtonConfigurationInitial();

                PowerMessage.Show();
                // Return the result based on the button clicked
                
            }
            catch(Exception ex)
            {
                
            }

        }

        private void SetButtonConfigurationInitial()
        {
            PowerMessage.LeftButton.Content = "Manual";
            PowerMessage.MiddleButton.Visibility = Visibility.Collapsed;
            PowerMessage.RightButton.Content = "Auto";
        }
        private void Button_Click(object sender, Button buttonClicked)
        {
            if(buttonClicked.Content != null)
            {
                switch(buttonClicked.Content)
                {
                    case "Manual":
                     OnManualButtonClick();
                        break;
                    case "Auto":
                        OnAutomaticButtonClick();
                        break;
                    case "Previous":
                        OnPreviousButtonClick();
                        break;
                    case "Next":
                        OnNextButtonClick();
                        break;
                    case "Abort":
                        OnAbortButtonClick();
                        break;
                    default:
                        break;
                }
            }

        }

        private void OnManualButtonClick()
        {
            ReplyProccessorState.PowerMode = ReplyProccessorState.Modes.Manual;
            Globals.InhibitConnectionInstructions = ReplyProccessorState.Auto_Connect;
            PowerMessage.Close();
            InitiliseManualPowerInstructions();

        }
        private void OnAutomaticButtonClick()
        {
            Globals.InhibitConnectionInstructions = ReplyProccessorState.Auto_Connect;
            ReplyProccessorState.PowerMode = ReplyProccessorState.Modes.Auto;
            PowerMessage.Close();
        }

        private void InitiliseManualPowerInstructions()
        {

            ManualInstructions = new List<string>();
            ManualInstructions.Add("Manual Power Set-UP \n 1 - Press the 'Power' Button ");
            ManualInstructions.Add("Manual Power Set-UP 2\n 2- Press 'Display Limit' Button  ");
            ManualInstructions.Add("Manual Power Set-UP 3\n 3- Adjust the Left hand Value (Voltage ) to 5v \n Using the Knob on the left hand side\n Display should show : 05.000V");
            ManualInstructions.Add("Manual Power Set-UP 4\n 4- Press the 'Voltage/Current' Button to change selection to Amps");
            ManualInstructions.Add("Manual Power Set-UP 5\n 5- Adjust the right hand Value (Voltage ) to 6Amps \n Using the Knob on the left hand side\n Display should show : 6.000A");
            ManualInstructions.Add("Manual Power Set-UP 6\n Finnaly Press the 'Output ON/OFF' button to toggle power ");

            manualPowerInstruction = new CustomMessageBox();

            manualPowerInstruction.ButtonClick += Button_Click;
            manualPowerInstruction.LeftButton.Content = "Previous";
            manualPowerInstruction.MiddleButton.Content = "Next";
            manualPowerInstruction.RightButton.Content = "Abort";
            manualPowerInstruction.MainMessage.Text = ManualInstructions[InstructionIndex];

            //manualPowerInstruction.Closed += WindowClosed;
            manualPowerInstruction.Show();
        }

        private void OnPreviousButtonClick()
        {
            if(InstructionIndex != 0)
            {
                InstructionIndex--;
                manualPowerInstruction.MainMessage.Text = ManualInstructions[InstructionIndex];
            }
            
        }
        private void OnNextButtonClick()
        {
            if (InstructionIndex != ManualInstructions.Count -1) // if not at the last index 
            {
                InstructionIndex ++ ;
                manualPowerInstruction.MainMessage.Text = ManualInstructions[InstructionIndex];
            }
            else // we have come to the end of the Instructions 
            {
                
                
                manualPowerInstruction.Close();
                ReplyProccessorState.Power = true;
            }

        }
        private void OnAbortButtonClick()
        {
            ReplyProccessorState.Power = false;
            manualPowerInstruction.Close();
            

        }

        /*private void WindowClosed(object sender, EventArgs e)
        {
            ReplyProccessorState.InitilisingTest = false;
        }*/

    }


}
