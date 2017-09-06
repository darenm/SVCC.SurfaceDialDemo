using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Template10.Services.Dialogs;

namespace SVCC.SurfaceDialDemo
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private string _currentFilename;
        private bool _isDirty;
        private WriteableBitmap _writeableBitmap;

        public MainPage()
        {
            InitializeComponent();
        }

        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                _isDirty = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void OpenFileClicked(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var result = await new MessageBox("There are unsaved changes - discard and proceed?").WithYesNo()
                    .SafeShowAsync();
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _currentFilename = file.Name;
                _writeableBitmap = await BitmapFactory.FromStream(await file.OpenAsync(FileAccessMode.Read));
                ImageControl.Source = _writeableBitmap;
            }
        }

        private async void SaveAsClicked(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker {SuggestedStartLocation = PickerLocationId.PicturesLibrary};
            savePicker.FileTypeChoices.Add("JPEG", new List<string> {".jpg", ".jpeg"});
            savePicker.FileTypeChoices.Add("PNG", new List<string> {".png"});
            savePicker.SuggestedFileName = $"{_currentFilename}-modified";

            string dialogText;
            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until
                // we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file

                switch (file.FileType)
                {
                    case ".jpg":
                    case ".jpeg":
                }
                _writeableBitmap.ToStream()
                await FileIO.WriteTextAsync(file, file.Name);
                // Let Windows know that we're finished changing the file so
                // the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                var status =
                    await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == FileUpdateStatus.Complete)
                {
                    dialogText = "File " + file.Name + " was saved.";
                }
                else
                {
                    dialogText = "File " + file.Name + " couldn't be saved.";
                }
            }
            else
            {
                dialogText = "Operation cancelled.";
            }

            await new MessageBox(dialogText).WithOk().SafeShowAsync();
        }
    }
}