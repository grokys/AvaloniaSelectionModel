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
        private int _anchorIndex;
        private int _selectedIndex;
        private SelectedIndexList _indexes;

        public SelectionModel()
        {
        }

        public SelectionModel(IEnumerable<T> source)
        {
            Source = source;
        }

        public int AnchorIndex
        {
            get => _anchorIndex;
            set
            {
                value = CoerceIndex(value);

                if (_anchorIndex != value)
                {
                    _anchorIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SelectImpl(value, true);
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

        public void Select(int index) => SelectImpl(index, true);

        public void Deselect(int index)
        {
            if (SelectedIndex == index)
            {
                SelectImpl(-1, false);
            }
        }

        private int CoerceIndex(int index)
        {
            index = Math.Max(index, -1);

            if (_items is object && index >= _items.Count)
            {
                index = -1;
            }

            return index;
        }

        private void SelectImpl(int index, bool setAnchor)
        {
            index = CoerceIndex(index);

            if (_selectedIndex != index)
            {
                var oldAnchorIndex = AnchorIndex;
                
                _selectedIndex = index;

                if (setAnchor)
                {
                    _anchorIndex = index;
                }
                
                RaisePropertyChanged();

                if (oldAnchorIndex != AnchorIndex)
                {
                    RaisePropertyChanged(nameof(AnchorIndex));
                }
            }
        }

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
