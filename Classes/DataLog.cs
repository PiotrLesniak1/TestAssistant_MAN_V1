using OfficeOpenXml;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace TestAssistant.Classes
{
    /*
     * File: DataLog.cs
     * Author: Piotr Lesniak (Raytheon)
     * Date: 16/10/23
     * Description: This file contains the implementation of the DataLog class, which is responsible for logging test data and outcomes to an Excel file.
     *              The class allows users to create and manage Excel logs for specific ReplyProccessor(RP) number and record test-related information, including user details, test points,
     *              test outcomes, and restart requests. It provides essential functionality for maintaining a detailed record of tests and their results.
     * Dependencies: This class relies on the EPPlus library for Excel file handling and the System.Windows namespace for displaying message boxes.
     * Usage: The DataLog class is utilized within the TestAssistant application to log and manage test data and outcomes. Users can add user details, record test points,
     *        update test outcomes and restart requests. This class helps maintain organized records of testing activities for each ReplyProccessor .
     */

    public class DataLog
    {
        private string ExcelFilePath; // File path where the Excel data will be logged.

        public string RP_NUM
        { get { return RPNumber; } } // RPNumber property to retrieve the RP number associated with this log.
        private string RPNumber; // RP number for this data log.
        private int NextRow; // Keeps track of the next row to write data to in the Excel file.

        public DataLog()
        {
            // Constructor: You can perform any additional initialization here if needed.
        }

        private void InitilizeFile(string rpNumber)
        {
            RPNumber = rpNumber; // Set the RP number for this data log.

            // Get the path to the MyDocuments directory.
            string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Create a "Test_Assistant" folder in MyDocuments if it doesn't exist.
            string testFolder = Path.Combine(appDataDirectory, "Test_Assistant");
            Directory.CreateDirectory(testFolder);

            // Create a "TestLogs" subfolder inside "Test_Assistant."
            string testLogsFolderPath = Path.Combine(testFolder, $"{rpNumber}_TestLogs");
            Directory.CreateDirectory(testLogsFolderPath);

            // Create a "TestOutcome" subfolder inside "Test_Assistant."
            string testOutcomeFolderPath = Path.Combine(testFolder, $"{rpNumber}_TestOutcome");
            Directory.CreateDirectory(testOutcomeFolderPath);

            // Set the Excel file path to save test outcomes.
            ExcelFilePath = Path.Combine(testOutcomeFolderPath, $"{rpNumber}.xlsx");

            // Set the ExcelPackage license context to non-commercial (assuming you are using EPPlus library).
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // You can perform further initialization related to Excel logging here.
        }

        public bool AddUserDetails(string rpNumber, string userID)
        {
            // Check if RP Number and User ID are empty.
            if (string.IsNullOrEmpty(rpNumber) || string.IsNullOrEmpty(userID))
            {
                MessageBox.Show("RP Number and User ID must not be empty.", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Initialize the Excel log file for the specified RP Number.
            InitilizeFile(rpNumber);

            try
            {
                // Open the Excel package for the log file.
                using (var package = new ExcelPackage(new FileInfo(ExcelFilePath)))
                {
                    ExcelWorksheet worksheet;

                    // Check if the worksheet "TESTData" already exists.
                    if (WorksheetExists(package, "TESTData"))
                    {
                        worksheet = package.Workbook.Worksheets["TESTData"];

                        // Find the next available row to add data.
                        NextRow = NextAvailableRow();

                        // Set the date in cell B3 to the current date and time.
                        if (worksheet.Cells["B3"] != null)
                        {
                            worksheet.Cells["B3"].Value = DateTime.Now.ToString();
                        }
                    }
                    else
                    {
                        // If the worksheet doesn't exist, create it and set headers.
                        worksheet = package.Workbook.Worksheets.Add("TESTData");

                        // Set headers
                        worksheet.Cells["A1"].Value = "RP Number";
                        worksheet.Cells["A2"].Value = "User ID";
                        worksheet.Cells["A3"].Value = "Date ";
                        worksheet.Cells["B1"].Value = RPNumber;
                        worksheet.Cells["B2"].Value = userID;
                        worksheet.Cells["B3"].Value = DateTime.Now.ToString();

                        worksheet.Cells["A4"].Value = "/////////////////////////////////////////////////////////////////////"; // SOME FORMATTING

                        worksheet.Cells["A5"].Value = "TESTS";
                        worksheet.Cells["B5"].Value = "Test Expected output ";
                        worksheet.Cells["C5"].Value = "Actual Output ";
                        worksheet.Cells["D5"].Value = "PASS / FAIL ";
                        worksheet.Cells["E5"].Value = " Test Outcome ";
                        worksheet.Cells["F5"].Value = "Attempt NO";

                        // Find the next available row to add data.
                        NextRow = 6;
                        worksheet.Cells[NextRow, 2].Value = rpNumber;
                        worksheet.Cells[NextRow, 3].Value = userID;
                        worksheet.Cells[NextRow, 4].Value = DateTime.Now.ToString();
                    }

                    // Save the Excel package.
                    package.Save();

                    return true;
                }
            }
            catch (IOException)
            {
                // Handle case where file already exists.
                MessageBox.Show($"A file with the name '{ExcelFilePath}' already exists.", "File Exists", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                // Handle other exceptions.
                MessageBox.Show($"An error occurred while processing the request.\n {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public void AddTestPointOutcome(string testType, string expectedOutput, string ActualOutput, string state, int attempts)
        {
            try
            {
                // Open the Excel package for the log file.
                using (var package = new ExcelPackage(new FileInfo(ExcelFilePath)))
                {
                    // Get the existing "TESTData" worksheet.
                    var worksheet = package.Workbook.Worksheets["TESTData"];

                    // Set values for the current test point.
                    worksheet.Cells[NextRow, 1].Value = testType;          // Test Type
                    worksheet.Cells[NextRow, 2].Value = expectedOutput;   // Expected Output
                    worksheet.Cells[NextRow, 3].Value = ActualOutput;     // Actual Output
                    worksheet.Cells[NextRow, 4].Value = state;            // PASS / FAIL / Other State

                    // Column 5 is taken by "Test Outcome" (can be changed in the future).
                    worksheet.Cells[NextRow, 6].Value = attempts + 1;     // Attempt Number (1-based index)

                    // Save the Excel package.
                    package.Save();

                    NextRow++; // Increment the row index for the next test point.
                }
            }
            catch (IOException)
            {
                // Handle case where file is open or inaccessible.
                MessageBox.Show($"Unable to write to '{ExcelFilePath}' because it is in use or inaccessible.", "File Access Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Handle other exceptions.
                MessageBox.Show($"An error occurred while processing the request.\n {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddTestOutcome(string testOutcome)
        {
            try
            {
                // Open the Excel package for the log file.
                using (var package = new ExcelPackage(new FileInfo(ExcelFilePath)))
                {
                    // Get the existing "TESTData" worksheet.
                    var worksheet = package.Workbook.Worksheets["TESTData"];

                    // Set the test outcome value in the Excel cell.
                    worksheet.Cells[NextRow, 5].Value = $"Test Outcome: {testOutcome}";

                    // Check if there are any empty cells in the current row and mark them with a placeholder value.
                    for (int column = 1; column <= worksheet.Dimension.Columns - 1; column++)
                    {
                        var cellValue = worksheet.Cells[NextRow, column].Text;
                        if (string.IsNullOrEmpty(cellValue))
                        {
                            worksheet.Cells[NextRow, column].Value = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
                            package.Save();
                        }
                    }

                    // Increment the row index for the next test point.
                    NextRow++;

                    // Save the Excel package.
                    package.Save();
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the process.
                MessageBox.Show($"An error occurred while processing the request.\n {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RestartRequested(string test)
        {
            try
            {
                // Open the Excel package for the log file.
                using (var package = new ExcelPackage(new FileInfo(ExcelFilePath)))
                {
                    // Get the existing "TESTData" worksheet.
                    var worksheet = package.Workbook.Worksheets["TESTData"];

                    // Set the test type and reason for restart in the Excel cells.
                    worksheet.Cells[NextRow, 1].Value = test;
                    worksheet.Cells[NextRow, 2].Value = "User Requested a restart";

                    // Save the Excel package.
                    package.Save();

                    // Increment the row index for the next test point.
                    NextRow++;
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the process.
                MessageBox.Show($"An error occurred while processing the request.\n {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void AbortRequest(string test)
        {
            try
            {
                // Open the Excel package for the log file.
                using (var package = new ExcelPackage(new FileInfo(ExcelFilePath)))
                {
                    // Get the existing "TESTData" worksheet.
                    var worksheet = package.Workbook.Worksheets["TESTData"];

                    // Set the test type and reason for restart in the Excel cells.
                    worksheet.Cells[NextRow, 1].Value = test;
                    worksheet.Cells[NextRow, 2].Value = "User Ended Test";

                    // Save the Excel package.
                    package.Save();

                    // Increment the row index for the next test point.
                    NextRow++;
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the process.
                MessageBox.Show($"An error occurred while processing the request.\n {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool WorksheetExists(ExcelPackage package, string worksheetName)
        {
            // Check if any worksheet in the package has the specified name.
            return package.Workbook.Worksheets.Any(worksheet => worksheet.Name == worksheetName);
        }

        private int NextAvailableRow()
        {
            using (var package = new ExcelPackage(new FileInfo(ExcelFilePath)))
            {
                var worksheet = package.Workbook.Worksheets["TESTData"]; // Get the existing "TESTData" worksheet

                bool isRowEmpty = true;
                int emptyColumns = 0;

                int MaxRows = worksheet.Dimension.End.Row + 1; // Retrieve the bottom right corner value of the rows

                for (int row = 1; row <= MaxRows + 1; row++)
                {
                    for (int column = 1; column <= worksheet.Dimension.Columns; column++)
                    {
                        var cellValue = worksheet.Cells[row, column].Text;

                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            isRowEmpty = false;
                            emptyColumns = 0;
                            break;
                        }
                        else
                        {
                            emptyColumns++;
                            if (emptyColumns == worksheet.Dimension.Columns)
                            {
                                isRowEmpty = true;
                            }
                        }
                    }

                    if (isRowEmpty)
                    {
                        return row;
                    }
                }

                // If no empty row is found, return -1 to indicate that there are no available rows.
                return -1;
            }
        }
    }
}