using System;
using System.Windows;
using System.Windows.Controls;
using static TestAssistant.Dialog.CustomMessageBox;

namespace TestAssistant.Dialog
{
    class DialogManager
    {



        private PowerOption BoardPower = new PowerOption();
        public ManualConnection ConnectionDialog = new ManualConnection();
        private ErrorMessage errorDialog = new ErrorMessage();
        private GenericMessage Message = new GenericMessage();

        public event EventHandler<string> DisplayErrorUI;




        public DialogManager()
        {
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            
        }

        public void DiplayErrorUI(string errorOrMessage)
        {
            DisplayErrorUI?.Invoke(this, errorOrMessage);
        }


        public void DisplayPowerOptions()
        {
            BoardPower.ShowPowerDialog();
        }
        public void DisplayManualConnection(string test)
        {
            
            ConnectionDialog.ShowConnectionDialog(test);
        }
        public void DisplayErrorMessage(string error)
        {

            errorDialog.DisplayError("ERROR", error);
        }
        public void DisplayMessage( string message ,string title = null)
        {
            
            Message.ShowDialog(title,message);
        }


    }
}
