using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace BlazorFluentUI
{
    public partial class BFUGlobalRules : IDisposable
    {
        [Inject]
        public IComponentStyle? ComponentStyle { get; set; }

        public Action? OnDispose { get; set; }
        private bool _isDisposed = false;

        protected override Task OnInitializedAsync()
        {
            if (ComponentStyle != null)
            {
                ComponentStyle.GlobalRules = this;
                ComponentStyle.SetDisposedAction();
            }
            else
            {
                throw new Exception("Cannot find ComponentStyle.  You probably forgot to wrap your app with a BFUTheme component.");
            }
            return base.OnInitializedAsync();
        }

        public void UpdateGlobalRules()
        {
            if(!_isDisposed)
                InvokeAsync(() => StateHasChanged());
        }

        public void Dispose()
        {
            _isDisposed = true;
            OnDispose?.Invoke();
        }
    }
}
