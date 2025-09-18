using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using Ude;
using Windows.Storage.Pickers;




namespace CppEncoder.Presentation;


public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private Mutex mutex = new Mutex();

    private async void OnSelectFilesClick(object sender, RoutedEventArgs e)
    {
        if (false == mutex.WaitOne(0))
        {
            // deny the upcoming requests if the previously selected files are in conversion.
            ContentDialog infoBox = new()
            {
                Title = "Information",
                Content = "Please wait until the previous request get finished.",
                CloseButtonText = "Okay",
                XamlRoot = this.XamlRoot
            };

            await infoBox.ShowAsync();
            return;
        }


        try
        {
            FolderPicker picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                ConcurrentBag<(string filePath, Encoding encoding)> files = new System.Collections.Concurrent.ConcurrentBag<(string filePath, Encoding encoding)>();
                byte[] UTF8_BOM = [0xEF, 0xBB, 0xBF];
                byte[] BOM_ValidationBuffer = new byte[3];

                Parallel.ForEach(System.IO.Directory.GetFiles(folder.Path, searchPattern: "*.*", System.IO.SearchOption.AllDirectories), filePath =>
                {
                    if ((filePath.EndsWith(".h", StringComparison.OrdinalIgnoreCase) == true) ||
                        (filePath.EndsWith(".hpp", StringComparison.OrdinalIgnoreCase) == true) ||
                        (filePath.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase) == true) ||
                        (filePath.EndsWith(".hxx", StringComparison.OrdinalIgnoreCase) == true) ||
                        (filePath.EndsWith(".cxx", StringComparison.OrdinalIgnoreCase) == true) ||
                        (filePath.EndsWith(".hh", StringComparison.OrdinalIgnoreCase) == true) ||
                        (filePath.EndsWith(".cc", StringComparison.OrdinalIgnoreCase) == true) ||
                        (filePath.EndsWith(".tpp", StringComparison.OrdinalIgnoreCase) == true) ||
                        (filePath.EndsWith(".inl", StringComparison.OrdinalIgnoreCase) == true))
                    {
                        using (FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read))
                        {
                            if (fileStream.Length <= 3)
                            {
                                return;
                            }

                            fileStream.ReadExactly(BOM_ValidationBuffer, 0, 3);

                            if (false == BOM_ValidationBuffer.SequenceEqual(UTF8_BOM))
                            {
                                CharsetDetector detector = new();
                                detector.Feed(fileStream);
                                detector.DataEnd();

                                if (detector.Charset == null)
                                {
                                    return;
                                }
                                files.Add( (filePath, Encoding.GetEncoding(detector.Charset)) );
                            }
                        }
                    }
                });

                FileListView.ItemsSource = files;
                StatusText.Text = $"{files.Count} files selected for encoding.";
            }

            mutex.ReleaseMutex(); // unlock it!
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Error selecting files: {exception.Message}";
            mutex.ReleaseMutex(); // unlock it!
        }
    }

    private async void OnEncodeClick(object sender, RoutedEventArgs e)
    {
        if (false == mutex.WaitOne(0))
        {
            // deny the upcoming requests if the previously selected files are in conversion.
            ContentDialog infoBox = new ContentDialog
            {
                Title = "Information",
                Content = "Please wait until the previous request get finished.",
                CloseButtonText = "Okay",
                XamlRoot = this.XamlRoot
            };

            await infoBox.ShowAsync();
            return;
        }


        if(true == FileListView.Items.Empty())
        {
            mutex.ReleaseMutex(); // unlock it!
            StatusText.Text = "No files selected for encoding!";
            return;
        }


        ContentDialog dialog = new ContentDialog
        {
            Title = "Warning",
            Content = "The Frogman Engine Coding Style Guide WARNS to write code and comments in English; not doing so might corrupt the file contents." +
                      "\nBy clicking the \"Okay\" button, you agree that you are aware of the risks and the affects that might alter and malform the contents within the files you select.",
            PrimaryButtonText = "Cancel",
            CloseButtonText = "Okay",
            XamlRoot = this.XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (ContentDialogResult.Primary == result)
        {
            StatusText.Text = "Encoding Canceled!";
            return;
        }


        try
        {
            Parallel.ForEach(FileListView.Items, file =>
            {
                var (filePath, encoding) = (ValueTuple<string, Encoding>)file;

                string content;
                using (StreamReader reader = new(filePath, encoding, detectEncodingFromByteOrderMarks: true))
                {
                    content = reader.ReadToEnd();
                }

                using (StreamWriter writer = new(filePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
                {
                    writer.Write(content);
                }
            });

            StatusText.Text = "Encoding Complete!";
            mutex.ReleaseMutex(); // unlock it!
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Error encoding files: {exception.Message}";
            mutex.ReleaseMutex(); // unlock it!
            return;
        }
    }
}
