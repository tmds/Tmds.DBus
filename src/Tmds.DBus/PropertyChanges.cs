// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;

namespace Tmds.DBus
{
    /// <summary>
    /// Event data for the properties changed event.
    /// </summary>
    public struct PropertyChanges
    {
        private KeyValuePair<string, object>[] _changed;
        private string[] _invalidated;

        /// <summary>
        /// Properties that have changed with their new value.
        /// </summary>
        public KeyValuePair<string, object>[] Changed => _changed;

        /// <summary>
        /// Properties that have changed.
        /// </summary>
        public string[] Invalidated => _invalidated;

        /// <summary>
        /// Creates a PropertyChanges event.
        /// </summary>
        /// <param name="changed">Properties that changed with their new value.</param>
        /// <param name="invalidated">Properties that changed without providing new value.</param>
        public PropertyChanges(KeyValuePair<string, object>[] changed,string[] invalidated = null)
        {
            _changed = changed ?? Array.Empty<KeyValuePair<string, object>>();
            _invalidated = invalidated ?? Array.Empty<string>();
        }

        /// <summary>
        /// Creates a PropertyChanges event for a single value change.
        /// </summary>
        public static PropertyChanges ForProperty(string prop, object val)
            => new PropertyChanges(new [] { new KeyValuePair<string, object>(prop, val) });

        /// <summary>
        /// Retrieves value for a specific property. The default is returned when the property is not present.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="name">Property name.</param>
        /// <returns>
        /// Value of the property. <c>default(T)</c> when property not present.
        /// </returns>
        public T GetValue<T>(string name)
        {
            // TODO: value type?
            for (int i = 0; i < Changed.Length; i++)
            {
                if (Changed[i].Key == name)
                {
                    return (T)Changed[i].Value;
                }
            }
            return default(T);
        }
    }
}