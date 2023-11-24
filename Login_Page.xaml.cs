using System;
using System.Text.RegularExpressions;
using System.Windows;
using TestAssistant.AppConfiguration;
using TestAssistant.Classes;

namespace TestAssistant
{
    // Interaction logic for Window1.xaml. This window is responsible for user login.
    public partial class Login_Page : Window
    {
        // DataLog instance to log user details.
        public static DataLog CDataLog = new DataLog();
        
        // Initializes a new instance of the Window1 class.
        public Login_Page()
        {
            InitializeComponent();
            
        }

        // Handles the Click event of the Login_Button control.
        // Attempts to log in the user, and if successful, opens the main application window.
        private void Login_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation check before attempting to save users details 
                if (ContainsOnlyNumbers(RPNum.Text) && ContainsOnlyNumbers(ID.Text))
                {
                    if (LogUserDetails())
                    {
                        // After a successful login:
                        MainWindow mainWindow = new MainWindow(); // Create an instance of the new window
                        mainWindow.Show(); // Show the new window

                        // Close the current login window
                        this.Close();
                    }
                    else
                    {
                        // Display an error message if login fails
                        MessageBox.Show("Login failed. Please check your credentials.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Login failed. No Letters should be present.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }

        // Checks if a string only contains numbers 
        // Returns true if only numbers are in the string otherwise if a letter is present it will return false .
        public static bool ContainsOnlyNumbers(string input)
        {
            // Define a regular expression pattern that matches only numbers (0-9)
            string pattern = @"^[0-9]+$";

            // Use Regex.IsMatch to check if the input string matches the pattern
            return Regex.IsMatch(input, pattern);
        }
        // Logs user details into the data log.
        // Returns true if user details are successfully logged; otherwise, false.
        private bool LogUserDetails()
        {
            // Add user details to the data log
            return CDataLog.AddUserDetails(RPNum.Text, ID.Text);
        }
    }
}