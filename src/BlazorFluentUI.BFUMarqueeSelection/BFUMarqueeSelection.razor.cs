using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlazorFluentUI
{
    public partial class BFUMarqueeSelection<TItem> : BFUComponentBase, IDisposable
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public bool IsDraggingConstrainedToRoot { get; set; }
        [Parameter] public bool IsEnabled { get; set; }
        [Parameter] public Func<bool>? OnShouldStartSelection { get; set; }

        [Inject] private IJSRuntime? JSRuntime { get; set; }

        [CascadingParameter] public BFUSelectionZone<TItem>? SelectionZone { get; set; }


        private ManualRectangle? dragRect;
        private DotNetObjectReference<BFUMarqueeSelection<TItem>>? dotNetRef;

        public static Dictionary<string, string> GlobalClassNames = new Dictionary<string, string>()
        {
            {"root", "ms-MarqueeSelection"},
            {"dragMask", "ms-MarqueeSelection-dragMask"},
            {"box", "ms-MarqueeSelection-box"},
            {"boxFill", "ms-MarqueeSelection-boxFill"}
        };
        
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                dotNetRef = DotNetObjectReference.Create(this);
                await JSRuntime!.InvokeVoidAsync("BlazorFluentUiMarqueeSelection.registerMarqueeSelection", dotNetRef, RootElementReference, new BFUMarqueeSelectionProps {IsDraggingConstrainedToRoot = this.IsDraggingConstrainedToRoot, IsEnabled = this.IsEnabled });
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        public async void Dispose()
        {
            if (dotNetRef != null)
            {
                await JSRuntime!.InvokeVoidAsync("BlazorFluentUiMarqueeSelection.unregisterMarqueeSelection", dotNetRef);
                dotNetRef?.Dispose();
            }
        }

        [JSInvokable] public Task<bool> OnShouldStartSelectionInternal()
        {
            if (OnShouldStartSelection == null)
                return Task.FromResult(true);
            else
                return Task.FromResult(OnShouldStartSelection.Invoke());
        }

        [JSInvokable] public void SetDragRect(ManualRectangle manualRectangle)
        {
            dragRect = manualRectangle;
        }

        [JSInvokable]
        public void UnselectAll()
        {
            SelectionZone?.ClearSelection();
            //dragRect = manualRectangle;
        }

        [JSInvokable]
        public IEnumerable<int>? GetSelectedIndicesAsync()
        {
            return SelectionZone?.GetSelectedIndices();
            //dragRect = manualRectangle;
        }
    }
}
