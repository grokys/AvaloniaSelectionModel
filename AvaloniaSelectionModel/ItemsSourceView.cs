// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a standardized view of the supported interactions between a given ItemsSource
    /// object and an <see cref="ItemsRepeater"/> control.
    /// </summary>
    /// <remarks>
    /// Components written to work with ItemsRepeater should consume the
    /// <see cref="ItemsRepeater.Items"/> via ItemsSourceView since this provides a normalized
    /// view of the Items. That way, each component does not need to know if the source is an
    /// IEnumerable, an IList, or something else.
    /// </remarks>
    public class ItemsSourceView<T> : IReadOnlyList<T>, INotifyCollectionChanged, IDisposable
    {
        private readonly IList<T> _inner;
        private INotifyCollectionChanged _notifyCollectionChanged;

        /// <summary>
        /// Initializes a new instance of the ItemsSourceView class for the specified data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        public ItemsSourceView(IEnumerable<T> source)
            : this((IEnumerable)source)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ItemsSourceView class for the specified data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        protected ItemsSourceView(IEnumerable source)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));

            if (source is IList<T> list)
            {
                _inner = list;
            }
            else if (source is IEnumerable<T> enumerable)
            {
                _inner = new List<T>(enumerable);
            }
            else
            {
                _inner = new List<T>(source.Cast<T>());
            }

            ListenToCollectionChanges();
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count => _inner.Count;

        /// <summary>
        /// Gets a value that indicates whether the items source can provide a unique key for each item.
        /// </summary>
        /// <remarks>
        /// TODO: Not yet implemented in Avalonia.
        /// </remarks>
        public bool HasKeyIndexMapping => false;

        /// <summary>
        ///  Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        public T this[int index] => _inner[index];

        /// <summary>
        /// Occurs when the collection has changed to indicate the reason for the change and which items changed.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_notifyCollectionChanged != null)
            {
                _notifyCollectionChanged.CollectionChanged -= OnCollectionChanged;
            }
        }

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>the item.</returns>
        public object GetAt(int index) => _inner[index];

        public int IndexOf(T item) => _inner.IndexOf(item);

        /// <summary>
        /// Retrieves the index of the item that has the specified unique identifier (key).
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The key</returns>
        /// <remarks>
        /// TODO: Not yet implemented in Avalonia.
        /// </remarks>
        public string KeyFromIndex(int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the unique identifier (key) for the item at the specified index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The index.</returns>
        /// <remarks>
        /// TODO: Not yet implemented in Avalonia.
        /// </remarks>
        public int IndexFromKey(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        internal static ItemsSourceView<T> Create(IEnumerable<T> items)
        {
            return items switch
            {
                ItemsSourceView<T> isv => isv,
                null => null,
                _ => new ItemsSourceView<T>(items),
            };
        }

        protected void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
        {
            _cachedSize = _inner.Count;
            CollectionChanged?.Invoke(this, args);
        }

        private void ListenToCollectionChanges()
        {
            if (_inner is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += OnCollectionChanged;
                _notifyCollectionChanged = incc;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnItemsSourceChanged(e);
        }
    }

    public class ItemsSourceView : ItemsSourceView<object>
    {
        public ItemsSourceView(IEnumerable source)
            : base(source)
        {
        }
    }
}
