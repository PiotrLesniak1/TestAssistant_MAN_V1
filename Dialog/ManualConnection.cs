using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TestAssistant.Dialog
{
    class ManualConnection 
    {
        private string Title = "        Manual Connection           ";

        private List<string> ManualInstructions = new List<string>();

        private CustomMessageBox ManualConnectionInstruction;

        public event EventHandler<string> ConnectionDialogClosed;

        private string TestRequested; 
        public void ShowConnectionDialog(string test)
        {
            TestRequested = test;
             
            //Start the power dialog with the user 
            InitiliseConnectionInstructions();
            ManualConnectionInstruction.ResetIndex();


        }

        public void InitiliseConnectionInstructions()
        {

            try
            {
                ManualConnectionInstruction = new CustomMessageBox();
                ManualConnectionInstruction.ButtonClick += Button_Click;
                ManualConnectionInstruction.SetButtonConfiguration(CustomMessageBox.BUTTON.PreviousNextExit);

                ManualConnectionInstruction.Title = $"{Title}";



                InitiliseManualPowerInstructions();

                
                

            }
            catch (Exception ex)
            {

            }

        }
        private void InitiliseManualPowerInstructions()
        {

            ManualInstructions = new List<string>();
            ManualInstructions.Add("\tConnect the USB end of your JTAG cable to your PC.");
            ManualInstructions.Add("\tConnect the JTAG end of the cable to \n\t the BOOT Processor Slot (JTAG 1).");
            ManualInstructions.Add("\tSelect the COM port and click \"Connect.\"");
            ManualInstructions.Add("\tConnect the USB end of your JTAG cable to your PC.");
            ManualInstructions.Add("\tConnect the JTAG end of the cable to \n\t the MAIN Processor Slot (JTAG 2).");
            ManualInstructions.Add("\tSelect the COM port and click \"Connect.\"");
            ManualInstructions.Add("\t Press Next when both have been connected \n\n\t Alternativly exiting the window will abort test initilisation ");




            ManualConnectionInstruction.MainMessage.Text = ManualInstructions[ManualConnectionInstruction.InstructionIndex];
            ManualConnectionInstruction.Show();
        }

        private void Button_Click(object sender, Button buttonClicked)
        {
            string name = sender.ToString();

            if (buttonClicked.Content != null)
            {
                switch (buttonClicked.Content)
                {

                    case "Previous":
                        OnPreviousButtonClick();
                        break;
                    case "Next":
                        OnNextButtonClick();
                        break;
                    case "Exit":
                        OnExitButtonClick();
                        break;

                    default:
                        break;
                }
            }

        }
        private void OnPreviousButtonClick()
        {
            if (ManualConnectionInstruction.InstructionIndex != 0)
            {
                ManualConnectionInstruction.InstructionIndex--;
                ManualConnectionInstruction.MainMessage.Text = ManualInstructions[ManualConnectionInstruction.InstructionIndex];
            }
        }
        private void OnNextButtonClick()
        {
            if (ManualConnectionInstruction.InstructionIndex != ManualInstructions.Count - 1) // if not at the last index 
            {
                ManualConnectionInstruction.InstructionIndex++;
                ManualConnectionInstruction.MainMessage.Text = ManualInstructions[ManualConnectionInstruction.InstructionIndex];
            }
            else // we have come to the end of the Instructions 
            {
                ManualConnectionInstruction.Close();
                ConnectionDialogClosed?.Invoke(this, TestRequested);
                TestRequested = null;
                
                
            }
        }
        private void OnExitButtonClick()
        {
            ManualConnectionInstruction.Close();
            ConnectionDialogClosed?.Invoke(this, TestRequested);
            TestRequested = null; 

            
        }

    }
}
