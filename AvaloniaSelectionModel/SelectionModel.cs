using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Avalonia.Controls.Selection
{
    public class SelectionModel<T> : INotifyPropertyChanged
    {
        private IEnumerable<T> _source;
        private ItemsSourceView<T> _items;
        private int _selectedIndex;
        private SelectedIndexList _indexes;

        public SelectionModel()
        {
        }

        public SelectionModel(IEnumerable<T> source)
        {
            Source = source;
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                value = Math.Max(value, -1);

                if (_items is object && value >= _items.Count)
                {
                    value = -1;
                }

                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IReadOnlyList<int> SelectedIndexes => _indexes ??= new SelectedIndexList(this);

        public T SelectedItem
        {
            get => (_selectedIndex > 0 && _items?.Count > _selectedIndex) ? _items[_selectedIndex] : default;
        }

        public bool SingleSelect { get; set; }

        public IEnumerable<T> Source 
        {
            get => _source;
            set
            {
                if (_source != value)
                {
                    _source = value;
                    _items?.Dispose();
                    _items = ItemsSourceView<T>.Create(value);

                    if (_items != null && _selectedIndex >= _items.Count)
                    {
                        SelectedIndex = -1;
                    }

                    RaisePropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class SelectedIndexList : IReadOnlyList<int>
        {
            private readonly SelectionModel<T> _owner;

            public SelectedIndexList(SelectionModel<T> owner) => _owner = owner;

            public int this[int index]
            {
                get
                {
                    if (index != 0 || _owner.SelectedIndex == -1)
                    {
                        throw new IndexOutOfRangeException("The index was out of range.");
                    }

                    return _owner.SelectedIndex;
                }
            }

            public int Count => _owner.SelectedIndex >= 0 ? 1 : 0;

            public IEnumerator<int> GetEnumerator()
            {
                if (_owner.SelectedIndex != -1)
                {
                    yield return _owner.SelectedIndex;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
