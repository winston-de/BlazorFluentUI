using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorFluentUI
{
    public partial class BFUIcon : BFUComponentBase
    {
        [Obsolete("Use IconName instead")]
        [Parameter]
        public string Icon
        {
            set
            {
                IconName = value;
            }
        }
        string iconName;
        [Parameter] public string IconName 
        {
            get => iconName;
            set {
                iconName = value;
                // if(value == null) 
                //     return;
                // var classname = string.Concat(value.Select(c => {
                //     if(c >= 'A' && c <= 'Z')
                //         return $"_{c.ToString().ToLower()}";
                //     else
                //         return c.ToString();
                // }));
                // IconClass = $"icon-ic_fluent{classname}_24_regular";
                // ClassName = value;
            }
        }

        public string IconClass { get; set; }
        [Parameter] public string? IconSrc { get; set; }
        [Parameter] public IconType IconType { get; set; }
        [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string,object> ExtraParameters { get; set; }
    }
}
