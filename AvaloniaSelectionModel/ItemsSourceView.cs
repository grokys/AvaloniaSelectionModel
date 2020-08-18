// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls.Utils;

#nullable enable

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
    public class ItemsSourceView<T> : INotifyCollectionChanged, IDisposable, IReadOnlyList<T>
    {
        /// <summary>
        ///  Gets an empty <see cref="ItemsSourceView"/>
        /// </summary>
        public static ItemsSourceView<T> Empty { get; } = new ItemsSourceView<T>(Array.Empty<T>());

        private readonly IList<T> _inner;
        private INotifyCollectionChanged? _notifyCollectionChanged;

        /// <summary>
        /// Initializes a new instance of the ItemsSourceView class for the specified data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        public ItemsSourceView(IEnumerable source)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));

            if (source is IList<T> list)
            {
                _inner = list;
            }
            else if (source is IEnumerable<T> objectEnumerable)
            {
                _inner = new List<T>(objectEnumerable);
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
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public T this[int index] => GetAt(index);

        /// <summary>
        /// Occurs when the collection has changed to indicate the reason for the change and which items changed.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

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
        /// <returns>The item.</returns>
        public T GetAt(int index) => _inner[index];

        public int IndexOf(T item) => _inner.IndexOf(item);

        public static ItemsSourceView<T> GetOrCreate(IEnumerable<T>? items)
        {
            if (items is ItemsSourceView<T> isv)
            {
                return isv;
            }
            else if (items is null)
            {
                return Empty;
            }
            else
            {
                return new ItemsSourceView<T>(items);
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(_inner);
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        internal void AddListener(ICollectionChangedListener listener)
        {
            if (_inner is INotifyCollectionChanged incc)
            {
                CollectionChangedEventManager.Instance.AddListener(incc, listener);
            }
        }

        internal void RemoveListener(ICollectionChangedListener listener)
        {
            if (_inner is INotifyCollectionChanged incc)
            {
                CollectionChangedEventManager.Instance.RemoveListener(incc, listener);
            }
        }

        protected void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
        {
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

        public struct Enumerator : IEnumerator<T>
        {
            private readonly IEnumerator<T> _innerEnumerator;
            public Enumerator(IList<T> inner) => _innerEnumerator = inner.GetEnumerator();
            public T Current => _innerEnumerator.Current;
            object? IEnumerator.Current => Current;
            public void Dispose() => (_innerEnumerator as IDisposable)?.Dispose();
            public bool MoveNext() => _innerEnumerator.MoveNext();
            void IEnumerator.Reset() => _innerEnumerator.Reset();
        }
    }
}
