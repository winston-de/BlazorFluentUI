using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Linq;
using System.Text.RegularExpressions;

namespace BlazorFabric
{
    public class MaskTextFieldBase : TextFieldBase
    {

        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public string Mask { get; set; }
        [Parameter] public char? MaskChar { get; set; }
        [Parameter] public IDictionary<char, Regex> MaskFormat { get; set; }

        private MaskWorker maskWorker;
        private Selection selection;
        private ICollection<MaskValue> maskCharData;
        private ChangeSelectionData changeSelectionData;
        private bool moveCursorOnMouseUp;
        protected TextFieldBase textFieldRef;

        protected string DisplayValue { get; set; }


        protected override Task OnInitializedAsync()
        {
            maskWorker = new MaskWorker(MaskFormat);
            selection = new Selection();

            maskCharData = maskWorker.ParseMask(Mask);
            if (!string.IsNullOrWhiteSpace(Value))
            {
                SetValue(Value);
                Value = null;
            }
            DisplayValue = maskWorker.GetMaskDisplay(Mask, maskCharData, MaskChar);
            return base.OnInitializedAsync();
        }

        protected override Task OnParametersSetAsync()
        {
            Console.WriteLine("OnParametersSetAsync is called");
            return base.OnParametersSetAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            Console.WriteLine("OnAfterRenderAsync MaskTextField is called");
            if (selection != null && selection.SetSelection)
            {
                selection.SetSelection = false;
                await SetSelectionRange(selection.SelectionStart, selection.SelectionEnd);
                StateHasChanged();
            }
        }

        protected override bool ShouldRender()
        {
            Console.WriteLine("ShouldRender is called");
            return base.ShouldRender();
        }

        protected string OnGetErrorMessageHandler(string value)
        {
            return OnGetErrorMessage?.Invoke(value);
        }

        protected void OnNotifyValidationResultHandler(string errorMessage, string value)
        {
            OnNotifyValidationResult?.Invoke(errorMessage, value);
        }

        protected async void OnInputChange(string value)
        {
            Console.WriteLine("OnInput MaskTextField");

            if (changeSelectionData == null)
            {
                changeSelectionData = new ChangeSelectionData()
                {
                    ChangeType = InputChangeType.Default,
                    SelectionStart = await GetSelectionStart(),
                    SelectionEnd = await GetSelectionEnd()
                };
            }
            if (changeSelectionData == null)
                return;

            int cursorPos = 0;
            if (changeSelectionData.ChangeType == InputChangeType.TextPasted)
            {
                Console.WriteLine("Pasted");
            }
            else if (changeSelectionData.ChangeType == InputChangeType.Delete || changeSelectionData.ChangeType == InputChangeType.BackSpace)
            {
                Console.WriteLine("Deleted");
            }
            else if (value.Length > DisplayValue.Length)
            {
                Console.WriteLine("value.Length > DisplayValue.Length");
                // This case is if the user added characters
                int charCount = value.Length - DisplayValue.Length;
                int startPos = changeSelectionData.SelectionEnd - charCount;
                string enteredString = value.Substring(startPos, charCount);

                Console.WriteLine($"charCount: {charCount} | startPos: {startPos} | enteredString: {enteredString}");

                cursorPos = maskWorker.InsertString(maskCharData, startPos, enteredString);
            }
            else if (value.Length <= DisplayValue.Length)
            {
                Console.WriteLine("value.Length <= DisplayValue.Length");
            }
            else
            {
                Console.WriteLine($"False: {value} | {value.Length} | {DisplayValue} | {DisplayValue.Length}");
                return;
            }

            changeSelectionData = null;
            DisplayValue = maskWorker.GetMaskDisplay(Mask, maskCharData, MaskChar);
            selection.SetSelection = true;
            selection.SelectionStart = cursorPos;
            selection.SelectionEnd = cursorPos;
        }

        protected override async Task OnFocusHandler(FocusEventArgs args)
        {
            Console.WriteLine("OnFocus MaskTextField");
            isFocused = true;
            SetFirstUnfilledMaskPosition();
            //await base.OnFocusHandler(args);
            return;// Task.CompletedTask;
        }
        protected override Task OnBlurHandler(FocusEventArgs args)
        {
            Console.WriteLine("OnBlur MaskTextField");
            isFocused = false;
            moveCursorOnMouseUp = true;
            return Task.CompletedTask;
        }
        protected Task OnMouseDownHandler(MouseEventArgs args)
        {
            Console.WriteLine("OnMouseDown MaskTextField");
            if (!isFocused)
            {
                moveCursorOnMouseUp = true;
            }
            return Task.CompletedTask;
        }
        protected Task OnMouseUpHandler(MouseEventArgs args)
        {
            Console.WriteLine("OnMouseUp MaskTextField");
            if (moveCursorOnMouseUp)
            {
                moveCursorOnMouseUp = false;
                SetFirstUnfilledMaskPosition();
            }
            return Task.CompletedTask;
        }
        protected Task OnKeyDownHandler(KeyboardEventArgs args)
        {
            Console.WriteLine("OnKeyDown MaskTextField");
            return Task.CompletedTask;
        }
        protected Task OnPasteHandler(ClipboardEventArgs args)
        {
            Console.WriteLine("OnPaste MaskTextField");
            return Task.CompletedTask;
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
        
        private async Task<int> GetSelectionStart()
        {
            return await JSRuntime.InvokeAsync<int>("BlazorFabricMaskTextField.getSelectionStart", textFieldRef.textAreaRef);
        }

        private async Task<int> GetSelectionEnd()
        {
            return await JSRuntime.InvokeAsync<int>("BlazorFabricMaskTextField.getSelectionEnd", textFieldRef.textAreaRef);
        }

        private async Task SetSelectionRange(int start, int end)
        {
            await JSRuntime.InvokeVoidAsync("BlazorFabricMaskTextField.setSelectionRange", textFieldRef.textAreaRef, start, end);
        }


        // Move the cursor position to the leftmost unfilled position
        private void SetFirstUnfilledMaskPosition()
        {
            for (int i = 0; i < maskCharData.Count; i++)
            {
                if (!maskCharData.ToArray()[i].Value.HasValue)
                {
                    Console.WriteLine("Set Unfilled Position");
                    selection.SetSelection = true;
                    selection.SelectionStart = maskCharData.ToArray()[i].DisplayIndex;
                    selection.SelectionEnd = maskCharData.ToArray()[i].DisplayIndex;
                    break;
                }
            }
        }


    }
}
