﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Composition;
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
        }

        private async void ApplyFilterClicked(object sender, RoutedEventArgs e)
        {
            IsDirty = true;
            MainImageBitmap = await _effect.WriteToWriteableBitmapAsync(EffectCanvas, _loadedImage.Size);
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

        #region Panel

        private void ValueSliderPanelOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            SetupComposition();
        }

        private void ClosePanel()
        {
            BrightnessToggle.IsChecked = false;
            ContrastToggle.IsChecked = false;
            HandlePanelVisibility();
        }

        private void ClosePanelClicked(object sender, RoutedEventArgs e)
        {
            ResetFilter();
            ClosePanel();
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

        #endregion

        #region Brightness

        private void BrightnessChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ((ExposureEffect) _effect).Exposure = (float) e.NewValue;
        }

        private void BrightnessClicked(object sender, RoutedEventArgs e)
        {
            if (ContrastToggle.IsChecked.GetValueOrDefault())
            {
                ContrastToggle.IsChecked = false;
            }
            FilterText = "Brightness";
            _effect = EffectFactory.CreateExposureEffect(_loadedImage, ValueSlider);

            HandlePanelVisibility();

            ValueSlider.ValueChanged += BrightnessChanged;
        }

        #endregion

        #region Contrast

        private void ContrastChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ((ContrastEffect) _effect).Contrast = (float) e.NewValue;
        }

        private void ContrastClicked(object sender, RoutedEventArgs e)
        {
            if (BrightnessToggle.IsChecked.GetValueOrDefault())
            {
                BrightnessToggle.IsChecked = false;
            }
            FilterText = "Contrast";
            _effect = EffectFactory.CreateContrastEffect(_loadedImage, ValueSlider);

            HandlePanelVisibility();
            ValueSlider.ValueChanged += ContrastChanged;
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
                MainImageBitmap = await BitmapFactory.FromStream(await file.OpenAsync(FileAccessMode.Read));
                ImageControl.Visibility = Visibility.Visible;
                await LoadWin2DImageAsync();
            }
            IsFileOpen = true;
        }

        private async Task LoadWin2DImageAsync()
        {
            _loadedImage = await MainImageBitmap.CreateCanvasBitmapAsync(EffectCanvas);
            _ratio = _loadedImage.Size.Height / _loadedImage.Size.Width;
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