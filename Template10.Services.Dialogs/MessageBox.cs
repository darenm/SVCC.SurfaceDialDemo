using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Template10.Services.Dialogs
{
    /// <summary>
    /// This class encapsulates the behavior for a simple message box.
    /// </summary>
    /// <remarks>
    /// This class uses a <see cref="ContentDialogEx"/> behind the scenes.
    /// </remarks>
    public class MessageBox
    {
        private readonly ContentDialogEx _contentDialogEx;
        private MessageBoxType _messageBoxType;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageBox" /> class.
        /// </summary>
        /// <param name="text">The text.</param>
        public MessageBox(string text)
        {
            _contentDialogEx = new ContentDialogEx
            {
                Content = text,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Colors.White)
            };

            Text = text;

            // default as an OK 
            WithOk();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageBox" /> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="messageBoxType">Type of the message box.</param>
        /// <exception cref="ArgumentOutOfRangeException">messageBoxType - null</exception>
        public MessageBox(string text, MessageBoxType messageBoxType) : this(text)
        {
            switch (messageBoxType)
            {
                case MessageBoxType.Ok:
                    WithOk();
                    break;
                case MessageBoxType.OkCancel:
                    WithOkCancel();
                    break;
                case MessageBoxType.YesNo:
                    WithYesNo();
                    break;
                case MessageBoxType.YesNoCancel:
                    WithYesNoCancel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageBoxType), messageBoxType, null);
            }
        }

        public string Text { get; set; }

        /// <summary>
        ///     Configures the <see cref="MessageBox" /> to display an Ok button.
        /// </summary>
        /// <returns>
        ///     <see cref="MessageBox" />
        /// </returns>
        public MessageBox WithOk()
        {
            _messageBoxType = MessageBoxType.Ok;
            SetPrimaryButton(ResolveResource("Ok"));
            return this;
        }

        /// <summary>
        ///     Configures the <see cref="MessageBox" /> to display Ok and Cancel buttons.
        /// </summary>
        /// <returns>
        ///     <see cref="MessageBox" />
        /// </returns>
        public MessageBox WithOkCancel()
        {
            WithOk();
            SetSecondaryButton(ResolveResource("Cancel"));
            _messageBoxType = MessageBoxType.OkCancel;
            return this;
        }

        /// <summary>
        ///     Configures the <see cref="MessageBox" /> to display Yes and No buttons.
        /// </summary>
        /// <returns>
        ///     <see cref="MessageBox" />
        /// </returns>
        public MessageBox WithYesNo()
        {
            SetPrimaryButton(ResolveResource("Yes"));
            SetSecondaryButton(ResolveResource("No"));
            _messageBoxType = MessageBoxType.YesNo;
            return this;
        }

        /// <summary>
        ///     Configures the <see cref="MessageBox" /> to display Yes, No and Cancel buttons.
        /// </summary>
        /// <returns>
        ///     <see cref="MessageBox" />
        /// </returns>
        public MessageBox WithYesNoCancel()
        {
            WithYesNo();
            SetCloseButton(ResolveResource("Cancel"));
            _messageBoxType = MessageBoxType.YesNoCancel;
            return this;
        }

        /// <summary>
        ///     Sets the primary button.
        /// </summary>
        /// <param name="displayText">The display text.</param>
        private void SetPrimaryButton(string displayText)
        {
            _contentDialogEx.PrimaryButtonText = displayText;
            _contentDialogEx.IsPrimaryButtonEnabled = true;
        }

        /// <summary>
        ///     Sets the secondary button.
        /// </summary>
        /// <param name="displayText">The display text.</param>
        private void SetSecondaryButton(string displayText)
        {
            _contentDialogEx.SecondaryButtonText = displayText;
            _contentDialogEx.IsSecondaryButtonEnabled = true;
        }

        /// <summary>
        ///     Sets the close button.
        /// </summary>
        /// <param name="displayText">The display text.</param>
        private void SetCloseButton(string displayText)
        {
            // Appears setting the text automatically enables the Close button
            _contentDialogEx.CloseButtonText = displayText;
        }


        /// <summary>
        ///     Resolves the resource.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns>The string value of the resource.</returns>
        private string ResolveResource(string resourceName)
        {
            // TODO: Resolve the resource
            return resourceName;
        }

        /// <summary>
        ///     Shows the <see cref="MessageBox" /> using an Async operation. The method is "safe" as it ensures
        ///     that any currently displayed MessageBox is closed before this MessageBox opens.
        /// </summary>
        /// <returns>Task&lt;MessageBoxResult&gt;.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if the dialog return type is unknown
        /// </exception>
        public async Task<MessageBoxResult> SafeShowAsync()
        {
            var result = await _contentDialogEx.SafeShowAsync();
            switch (result)
            {
                case ContentDialogResult.None:
                    // assuming this is Cancel
                    return MessageBoxResult.Cancel;

                case ContentDialogResult.Primary:
                    switch (_messageBoxType)
                    {
                        case MessageBoxType.Ok:
                        case MessageBoxType.OkCancel:
                            return MessageBoxResult.Ok;
                        case MessageBoxType.YesNo:
                        case MessageBoxType.YesNoCancel:
                            return MessageBoxResult.Yes;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                case ContentDialogResult.Secondary:
                    switch (_messageBoxType)
                    {
                        case MessageBoxType.Ok:
                        case MessageBoxType.OkCancel:
                            return MessageBoxResult.Cancel;
                        case MessageBoxType.YesNo:
                        case MessageBoxType.YesNoCancel:
                            return MessageBoxResult.No;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}