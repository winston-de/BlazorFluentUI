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
    public class MaskTextFieldBase : FabricComponentBase
    {

        [Inject] private IJSRuntime JSRuntime { get; set; }

        #region TextFieldParam
        [Parameter] public bool Required { get; set; }
        [Parameter] public bool Multiline { get; set; }
        [Parameter] public InputType InputType { get; set; } = InputType.Text;
        [Parameter] public bool Resizable { get; set; } = true;
        [Parameter] public bool AutoAdjustHeight { get; set; }
        [Parameter] public bool Underlined { get; set; }
        [Parameter] public bool Borderless { get; set; }
        [Parameter] public string Label { get; set; }
        [Parameter] public RenderFragment RenderLabel { get; set; }
        [Parameter] public string Description { get; set; }
        [Parameter] public string Prefix { get; set; }
        [Parameter] public string Suffix { get; set; }
        [Parameter] public string DefaultValue { get; set; }
        [Parameter] public string Value { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool ReadOnly { get; set; }
        [Parameter] public string ErrorMessage { get; set; }
        [Parameter] public bool ValidateOnFocusIn { get; set; }
        [Parameter] public bool ValidateOnFocusOut { get; set; }
        [Parameter] public bool ValidateOnLoad { get; set; } = true;
        [Parameter] public int DeferredValidationTime { get; set; } = 200;
        [Parameter] public AutoComplete AutoComplete { get; set; } = AutoComplete.On;
        [Parameter] public string Placeholder { get; set; }
        [Parameter] public string IconName { get; set; }

        [Parameter]
        public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }
        [Parameter]
        public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }
        [Parameter]
        public EventCallback<KeyboardEventArgs> OnKeyPress { get; set; }
        [Parameter]
        public Func<string, string> OnGetErrorMessage { get; set; }
        [Parameter]
        public Action<string, string> OnNotifyValidationResult { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }  // expose click event for Combobox and pickers
        [Parameter]
        public EventCallback<FocusEventArgs> OnBlur { get; set; }
        [Parameter]
        public EventCallback<FocusEventArgs> OnFocus { get; set; }

        [Parameter]
        public EventCallback<string> OnChange { get; set; }
        [Parameter]
        public EventCallback<string> OnInput { get; set; }

        #endregion


        [Parameter] public string Mask { get; set; }
        [Parameter] public char? MaskChar { get; set; }
        [Parameter] public IDictionary<char, Regex> MaskFormat { get; set; }

        private MaskWorker maskWorker;
        private Selection selection;
        private ICollection<MaskValue> maskCharData;
        private ChangeSelectionData changeSelectionData;
        private bool moveCursorOnMouseUp;
        private bool isFocused;
        protected TextField textFieldComponent;


        protected string DisplayValue { get; set; }


        protected override void OnInitialized()
        {
            maskWorker = new MaskWorker(MaskFormat);
            selection = new Selection();
            if (OnGetErrorMessage != null)
            {
                if (UnknownParameters == null)
                    UnknownParameters = new Dictionary<string, object>();
                UnknownParameters.Add("OnGetErrorMessage", OnGetErrorMessage);
            }

            if (OnNotifyValidationResult != null)
            {
                UnknownParameters.Add("OnNotifyValidationResult", OnNotifyValidationResult);
            }

            maskCharData = maskWorker.ParseMask(Mask);
            if (!string.IsNullOrWhiteSpace(Value))
            {
                SetValue(Value);
                Value = null;
            }
            DisplayValue = maskWorker.GetMaskDisplay(Mask, maskCharData, MaskChar);
            base.OnInitialized();
        }

        protected override void OnParametersSet()
        {
            Console.WriteLine("OnParametersSet is called");
            base.OnParametersSet();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            Console.WriteLine("OnAfterRender MaskTextField is called");
            if (selection != null && selection.SetSelection)
            {
                selection.SetSelection = false;
                await SetSelectionRange(selection.SelectionStart, selection.SelectionEnd);
            }
        }

        protected string OnGetErrorMessageHandler(string value)
        {
            return OnGetErrorMessage?.Invoke(value);
        }

        protected void OnNotifyValidationResultHandler(string errorMessage, string value)
        {
            OnNotifyValidationResult?.Invoke(errorMessage, value);
        }

        protected async void OnTextFieldInput(string value)
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
                Console.WriteLine("Delete");
                var isDel = changeSelectionData.ChangeType == InputChangeType.Delete;
                var charCount = changeSelectionData.SelectionEnd - changeSelectionData.SelectionStart;

                if (charCount > 0)
                {
                    // charCount is > 0 if range was deleted
                    maskCharData = maskWorker.ClearRange(maskCharData, changeSelectionData.SelectionStart, charCount);
                    cursorPos = maskWorker.GetRightFormatIndex(maskCharData, changeSelectionData.SelectionStart);
                }
                else
                {
                    // If charCount === 0, there was no selection and a single character was deleted
                    if (isDel)
                    {
                        maskCharData = maskWorker.ClearNext(maskCharData, changeSelectionData.SelectionStart);
                        cursorPos = maskWorker.GetRightFormatIndex(maskCharData, changeSelectionData.SelectionStart);
                    }
                    else
                    {
                        maskCharData = maskWorker.ClearPrev(maskCharData, changeSelectionData.SelectionStart);
                        cursorPos = maskWorker.GetLeftFormatIndex(maskCharData, changeSelectionData.SelectionStart);
                    }
                }
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
                int charCount = 1;
                int selectCount = DisplayValue.Length + charCount - value.Length;
                int startPos = changeSelectionData.SelectionEnd - charCount;
                string enteredString = value.Substring(startPos, charCount);

                // Clear the selected range
                maskCharData = maskWorker.ClearRange(maskCharData, startPos, selectCount);
                // Insert the printed character
                cursorPos = maskWorker.InsertString(maskCharData, startPos, enteredString);
            }
            else
            {
                Console.WriteLine($"False: {value} | {value.Length} | {DisplayValue} | {DisplayValue.Length}");
                return;
            }

            changeSelectionData = null;
            DisplayValue = "";// maskWorker.GetMaskDisplay(Mask, maskCharData, MaskChar);
            await InvokeAsync(() => StateHasChanged());
            DisplayValue = maskWorker.GetMaskDisplay(Mask, maskCharData, MaskChar);
            selection.SetSelection = true;
            selection.SelectionStart = cursorPos;
            selection.SelectionEnd = cursorPos;

            await InvokeAsync(() => StateHasChanged());

        }

        protected Task OnTextFieldFocus(FocusEventArgs args)
        {
            Console.WriteLine("OnFocus MaskTextField");
            isFocused = true;
            SetFirstUnfilledMaskPosition();
            return Task.CompletedTask;

        }

        protected Task OnTextFieldBlur(FocusEventArgs args)
        {
            Console.WriteLine("OnBlur MaskTextField");
            isFocused = false;
            moveCursorOnMouseUp = true;
            return Task.CompletedTask;

        }

        protected Task OnTextFieldMouseDown(MouseEventArgs args)
        {
            Console.WriteLine("OnMouseDown MaskTextField");
            if (!isFocused)
            {
                moveCursorOnMouseUp = true;
            }
            return Task.CompletedTask;
        }

        protected Task OnTextFieldMouseUp(MouseEventArgs args)
        {
            Console.WriteLine("OnMouseUp MaskTextField");
            if (moveCursorOnMouseUp)
            {
                moveCursorOnMouseUp = false;
                SetFirstUnfilledMaskPosition();
            }
            return Task.CompletedTask;
        }

        protected async Task OnTextFieldKeyDown(KeyboardEventArgs args)
        {
            Console.WriteLine("OnKeyDown MaskTextField");
            if (args.CtrlKey || args.MetaKey)
                return;

            if (args.Key == "Backspace" || args.Key == "Delete")
            {
                var selectionStart = await GetSelectionStart();
                var selectionEnd = await GetSelectionEnd();
                if (!(args.Key == "Backspace" && selectionEnd > 0) && !(args.Key == "Delete" && selectionStart < DisplayValue.Length))
                    return;

                changeSelectionData = new ChangeSelectionData()
                {
                    ChangeType = args.Key == "Backspace" ? InputChangeType.BackSpace : InputChangeType.Delete,
                    SelectionStart = selectionStart,
                    SelectionEnd = selectionEnd
                };
            }

            //return Task.CompletedTask;
        }

        protected Task OnTextFieldPaste(ClipboardEventArgs args)
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

        #region JsFunctions
        private async Task<int> GetSelectionStart()
        {
            return await JSRuntime.InvokeAsync<int>("BlazorFabricMaskTextField.getSelectionStart", textFieldComponent.textAreaRef);
        }

        private async Task<int> GetSelectionEnd()
        {
            return await JSRuntime.InvokeAsync<int>("BlazorFabricMaskTextField.getSelectionEnd", textFieldComponent.textAreaRef);
        }

        private async Task SetSelectionRange(int start, int end)
        {
            await JSRuntime.InvokeVoidAsync("BlazorFabricMaskTextField.setSelectionRange", textFieldComponent.textAreaRef, start, end);
        }
        #endregion

    }
}
