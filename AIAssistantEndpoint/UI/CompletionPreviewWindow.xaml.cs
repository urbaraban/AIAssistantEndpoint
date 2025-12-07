using System.Windows;

namespace AIAssistantEndpoint.UI
{
    public partial class CompletionPreviewWindow : Window
    {
        public string FinalText { get; private set; }

        public CompletionPreviewWindow()
        {
            InitializeComponent();
        }

        public void UpdatePreview(string text)
        {
            Dispatcher.Invoke(() =>
            {
                PreviewTextBox.Text = text;
            });
        }

        public void EnableAccept()
        {
            Dispatcher.Invoke(() =>
            {
                AcceptButton.IsEnabled = true;
            });
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            FinalText = PreviewTextBox.Text;
            this.DialogResult = true;
            this.Close();
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
