using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Template10.Services.Dialogs;

namespace SVCC.SurfaceDialDemo
{
    /// <inheritdoc />
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private StorageFile _currentFile;
        private WriteableBitmap _filteredBitmap;
        private string _filterText = "Sample";
        private bool _isDirty;
        private bool _isFileOpen;
        private WriteableBitmap _writeableBitmap;

        public MainPage()
        {
            InitializeComponent();
            ValueSliderPanel.Loaded += ValueSliderPanelOnLoaded;
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

        public bool IsFileOpen
        {
            get => _isFileOpen;
            set
            {
                _isFileOpen = value;
                OnPropertyChanged();
            }
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
            }
        }

        public WriteableBitmap FilteredBitmap
        {
            get => _filteredBitmap;
            set
            {
                _filteredBitmap = value;
                OnPropertyChanged();
            }
        }

        public WriteableBitmap WriteableBitmap
        {
            get => _writeableBitmap;
            set
            {
                _writeableBitmap = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void ApplyFilterClicked(object sender, RoutedEventArgs e)
        {
            await TransferFromCanvasToWriteableBitmapAsync();
            IsDirty = true;
            WriteableBitmap = FilteredBitmap;
            ResetFilter();
            ClosePane();
        }

        private void BrightnessChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ((ExposureEffect) _effect).Exposure = (float) e.NewValue;
        }

        private async void BrightnessClicked(object sender, RoutedEventArgs e)
        {
            if (ContrastToggle.IsChecked.GetValueOrDefault())
            {
                ContrastToggle.IsChecked = false;
            }
            FilterText = "Brightness";
            await LoadWritableBitmapToCanvasBitmap();
            _effect = CreateExposureEffect();

            HandlePanelVisibility();

            ValueSlider.ValueChanged += BrightnessChanged;
        }

        private async Task LoadWritableBitmapToCanvasBitmap()
        {
            using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
            {
                await WriteableBitmap.ToStream(stream, BitmapEncoder.PngEncoderId);

                using (var inputStream = stream.CloneStream())
                {
                    FilteredBitmap = await BitmapFactory.FromStream(inputStream);
                    using (var clone = inputStream.CloneStream())
                    {
                        _loadedImage = await CanvasBitmap.LoadAsync(EffectCanvas, clone);
                        _ratio = _loadedImage.Size.Height / _loadedImage.Size.Width;
                    }
                }
            }
        }

        private void CancelFilterClicked(object sender, RoutedEventArgs e)
        {
            ResetFilter();
            ClosePane();
        }

        private void ClosePane()
        {
            BrightnessToggle.IsChecked = false;
            ContrastToggle.IsChecked = false;
            HandlePanelVisibility();
        }

        private void ClosePaneClicked(object sender, RoutedEventArgs e)
        {
            ResetFilter();
            ClosePane();
        }

        private void ContrastChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ((ContrastEffect) _effect).Contrast = (float) e.NewValue;
        }

        private async void ContrastClicked(object sender, RoutedEventArgs e)
        {
            if (BrightnessToggle.IsChecked.GetValueOrDefault())
            {
                BrightnessToggle.IsChecked = false;
            }
            FilterText = "Contrast";
            await LoadWritableBitmapToCanvasBitmap();
            _effect = CreateContrastEffect();

            HandlePanelVisibility();
            ValueSlider.ValueChanged += ContrastChanged;
        }

