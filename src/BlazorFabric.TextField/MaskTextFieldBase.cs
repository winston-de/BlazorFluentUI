using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Linq;

namespace BlazorFabric
{
    public class MaskTextFieldBase : TextFieldBase
    {
        private readonly MaskWorker _maskWorker;
        private ICollection<MaskValue> maskCharData;

        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public string Mask { get; set; }
        [Parameter] public char? MaskChar { get; set; }
        [Parameter] public string MaskFormat { get; set; }

        protected TextFieldBase textFieldRef;

        protected string DisplayValue { get; set; }

        public MaskTextFieldBase()
        {
            _maskWorker = new MaskWorker();
        }

        private void SetValue(string newValue)
        {
            int valueIndex = 0, charDataIndex = 0;
            while (valueIndex < newValue.Length && charDataIndex < maskCharData.Count)
            {
                char testValue = newValue[valueIndex];
                if (maskCharData.ToArray()[charDataIndex].Format.IsMatch(testValue.ToString()))
                {
                    maskCharData.ToArray()[charDataIndex].Value = testValue;
                    charDataIndex++;
                }
                valueIndex++;
            }
        }

        protected override Task OnInitializedAsync()
        {
            maskCharData = _maskWorker.ParseMask(Mask);
            if (!string.IsNullOrWhiteSpace(Value))
            {
                SetValue(Value);
                Value = null;
            }
            DisplayValue = _maskWorker.GetMaskDisplay(Mask,maskCharData,MaskChar);
            return base.OnInitializedAsync();
        }

        protected string OnGetErrorMessageHandler(string value)
        {
            return OnGetErrorMessage?.Invoke(value);
        }

        protected void OnNotifyValidationResultHandler(string errorMessage,string value)
        {
            OnNotifyValidationResult?.Invoke(errorMessage, value);
        }

        protected override async Task OnFocusHandler(FocusEventArgs args)
        {
            //await JSRuntime.InvokeVoidAsync("BlazorFabricMaskTextField.setSelectionRange", textFieldRef.inputReference, 1, 1);
            await base.OnFocusHandler(args);
            return;// Task.CompletedTask;
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
