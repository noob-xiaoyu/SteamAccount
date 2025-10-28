using SteamAccountManager.ViewModels;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace SteamAccountManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string uri = e.Uri.AbsoluteUri;
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.DataContext is MainViewModel viewModel)
            {
                viewModel.SaveAllOnExit();
            }
        }

        private void ApiKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string pattern = "[A-F0-9]{32}";
                var match = Regex.Match(textBox.Text, pattern, RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    string extractedKey = match.Value.ToUpper();
                    if (textBox.Text != extractedKey)
                    {
                        textBox.Text = extractedKey;
                        textBox.CaretIndex = extractedKey.Length;
                    }
                }
            }
        }
    }
}