using Microsoft.AspNetCore.Components;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BlazorFluentUI
{
    public class BFUDetailsRowColumn<TItem, TProp> : BFUDetailsRowColumn<TItem>
    {
        //public RenderFragment<TProp> ColumnItemTemplate { get; set; }
        //public Func<TItem, object> FieldSelector { get; set; }
        private Func<TProp, bool> _filterPredicate;
        public new Func<TProp, bool> FilterPredicate { get=>_filterPredicate; set { _filterPredicate = value; base.FilterPredicate = x => FilterPredicate != null ? FilterPredicate((TProp)x) : true; } }

        public BFUDetailsRowColumn()
        {
            PropType = typeof(TProp);
            //base.FilterPredicate = x => FilterPredicate != null ? FilterPredicate((TProp)x) : true;
            Initialize();
        }
        public BFUDetailsRowColumn(string fieldName, Func<TItem, object> fieldSelector)
        {
            PropType = typeof(TProp);

            
            Name = fieldName;
            Key = fieldName;
            AriaLabel = fieldName;
            FieldSelector = fieldSelector;

            Initialize();
        }
    }

    public abstract class BFUDetailsRowColumn<TItem> : INotifyPropertyChanged
    {
        public Type PropType { get; protected set; }

        public string AriaLabel { get; set; }
        public double CalculatedWidth { get; set; } = double.NaN;
        public ColumnActionsMode ColumnActionsMode { get; set; } = ColumnActionsMode.Clickable;
        public RenderFragment<object> ColumnItemTemplate { get; set; }
        public Func<TItem, object> FieldSelector { get; set; }  //was IComparable

        private Func<object, bool> _filterPredicate;
        public Func<object,bool> FilterPredicate { get => _filterPredicate; set { if (_filterPredicate == value) return; else { _filterPredicate = value; OnPropertyChanged(); } } }
        public string FilterAriaLabel { get; set; }
        public string GroupAriaLabel { get; set; }
        public string IconClassName { get; set; }
        public string IconName { get; set; }
        /// <summary>
        /// Forces columns to be in a particular order.  Useful for libraries (like DynamicData) that don't maintain order of collections internally.
        /// </summary>
        public int Index { get; set; }
        public bool IsCollapsible { get; set; }
        public bool IsFiltered { get; set; }
        public bool IsGrouped { get; set; }
        public bool IsIconOnly { get; set; }
        public bool IsMenuOpen { get; set; }
        public bool IsMultiline { get; set; }
        public bool IsPadded { get; set; }
        public bool IsResizable { get; set; }
        public bool IsRowHeader { get; set; }  // only one can be set, it's for the "role" (and a style is set, too)
        
        private bool _isSorted;
        public bool IsSorted { get => _isSorted; set { if (_isSorted == value) return; else { _isSorted = value; OnPropertyChanged(); } } }
        //public EventCallback<bool> IsSortedChanged { get; set; }
        
        private bool _isSortedDescending; 
        public bool IsSortedDescending { get => _isSortedDescending; set { if (_isSortedDescending == value) return; else { _isSortedDescending = value; OnPropertyChanged(); } } }
        //public EventCallback<bool> IsSortedDescendingChanged { get; set; }
        public string Key { get; set; }
        public double MaxWidth { get; set; } = 300;
        public double MinWidth { get; set; } = 100;
        public string Name { get; set; }
        public Action<BFUDetailsRowColumn<TItem>> OnColumnClick { get; set; }
        public Action<BFUDetailsRowColumn<TItem>> OnColumnContextMenu { get; set; }
        public string SortedAscendingAriaLabel { get; set; }
        public string SortedDescendingAriaLabel { get; set; }
        public int SortPriority { get; set; }
        public Type Type { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public IObservable<PropertyChangedEventArgs> PropertyChangedObs { get; private set; }

        protected void Initialize()
        {
            

            this.PropertyChangedObs = Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
              handler =>
              {
                  PropertyChangedEventHandler changed = (sender, e) => handler(e);
                  return changed;
              },
              handler => this.PropertyChanged += handler,
              handler => this.PropertyChanged -= handler);
        }

        

    }



}
