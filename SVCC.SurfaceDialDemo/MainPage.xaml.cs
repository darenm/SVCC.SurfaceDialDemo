using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using Windows.UI.Composition;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
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
        #region Fields

        private StorageFile _currentFile;
        private string _filterText = "Sample";
        private bool _isDirty;
        private bool _isFileOpen;
        private WriteableBitmap _mainImageBitmap;

        #endregion

        #region Properties

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
                if (_isFileOpen != value)
                {
                    _isFileOpen = value;
                    if (_isFileOpen)
                    {
                        AddDialMenuItems();
                    }

                    OnPropertyChanged();
                }
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

        public WriteableBitmap MainImageBitmap
        {
            get => _mainImageBitmap;
            set
            {
                _mainImageBitmap = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public MainPage()
        {
            InitializeComponent();
            ValueSliderPanel.Loaded += ValueSliderPanelOnLoaded;
            Loaded += (sender, args) => SetupSurfaceDial();
        }

        #region Surface Dial

        private RadialController _surfaceDial;
        private RadialControllerMenuItem _brightnessMenuItem;
        private RadialControllerMenuItem _contrastMenuItem;


        private void SetupSurfaceDial()
        {
            if (!RadialController.IsSupported())
            {
                return;
            }

            var config = RadialControllerConfiguration.GetForCurrentView();
            config.SetDefaultMenuItems(new [] { RadialControllerSystemMenuItemKind.Volume });

            _surfaceDial = RadialController.CreateForCurrentView();
        }

        private void AddDialMenuItems()
        {
            if (!RadialController.IsSupported())
            {
                return;
            }

            var brightnessIcon = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/Bright.png"));
            _brightnessMenuItem = RadialControllerMenuItem.CreateFromIcon("Brightness", brightnessIcon);
            _brightnessMenuItem.Invoked += BrightnessMenuItemOnInvoked;
            _surfaceDial.Menu.Items.Add(_brightnessMenuItem);

            var contrastIcon = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/Contrast.png"));
            _contrastMenuItem = RadialControllerMenuItem.CreateFromIcon("Contrast", contrastIcon);
            _contrastMenuItem.Invoked += ContrastMenuItemOnInvoked;
            _surfaceDial.Menu.Items.Add(_contrastMenuItem);
        }

        private void BrightnessMenuItemOnInvoked(RadialControllerMenuItem radialControllerMenuItem, object o)
        {
            ShowBrightnessFilter();
            SubscribeDialEvents();
        }

        private void ContrastMenuItemOnInvoked(RadialControllerMenuItem radialControllerMenuItem, object o)
        {
            ShowContrastFilter();
            SubscribeDialEvents();
        }

        private void SubscribeDialEvents()
        {
            _surfaceDial.RotationChanged += SurfaceDialOnRotationChanged;
            _surfaceDial.ButtonClicked += SurfaceDialOnButtonClicked;
        }

        private void UnsubscribeDialEvents()
        {
            _surfaceDial.RotationChanged -= SurfaceDialOnRotationChanged;
            _surfaceDial.ButtonClicked -= SurfaceDialOnButtonClicked;
        }

        private async void SurfaceDialOnButtonClicked(
            RadialController radialController, 
            RadialControllerButtonClickedEventArgs radialControllerButtonClickedEventArgs)
        {
            await ApplyFilter();
            UnsubscribeDialEvents();
        }

        private void SurfaceDialOnRotationChanged(
            RadialController radialController, 
            RadialControllerRotationChangedEventArgs radialControllerRotationChangedEventArgs)
        {
            if (radialControllerRotationChangedEventArgs.RotationDeltaInDegrees > 0)
            {
                if (ValueSlider.Value < ValueSlider.Maximum)
                {
                    ValueSlider.Value += ValueSlider.StepFrequency;
                }
            }
            else
            {
                if (ValueSlider.Value > ValueSlider.Minimum)
                {
                    ValueSlider.Value -= ValueSlider.StepFrequency;
                }
            }
        }

        #endregion

        #region Filter Operations

        private async void ApplyFilterClicked(object sender, RoutedEventArgs e)
        {
            await ApplyFilter();
        }

        private async Task ApplyFilter()
        {
            IsDirty = true;
            MainImageBitmap = await _effect.WriteToWriteableBitmapAsync(EffectCanvas, _loadedImage.Size);
            await LoadWin2DImageAsync();
            ResetFilter();
            ClosePanel();
        }

        private void CancelFilterClicked(object sender, RoutedEventArgs e)
        {
            ResetFilter();
            ClosePanel();
        }

        private void ResetFilter()
        {
            EffectCanvas.Visibility = Visibility.Collapsed;
        }

        private async void UndoClicked(object sender, RoutedEventArgs e)
        {
            await OpenImageFile(_currentFile);
            IsDirty = false;
        }

        #endregion

        #region Panel

        private bool _isPanelOpen;

        private void ValueSliderPanelOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            SetupComposition();
        }

        private void ClosePanel()
        {
            _isPanelOpen = false;
            ValueSlider.ValueChanged -= ContrastChanged;
            ValueSlider.ValueChanged -= BrightnessChanged;

            ValueSliderPanel.Visibility = Visibility.Collapsed;
            EffectCanvas.Visibility = Visibility.Collapsed;
        }

        private void ClosePanelClicked(object sender, RoutedEventArgs e)
        {
            ResetFilter();
            ClosePanel();
        }

        private void ShowPanel()
        {
            _isPanelOpen = true;
            ValueSlider.Value = 0;
            ValueSliderPanel.Visibility = Visibility.Visible;
            EffectCanvas.Visibility = Visibility.Visible;
        }

        #endregion

        #region Brightness

        private void BrightnessChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ((ExposureEffect) _effect).Exposure = (float) e.NewValue;
        }

        private void BrightnessClicked(object sender, RoutedEventArgs e)
        {
            ShowBrightnessFilter();
        }

        private void ShowBrightnessFilter()
        {
            FilterText = "Brightness";
            ValueSlider.ValueChanged -= ContrastChanged;
            _effect = EffectFactory.CreateExposureEffect(_loadedImage, ValueSlider);
            ValueSlider.ValueChanged += BrightnessChanged;
            ShowPanel();
        }

        #endregion

        #region Contrast

        private void ContrastChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ((ContrastEffect) _effect).Contrast = (float) e.NewValue;
        }

        private void ContrastClicked(object sender, RoutedEventArgs e)
        {
            ShowContrastFilter();
        }

        private void ShowContrastFilter()
        {
            FilterText = "Contrast";
            ValueSlider.ValueChanged -= BrightnessChanged;
            _effect = EffectFactory.CreateContrastEffect(_loadedImage, ValueSlider);
            ValueSlider.ValueChanged += ContrastChanged;
            ShowPanel();
        }

        #endregion

        #region IPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Composition

        private void SetupComposition()
        {
            ElementCompositionPreview.SetIsTranslationEnabled(ValueSliderPanel, true);
            ElementCompositionPreview.GetElementVisual(ValueSliderPanel);

            ElementCompositionPreview.SetImplicitShowAnimation(ValueSliderPanel, SetupAnimation(true));
            ElementCompositionPreview.SetImplicitHideAnimation(ValueSliderPanel, SetupAnimation(false));
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

        #endregion

        #region File IO

        private async void OpenFileClicked(object sender, RoutedEventArgs e)
        {
            if (_isPanelOpen)
            {
                ClosePanel();
            }

            if (IsDirty)
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
                MainImageBitmap = await BitmapFactory.FromStream(await file.OpenAsync(FileAccessMode.Read));
                ImageControl.Visibility = Visibility.Visible;
                await LoadWin2DImageAsync();
            }
            IsFileOpen = true;
            IsDirty = false;
        }

        private async Task LoadWin2DImageAsync()
        {
            _loadedImage = await MainImageBitmap.CreateCanvasBitmapAsync(EffectCanvas);
            _imageRatio = _loadedImage.Size.Height / _loadedImage.Size.Width;
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
                    await MainImageBitmap.ToStream(stream, encoderId);
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

        #endregion

        #region Canvas

        private CanvasBitmap _loadedImage;
        private ICanvasImage _effect;
        private double _imageRatio;

        private void Canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_effect == null || _loadedImage == null)
            {
                return;
            }

            var targetSize = sender.Size;
            var imageSize = _loadedImage.Size;

            var destinationRatio = targetSize.Height / targetSize.Width;
            var widthsRatio = imageSize.Width / targetSize.Width;
            var heightsRatio = imageSize.Height / targetSize.Height;
            var ratioToUse = _imageRatio < destinationRatio ? widthsRatio : heightsRatio;

            var destinationSize = new Size(imageSize.Width / ratioToUse, imageSize.Height / ratioToUse);

            var offset = new Point((targetSize.Width - destinationSize.Width) / 2,
                (targetSize.Height - destinationSize.Height) / 2);

            var ds = args.DrawingSession;
            ds.DrawImage(_effect, new Rect(offset, destinationSize), new Rect(offset, _loadedImage.Size));

            sender.Invalidate();
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (EffectCanvas.ReadyToDraw)
            {
                if (_effect is ExposureEffect)
                {
                    _effect = EffectFactory.CreateExposureEffect(_loadedImage, ValueSlider);
                }
                else
                {
                    _effect = EffectFactory.CreateContrastEffect(_loadedImage, ValueSlider);
                }
            }
        }

        #endregion
    }
}