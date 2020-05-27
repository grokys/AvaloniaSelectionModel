using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public class SelectionModel<T> : INotifyPropertyChanged
    {
        private IEnumerable<T>? _source;
        private bool _singleSelect;
        private SelectedIndexes<T>? _selectedIndexes;
        private SelectedItems<T>? _selectedItems;
        private State _state;

        public SelectionModel()
        {
            _state.SelectedIndex = _state.AnchorIndex = -1;
        }

        public SelectionModel(IEnumerable<T> source)
            : this()
        {
            Source = source;
        }

        public int AnchorIndex
        {
            get => _state.AnchorIndex;
            set
            {
                value = CoerceIndex(value);

                if (_state.AnchorIndex != value)
                {
                    _state.AnchorIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int SelectedIndex
        {
            get => _state.SelectedIndex;
            set
            {
                value = CoerceIndex(value);

                if (value >= 0)
                {
                    SelectImpl(value, true, true);
                }
                else
                {
                    ClearSelection();
                }
            }
        }

        [MaybeNull]
        public T SelectedItem
        {
            get => (_state.SelectedIndex >= 0 && Items?.Count > _state.SelectedIndex) ? Items[_state.SelectedIndex] : default;
        }

        public IReadOnlyList<int> SelectedIndexes => _selectedIndexes ??= new SelectedIndexes<T>(this);
        public IReadOnlyList<T> SelectedItems => _selectedItems ??= new SelectedItems<T>(this);

        public bool SingleSelect 
        {
            get => _singleSelect;
            set
            {
                if (_singleSelect != value)
                {
                    _singleSelect = value;

                    if (_singleSelect)
                    {
                        _state.Ranges = null;
                    }
                    else
                    {
                        _state.Ranges = new List<IndexRange>();

                        if (_state.SelectedIndex > 0)
                        {
                            _state.Ranges.Add(new IndexRange(_state.SelectedIndex, _state.SelectedIndex));
                        }
                    }

                    RaisePropertyChanged();
                }
            }
        }

        public IEnumerable<T>? Source 
        {
            get => _source;
            set
            {
                if (_source != value)
                {
                    _source = value;
                    Items?.Dispose();
                    Items = ItemsSourceView<T>.Create(value);

                    if (Items != null)
                    {
                        TrimInvalidSelections();
                    }

                    RaisePropertyChanged();
                }
            }
        }

        internal ItemsSourceView<T>? Items { get; private set; }
        internal List<IndexRange>? Ranges => _singleSelect ? null : _state.Ranges ??= new List<IndexRange>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public void ClearSelection()
        {
            var oldSelectedIndex = _state.SelectedIndex;
            var oldAnchorIndex = _state.AnchorIndex;

            _state.SelectedIndex = _state.AnchorIndex = -1;
            Ranges?.Clear();

            if (_state.SelectedIndex !=  oldSelectedIndex)
            {
                RaisePropertyChanged(nameof(SelectedIndex));
            }

            if (_state.AnchorIndex != oldAnchorIndex)
            {
                RaisePropertyChanged(nameof(AnchorIndex));
            }
        }

        public void Select(int index) => SelectImpl(index, false, true);

        public void Deselect(int index) => DeselectImpl(index);

        private int CoerceIndex(int index)
        {
            index = Math.Max(index, -1);

            if (Items is object && index >= Items.Count)
            {
                index = -1;
            }

            return index;
        }

        private void SelectImpl(int index, bool reset, bool setAnchor)
        {
            if (index == -1)
            {
                throw new AvaloniaInternalException("Cannot select index -1.");
            }

            index = CoerceIndex(index);

            if (SingleSelect || reset || SelectedIndex == -1)
            {
                if (_state.SelectedIndex != index)
                {
                    var oldAnchorIndex = AnchorIndex;

                    _state.SelectedIndex = index;

                    if (reset)
                    {
                        Ranges?.Clear();
                    }

                    if (index != -1)
                    {
                        Ranges?.Add(new IndexRange(index, index));
                    }

                    if (setAnchor)
                    {
                        _state.AnchorIndex = index;
                    }

                    RaisePropertyChanged();

                    if (oldAnchorIndex != AnchorIndex)
                    {
                        RaisePropertyChanged(nameof(AnchorIndex));
                    }
                }
            }
            else if (index >= 0)
            {
                if (Ranges is null)
                {
                    throw new AvaloniaInternalException("Ranges was null but multiple selection is enabled.");
                }

                IndexRange.Add(Ranges, new IndexRange(index, index));

                if (setAnchor && _state.AnchorIndex != index)
                {
                    _state.AnchorIndex = index;
                    RaisePropertyChanged(nameof(AnchorIndex));
                }
            }
        }

        private void DeselectImpl(int index)
        {
            index = CoerceIndex(index);

            if (index == -1)
            {
                return;
            }

            if (SingleSelect && _state.SelectedIndex == index)
            {
                _state.SelectedIndex = -1;
                RaisePropertyChanged(nameof(SelectedIndex));
            }
            else
            {
                if (Ranges is null)
                {
                    throw new AvaloniaInternalException("Ranges was null but multiple selection is enabled.");
                }

                IndexRange.Remove(Ranges, new IndexRange(index, index));
            }
        }

        private void TrimInvalidSelections()
        {
            if (Items is null)
            {
                throw new AvaloniaInternalException("Cannot trim invalid selections on null source.");
            }

            if (SingleSelect)
            {
                if (SelectedIndex >= Items.Count)
                {
                    SelectedIndex = -1;
                }
            }
            else
            {
                if (Ranges is null)
                {
                    throw new AvaloniaInternalException("Ranges was null but multiple selection is enabled.");
                }

                var validRange = new IndexRange(0, Items.Count - 1);
                IndexRange.Intersect(Ranges, validRange);

                if (SelectedIndex >= Items.Count)
                {
                    _state.SelectedIndex = Ranges.Count > 0 ? Ranges[0].Begin : -1;
                    RaisePropertyChanged(nameof(SelectedIndex));
                }
            }
        }

        private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private struct State
        {
            public int AnchorIndex;
            public int SelectedIndex;
            public List<IndexRange>? Ranges;
        }
    }
}
