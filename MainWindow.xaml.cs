using System.IO;
using System.Text;
using System.Windows;

namespace FileEncryption
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string filePath = string.Empty;
        byte[] key;
        byte[] iv;

        public MainWindow()
        {
            InitializeComponent();
            key = Encoding.UTF8.GetBytes(EncryptionHelper.AES_key);
            iv = Encoding.UTF8.GetBytes(EncryptionHelper.AES_iv);
            filePathLBL.SetBinding(System.Windows.Controls.Label.ContentProperty, new System.Windows.Data.Binding("filePath"));
        }
       
        private void filePicker_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Title = "Select a file";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
                filePathLBL.Content = filePath;
            }
        }

        private void EncryptFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                System.Windows.Forms.MessageBox.Show("Please select a file first.");
                return;
            }
            var output = filePath + ".enc";
            EncryptionHelper.EncryptFile(filePath, output, key, iv);
            filePath = output;
            EncryptFileLBL.Content = "File encrypted successfully.";
        }

        private async void DecryptFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                System.Windows.Forms.MessageBox.Show("Please select a file first.");
                return;
            }
            var result = await EncryptionHelper.DecryptFile(filePath, key, iv);
            var output = filePath.Replace(".enc", string.Empty);
            using (var fileStream = new System.IO.FileStream(output, System.IO.FileMode.Create))
            {
                await result.CopyToAsync(fileStream);
            }
            File.Delete(filePath);
            filePath = string.Empty;
            DecryptFileLBL.Content = "File decrypted successfully.";
        }

        private async void SaveFileZipButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                System.Windows.Forms.MessageBox.Show("Please select a file first.");
                return;
            }
            filePath = await EncryptionHelper.SaveFileZip(EncryptionHelper.ConvertFilePathToIFormFile(filePath), filePath);
        }
        private async void UnZipFileFromZipButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                System.Windows.Forms.MessageBox.Show("Please select a file first.");
                return;
            }
            var result = await EncryptionHelper.UnZipFileFromZip(filePath);
            var savePath = Path.Combine(Environment.CurrentDirectory, result.name);
            await File.WriteAllBytesAsync(savePath, result.file);
        }

        private void EncryptStringButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(EncryptStringtxtBox.Text))
            {
                System.Windows.Forms.MessageBox.Show("Please enter a string first.");
                return;
            }
            var encryptedString = EncryptionHelper.EncryptString(EncryptStringtxtBox.Text, key, iv);
            EncryptStringtxtBox.Text = string.Empty;
            DecryptStringtxtBox.Text = encryptedString;
        }

        private void DecryptStringButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DecryptStringtxtBox.Text))
            {
                System.Windows.Forms.MessageBox.Show("Please enter a string first.");
                return;
            }
            var decryptedString = EncryptionHelper.DecryptString(DecryptStringtxtBox.Text, key, iv);
            DecryptStringtxtBox.Text = string.Empty;
            EncryptStringtxtBox.Text = decryptedString;
        }
    }
}