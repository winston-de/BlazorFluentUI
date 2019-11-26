using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace BlazorFabric
{
    public class MaskTextFieldBase : TextFieldBase
    {
        [Parameter] public string Mask { get; set; }
        [Parameter] public string MaskChar { get; set; }
        [Parameter] public string MaskFormat { get; set; }


        protected string OnGetErrorMessageHandler(string value)
        {
            return OnGetErrorMessage?.Invoke(value);
        }

        protected void OnNotifyValidationResultHandler(string errorMessage,string value)
        {
            OnNotifyValidationResult?.Invoke(errorMessage, value);
        }

        protected override Task OnFocusHandler(FocusEventArgs args)
        {
            Console.WriteLine("MaskedTextField");
            base.OnFocusHandler(args);
            return Task.CompletedTask;
        }
        protected override Task OnBlurHandler(FocusEventArgs args)
        {
            return Task.CompletedTask;
        }
        protected Task OnMouseDownHandler(MouseEventArgs args)
        {
            return Task.CompletedTask;
        }
        protected Task OnMouseUpHandler(MouseEventArgs args)
        {
            return Task.CompletedTask;
        }
        protected Task OnChangeHandler(string value)
        {
            return Task.CompletedTask;
        }
        protected Task OnKeyDownHandler(KeyboardEventArgs args)
        {
            return Task.CompletedTask;
        }
        protected Task OnPasteHandler(ClipboardEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
