using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Template10.Services.Dialogs
{
    public sealed class ContentDialogEx : ContentDialog
    {

        public static readonly DependencyProperty ProgrammaticDismissOnlyProperty = DependencyProperty.Register(
            "ProgrammaticDismissOnly", typeof(bool), typeof(ContentDialogEx), new PropertyMetadata(default(bool)));

        private bool _programmaticCloseRequested;

        public bool ProgrammaticDismissOnly
        {
            get => (bool)GetValue(ProgrammaticDismissOnlyProperty);
            set => SetValue(ProgrammaticDismissOnlyProperty, value);
        }

        public ContentDialogEx()
        {
            this.DefaultStyleKey = typeof(ContentDialogEx);

            // for testing - secondary button click equates to programmatic hide request
            this.SecondaryButtonClick += (sender, args) => _programmaticCloseRequested = true;

            this.Closing += (sender, args) =>
            {
                if (ProgrammaticDismissOnly && !_programmaticCloseRequested)
                {
                    args.Cancel = true;
                }
            };
        }

        public new void Hide()
        {
            _programmaticCloseRequested = true;
            base.Hide();
        }
    }
}
