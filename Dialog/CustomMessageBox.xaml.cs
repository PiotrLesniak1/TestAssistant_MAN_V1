using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TestAssistant.Dialog
{
    /// <summary>
    /// Interaction logic for MessageBoxPower.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        public event EventHandler<Button> ButtonClick;

        //Events 
        //DELEGATE 

        public delegate void ButtonPressed();

       


        public int InstructionIndex = 0;
        public  enum BUTTON
        {
            Ok,
            YesNo,
            YesNoCancel,
            ManualAutomatic,
            PreviousNextExit,
            Exit
        }
        public CustomMessageBox()
        {
            InitializeComponent();
            
        }
        private void LeftButton_Pressed(object sender, RoutedEventArgs e)
        {
            // Trigger the custom event when the button is clicked
            ButtonClick?.Invoke(this, LeftButton);
        }

        private void MiddleButton_Pressed(object sender, RoutedEventArgs e)
        {
            // Trigger the custom event when the button is clicked
            ButtonClick?.Invoke(this, MiddleButton);
        }

        private void RightButton_Pressed(object sender, RoutedEventArgs e)
        {
            // Trigger the custom event when the button is clicked
            ButtonClick?.Invoke(this, RightButton);
        }

        public void SetButtonConfiguration(BUTTON buttonConfig)
        {
            switch (buttonConfig)
            {
                case BUTTON.Ok:
                    LeftButton.Content = "OK";
                    RightButton.Visibility = Visibility.Collapsed;
                    MiddleButton.Visibility = Visibility.Collapsed;
                    break;

                case BUTTON.YesNo:
                    LeftButton.Content = "Yes";
                    MiddleButton.Content = "No";
                    RightButton.Visibility = Visibility.Collapsed;
                    break;

                case BUTTON.YesNoCancel:
                    LeftButton.Content = "Yes";
                    MiddleButton.Content = "No";
                    RightButton.Content = "Cancel";
                    break;

                case BUTTON.Exit:
                    LeftButton.Content = "Exit";
                    MiddleButton.Visibility = Visibility.Collapsed;
                    RightButton.Visibility = Visibility.Collapsed;
                    break;
                case BUTTON.ManualAutomatic:

                    LeftButton.Content = "Manual";
                    MiddleButton.Visibility = Visibility.Collapsed;
                    RightButton.Content = "Auto";
                    break;
                case BUTTON.PreviousNextExit:

                    LeftButton.Content   = "Previous";
                    MiddleButton.Content = "Next";
                    RightButton.Content  = "Exit";
                    break;

                default:
                    // Default to OK button configuration
                    LeftButton.Content = "OK";
                    RightButton.Visibility = Visibility.Collapsed;
                    MiddleButton.Visibility = Visibility.Collapsed;
                    break;
            }
        }
        public void ResetIndex()
        {
            InstructionIndex = 0;
        }


    }
}