        private void HandlePanelVisibility()
        {
            ValueSlider.ValueChanged -= ContrastChanged;
            ValueSlider.ValueChanged -= BrightnessChanged;

            if (ContrastToggle.IsChecked.HasValue && ContrastToggle.IsChecked.Value ||
                BrightnessToggle.IsChecked.HasValue && BrightnessToggle.IsChecked.Value)
            {
                ValueSlider.Value = 0;
                ValueSliderPanel.Visibility = Visibility.Visible;
                EffectCanvas.Visibility = Visibility.Visible;
            }
            else
            {
                ValueSliderPanel.Visibility = Visibility.Collapsed;
                EffectCanvas.Visibility = Visibility.Collapsed;
            }
        }

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
            await OpenImageFile(file);
        }

        private async Task OpenImageFile(StorageFile file)
        {
            if (file != null)
            {
                _currentFile = file;
                WriteableBitmap = await BitmapFactory.FromStream(await file.OpenAsync(FileAccessMode.Read));
            }
            IsFileOpen = true;
        }

        private void ResetFilter()
        {
            EffectCanvas.Visibility = Visibility.Collapsed;
            FilteredBitmap = null;
        }

        private async void SaveAsClicked(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker {SuggestedStartLocation = PickerLocationId.PicturesLibrary};
            savePicker.FileTypeChoices.Add("JPEG", new List<string> {".jpg", ".jpeg"});
            savePicker.FileTypeChoices.Add("PNG", new List<string> {".png"});
            savePicker.SuggestedFileName = $"{_currentFile.DisplayName}-modified";

            string dialogText;
            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until
                // we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file

                Guid encoderId;
                switch (file.FileType)
                {
                    case ".jpg":
                    case ".jpeg":
                        encoderId = BitmapEncoder.JpegEncoderId;
                        break;

                    default:
                        encoderId = BitmapEncoder.PngEncoderId;
                        break;
                }

                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await WriteableBitmap.ToStream(stream, encoderId);
                }

                var status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == FileUpdateStatus.Complete)
                {
                    dialogText = "File " + file.Name + " was saved.";
                    _currentFile = file;
                    IsDirty = false;
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

        private static CompositionAnimationGroup SetupAnimation(bool isShow)
        {
            float horizontalOffsetFrom, horizontalOffsetTo;
            if (isShow)
            {
                horizontalOffsetFrom = -320;
                horizontalOffsetTo = 0;
            }
            else
            {
                horizontalOffsetFrom = 0;
                horizontalOffsetTo = -320;
            }

            var sliderPanelTranslationAnimation = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
            if (isShow)
            {
                sliderPanelTranslationAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
                sliderPanelTranslationAnimation.DelayTime = TimeSpan.FromSeconds(0.2);
            }
            sliderPanelTranslationAnimation.Duration = TimeSpan.FromSeconds(0.5);
            sliderPanelTranslationAnimation.Target = "Translation.X";
            sliderPanelTranslationAnimation.InsertKeyFrame(0, horizontalOffsetFrom);
            sliderPanelTranslationAnimation.InsertKeyFrame(1, horizontalOffsetTo);

            var sliderPanelShowAnimations = Window.Current.Compositor.CreateAnimationGroup();
            sliderPanelShowAnimations.Add(sliderPanelTranslationAnimation);
            return sliderPanelShowAnimations;
        }

        private async void UndoClicked(object sender, RoutedEventArgs e)
        {
            await OpenImageFile(_currentFile);
            IsDirty = false;
        }

        private void ValueSliderPanelOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            ElementCompositionPreview.SetIsTranslationEnabled(ValueSliderPanel, true);
            ElementCompositionPreview.GetElementVisual(ValueSliderPanel);

            ElementCompositionPreview.SetImplicitShowAnimation(ValueSliderPanel, SetupAnimation(true));
            ElementCompositionPreview.SetImplicitHideAnimation(ValueSliderPanel, SetupAnimation(false));
        }

        #region Canvas

        private CanvasBitmap _loadedImage;
        private ICanvasImage _effect;
        private double _ratio;

        private void Canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_effect == null || _loadedImage == null)
            {
                return;
            }

            var size = sender.Size;
            var ds = args.DrawingSession;

            Size destinationSize;
            if (size.Height > size.Width)
            {
                //portrait
                destinationSize = new Size(size.Width, size.Width / _ratio);
            }
            else
            {
                destinationSize = new Size(size.Height / _ratio, size.Height);
            }

            var offset = new Point((size.Width - destinationSize.Width) / 2,
                (size.Height - destinationSize.Height) / 2);

            ds.DrawImage(_effect, new Rect(offset, destinationSize), new Rect(offset, _loadedImage.Size));

            sender.Invalidate();
        }

        private void Canvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(Canvas_CreateResourcesAsync(sender).AsAsyncAction());
        }

        async Task Canvas_CreateResourcesAsync(CanvasControl sender)
        {
            //_loadedImage = await CanvasBitmap.LoadAsync(sender, "HeadShot.jpeg");
            //_ratio = _loadedImage.Size.Height / _loadedImage.Size.Width;
        }


        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (EffectCanvas.ReadyToDraw)
            {
                if (_effect is ExposureEffect)
                {
                    _effect = CreateExposureEffect();
                }
                else
                {
                    _effect = CreateContrastEffect();
                }
            }
        }

        private ICanvasImage CreateExposureEffect()
        {
            ValueSlider.Minimum = -2;
            ValueSlider.Maximum = 2;
            ValueSlider.StepFrequency = 0.2;


            var brightnessEffect = new ExposureEffect
            {
                Source = _loadedImage,
                Exposure = 0
            };

            return brightnessEffect;
        }

        private ICanvasImage CreateContrastEffect()
        {
            ValueSlider.Minimum = -1;
            ValueSlider.Maximum = 1;
            ValueSlider.StepFrequency = 0.1;

            var brightnessEffect = new ContrastEffect
            {
                Source = _loadedImage,
                Contrast = 0
            };

            return brightnessEffect;
        }

        #endregion

        #region InMemory Stream for transferring from Canvas to WriteableBitmap

        private async Task TransferFromCanvasToWriteableBitmapAsync()
        {
            // Initialize the in-memory stream where data will be stored.
            using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
            {
                await CanvasImage.SaveAsync(_effect, new Rect(new Point(0, 0), _loadedImage.Size), 96, EffectCanvas, stream,
                    CanvasBitmapFileFormat.Png);

                using (var inputStream = stream.CloneStream())
                {
                    FilteredBitmap = await BitmapFactory.FromStream(inputStream);
                }
            }
        }

        #endregion
    }
}
