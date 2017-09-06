using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace Template10.Services.Dialogs
{
    public static class ContentDialogExExtensions
    {
        private static readonly object ShowLockPoint = new object();

        public static async Task<IUICommand> SafeShowAsync(this MessageDialog mb)
        {
            using (var loc = await LockAsync.Create(ShowLockPoint))
            {
                return await mb.ShowAsync();
            }
        }

        public static async Task<ContentDialogResult> SafeShowAsync(this ContentDialog cd)
        {
            using (var loc = await LockAsync.Create(ShowLockPoint))
            {
                return await cd.ShowAsync();
            }
        }

        public static ContentDialog WithOkToClose(this ContentDialog cd)
        {
            cd.PrimaryButtonText = "Ok";
            cd.IsPrimaryButtonEnabled = true;
            return cd;
        }

        public static ContentDialog SetPrimaryButton(this ContentDialog cd, string text)
        {
            cd.PrimaryButtonText = text;
            cd.IsPrimaryButtonEnabled = true;
            return cd;
        }

        public static ContentDialog SetCloseButton(this ContentDialog cd, string text)
        {
            cd.CloseButtonText = text;
            return cd;
        }

        public static ContentDialog SetPrimaryButton(this ContentDialog cd, string text,
            TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickHandler)
        {
            cd.SetPrimaryButton(text);
            cd.PrimaryButtonClick += clickHandler;
            return cd;
        }

        public static ContentDialog SetSecondaryButton(this ContentDialog cd, string text)
        {
            cd.SecondaryButtonText = text;
            cd.IsSecondaryButtonEnabled = true;
            return cd;
        }

        public static ContentDialog SetSecondaryButton(this ContentDialog cd, string text,
            TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickHandler)
        {
            cd.SetSecondaryButton(text);
            cd.SecondaryButtonClick += clickHandler;
            return cd;
        }
    }
}
