using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static TestAssistant.Dialog.CustomMessageBox;

namespace TestAssistant.Dialog
{
    class GenericMessage
    {
        private CustomMessageBox CustomMessage;
        public void ShowDialog(string title = null, string message = "", BUTTON buttonConfig = BUTTON.Ok)
        {
            CustomMessage = new CustomMessageBox();
            CustomMessage.ButtonClick += Button_Click;

            CustomMessage.Title = $"             {title}             ";
            CustomMessage.MainMessage.Text = message;


            CustomMessage.SetButtonConfiguration(buttonConfig);

            CustomMessage.ShowDialog();

            // Return the result based on the button clicked

        }
        private void Button_Click(object sender, Button buttonClicked)
        {
            CustomMessage.Close();
        }
    }
}
