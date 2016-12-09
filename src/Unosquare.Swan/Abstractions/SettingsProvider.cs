﻿namespace Unosquare.Swan.Abstractions
{
    using Reflection;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Web.Script.Serialization;

    /// <summary>
    /// Represents a provider to save and load settings using a plain JSON file
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Unosquare.Swan.Abstractions.SingletonBase{Unosquare.Swan.Abstractions.SettingsProvider{T}}" />
    public class SettingsProvider<T> : SingletonBase<SettingsProvider<T>>
    {
        private JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
        private T _global;

        public string ConfigurationFilePath { get; set; } = CurrentApp.EntryAssemblyDirectory;

        /// <summary>
        /// Gets the global settings object
        /// </summary>
        public T Global
        {
            get
            {
                lock (SyncRoot)
                {
                    if (_global == null)
                        ReloadGlobalSettings();

                    return _global;
                }
            }
        }

        /// <summary>
        /// Reloads the global settings.
        /// </summary>
        public void ReloadGlobalSettings()
        {
            lock (SyncRoot)
            {
                if (File.Exists(ConfigurationFilePath) == false)
                {
                    _global = Activator.CreateInstance<T>();
                    PersistGlobalSettings();
                }

                _global = javaScriptSerializer.Deserialize<T>(File.ReadAllText(ConfigurationFilePath));
            }
        }

        /// <summary>
        /// Gets the json data.
        /// </summary>
        /// <returns></returns>
        public string GetJsonData()
        {
            lock (SyncRoot)
            {
                return File.ReadAllText(ConfigurationFilePath);
            }
        }

        /// <summary>
        /// Persists the global settings.
        /// </summary>
        public void PersistGlobalSettings()
        {
            lock (SyncRoot)
            {
                var stringData = javaScriptSerializer.Serialize(Global);
                File.WriteAllText(ConfigurationFilePath, stringData);
            }
        }

        /// <summary>
        /// Updates settings from list.
        /// </summary>
        /// <param name="list">The list.</param>
        public void UpdateFromList(List<ExtendedPropertyInfo> list)
        {
            foreach (var property in list)
            {
                // TODO: evaluate if the value changed and report back

                var prop = Current.Global.GetType().GetTypeInfo().GetProperty(property.Property);
                var originalValue = prop.GetValue(Current.Global);

                if (prop.PropertyType == typeof(bool))
                    prop.SetValue(Current.Global, Convert.ToBoolean(property.Value));
                else if (prop.PropertyType == typeof(int))
                    prop.SetValue(Current.Global, Convert.ToInt32(property.Value));
                else if (prop.PropertyType == typeof(int?))
                {
                    if (property.Value == null) prop.SetValue(Current.Global, property.Value);
                    else prop.SetValue(Current.Global, Convert.ToInt32(property.Value));
                }
                else if (prop.PropertyType == typeof(int[]))
                    prop.SetValue(Current.Global, property.Value.ToString().Split(',').Select(int.Parse).ToArray());
                else if (prop.PropertyType == typeof(string[]))
                    prop.SetValue(Current.Global, property.Value.ToString().Split(',').ToArray());
                else
                    prop.SetValue(Current.Global, property.Value);

                Current.PersistGlobalSettings();
            }
        }

        /// <summary>
        /// Parses the property.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private ExtendedPropertyInfo ParseProperty(string value)
        {
            if (value == null) return null;

            var sets = value.Split('|');
            if (sets.Length != 3) return null;

            var prop = new ExtendedPropertyInfo<T>(sets[1]);
            prop.Value = sets[2];

            return prop;
        }

        /// <summary>
        /// Gets the list.
        /// </summary>
        /// <returns></returns>
        internal List<ExtendedPropertyInfo<T>> GetList()
        {
            var dict = javaScriptSerializer.Deserialize<Dictionary<string, object>>(GetJsonData());

            return dict.Keys
                    .Select(x => new ExtendedPropertyInfo<T>(x) { Value = dict[x] })
                    .ToList();
        }
    }
}