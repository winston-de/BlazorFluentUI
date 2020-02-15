﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

namespace BlazorFabric
{
    public class ComponentStyle : IComponentStyle
    {
        public ICollection<ILocalCSSheet> LocalCSSheets { get; set; }
        public ObservableCollection<IGlobalCSSheet> GlobalCSSheets { get; set; }
        public ObservableCollection<IGlobalCSSheet> GlobalRulesSheets { get; set; }
        public ObservableRangeCollection<string> GlobalCSRules { get; set; }

        public ComponentStyle()
        {
            LocalCSSheets = new HashSet<ILocalCSSheet>();
            GlobalCSSheets = new ObservableCollection<IGlobalCSSheet>();
            GlobalCSSheets.CollectionChanged += CollectionChanged;
            GlobalRulesSheets = new ObservableCollection<IGlobalCSSheet>();
            GlobalCSRules = new ObservableRangeCollection<string>();

        }

        public bool ComponentStyleExist(object component)
        {
            if (component == null)
                return false;
            var componentType = component.GetType();
            return GlobalRulesSheets.Any(x => x.Component?.GetType() == componentType);
        }

        public bool StyleSheetIsNeeded(object component)
        {
            if (component == null)
                return false;
            var componentType = component.GetType();
            return GlobalCSSheets.Any(x => x.Component?.GetType() == componentType);
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                {
                    
                    if (((IGlobalCSSheet)item).Component != null && !StyleSheetIsNeeded(((IGlobalCSSheet)item).Component))
                    {
                        item.PropertyChanged -= ItemChanged;
                        GlobalRulesSheets.Remove(GlobalRulesSheets.First(x => x.Component?.GetType() == ((IGlobalCSSheet)item).Component.GetType()));
                    }
                    else if(((IGlobalCSSheet)item).Component != null && ((IGlobalCSSheet)item).HasEvent)
                    {
                        item.PropertyChanged -= ItemChanged;
                        GlobalCSSheets.First(x => x.Component?.GetType() == ((IGlobalCSSheet)item).Component.GetType()).HasEvent = true;
                        ((INotifyPropertyChanged)GlobalCSSheets.First(x => x.Component?.GetType() == ((IGlobalCSSheet)item).Component.GetType())).PropertyChanged += ItemChanged;
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    if (!ComponentStyleExist(((IGlobalCSSheet)item).Component))
                    {
                        GlobalRulesSheets.Add((IGlobalCSSheet)item);
                        ((IGlobalCSSheet)item).HasEvent = true;
                        item.PropertyChanged += ItemChanged;
                    }
                }
            }
        }
        private void ItemChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateGlobalRules();
        }

        private void UpdateGlobalRules()
        {
            var newRules = GetGlobalCSRules();
            if (newRules?.Count > 0)
            {
                GlobalCSRules.ReplaceRange(newRules);
            }
        }

        private ICollection<string> GetNewRules(ICollection<Rule> rules)
        {
            var addRules = new Collection<string>();
            foreach (var rule in rules)
            {
                var ruleAsString = PrintRule(rule);
                if (!GlobalCSRules.Contains(ruleAsString))
                {
                    addRules.Add(ruleAsString);
                }
            }
            return addRules;
        }

        private ICollection<string> GetOldRules()
        {
            var addRules = new Collection<string>();
            foreach (var styleSheet in GlobalRulesSheets)
            {
                foreach (var rule in styleSheet.Rules)
                {
                    var ruleAsString = PrintRule(rule);
                    if (!GlobalCSRules.Contains(ruleAsString))
                    {
                        addRules.Add(ruleAsString);
                    }
                }
            }
            return addRules;
        }

        private ICollection<string> GetGlobalCSRules()
        {
            var globalCSRules = new Collection<string>();
            var update = false;
            foreach (var styleSheet in GlobalRulesSheets)
            {
                if (styleSheet.Rules == null)
                    continue;
                foreach (var rule in styleSheet.Rules)
                {
                    var ruleAsString = PrintRule(rule);
                    if (!globalCSRules.Contains(ruleAsString))
                    {
                        globalCSRules.Add(ruleAsString);
                    }
                    if (!GlobalCSRules.Contains(ruleAsString))
                    {
                        update = true;
                    }
                }
            }
            if (!update)
            {
                foreach (var rule in GlobalCSRules)
                {
                    if (!globalCSRules.Contains(rule))
                    {
                        update = true;
                    }
                }
            }
            if (update)
                return globalCSRules;
            return null;
        }

        public string PrintRule(Rule rule)
        {
            var ruleAsString = "";
            ruleAsString += $"{rule.Selector.GetSelectorAsString()}{{";
            foreach (var property in rule.Properties.GetType().GetProperties())
            {
                string cssProperty = "";
                string cssValue = "";
                Attribute attribute = null;

                //Catch Ignore Propertie
                attribute = property.GetCustomAttribute(typeof(CsIgnoreAttribute));
                if (attribute != null)
                    continue;

                attribute = property.GetCustomAttribute(typeof(CsPropertyAttribute));
                if (attribute != null)
                {
                    if ((attribute as CsPropertyAttribute).IsCssStringProperty)
                    {
                        ruleAsString += property.GetValue(rule.Properties)?.ToString();
                        continue;
                    }

                    cssProperty = (attribute as CsPropertyAttribute).PropertyName;
                }
                else
                {
                    cssProperty = property.Name;
                }

                cssValue = property.GetValue(rule.Properties)?.ToString();
                if (cssValue != null)
                {
                    ruleAsString += $"{cssProperty.ToLower()}:{(string.IsNullOrEmpty(cssValue) ? "\"\"" : cssValue)};";
                }
            }
            ruleAsString += "}";
            return ruleAsString;
        }
    }
}