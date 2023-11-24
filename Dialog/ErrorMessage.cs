using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static TestAssistant.Dialog.CustomMessageBox;

namespace TestAssistant.Dialog
{
    class ErrorMessage
    {
        private CustomMessageBox CustomMessage;
        public void DisplayError(string title, string message)
        {
            CustomMessage = new CustomMessageBox();
            CustomMessage.ButtonClick += Button_Click;

            CustomMessage.Title = $"             {title}             ";
            CustomMessage.MainMessage.Text = message;


            CustomMessage.SetButtonConfiguration(BUTTON.Ok);

            CustomMessage.ShowDialog();

            // Return the result based on the button clicked
        }
        private void Button_Click(object sender, Button buttonClicked)
        {
            CustomMessage.Close();
        }
    }
}
