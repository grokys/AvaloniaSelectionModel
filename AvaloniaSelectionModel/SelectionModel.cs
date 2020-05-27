﻿using System;
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
        private int _updateCount;
        private State _state;
        private State _startState;

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

                    if (_updateCount == 0)
                    {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public int SelectedIndex
        {
            get => _state.SelectedIndex;
            set
            {
                value = CoerceIndex(value);

                if (SelectedIndex != value)
                {
                    using (new Operation(this))
                    {
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
                    if (_updateCount != 0)
                    {
                        throw new InvalidOperationException("Cannot change selection mode while update is in progress.");
                    }

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
                    if (_updateCount != 0)
                    {
                        throw new InvalidOperationException("Cannot change source while update is in progress.");
                    }

                    using (new Operation(this))
                    {
                        _source = value;
                        _state.Items?.Dispose();
                        _state.Items = ItemsSourceView<T>.Create(value);

                        if (Items != null)
                        {
                            TrimInvalidSelections();
                        }
                    }
                }
            }
        }

        internal ItemsSourceView<T>? Items => _state.Items;
        internal List<IndexRange>? Ranges => _singleSelect ? null : _state.Ranges ??= new List<IndexRange>();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<SelectionModelSelectionChangedEventArgs<T>>? SelectionChanged;

        public void ClearSelection()
        {
            using var o = new Operation(this);
            _state.SelectedIndex = _state.AnchorIndex = -1;
            Ranges?.Clear();
        }

        public void Select(int index)
        {
            using var o = new Operation(this);
            SelectImpl(index, false, true);
        }

        public void SelectRange(int start, int end)
        {
            if (SingleSelect)
            {
                throw new InvalidOperationException("Cannot select range with single selection.");
            }

            using var o = new Operation(this);
            var max = Items is object ? Items.Count - 1 : int.MaxValue;

            if (start > max)
            {
                return;
            }

            start = Math.Max(start, 0);
            end = Math.Min(end, max);
            IndexRange.Add(Ranges!, new IndexRange(start, end));

            if (SelectedIndex == -1)
            {
                _state.SelectedIndex = start;
            }

            if (AnchorIndex == -1)
            {
                _state.AnchorIndex = start;
            }
        }

        public void Deselect(int index)
        {
            using var o = new Operation(this);
            DeselectImpl(index);
        }

        public void DeselectRange(int start, int end)
        {
            using var o = new Operation(this);

            start = Math.Max(start, 0);

            if (SingleSelect)
            {
                if (SelectedIndex >= start && SelectedIndex <= end)
                {
                    DeselectImpl(SelectedIndex);
                }
            }
            else
            {
                IndexRange.Remove(Ranges!, new IndexRange(start, end));

                if (SelectedIndex >= start && SelectedIndex <= end)
                {
                    _state.SelectedIndex = Ranges!.Count > 0 ? Ranges[0].Begin : -1;
                }
            }
        }

        public void BeginBatchUpdate()
        {
            if (_updateCount++ == 0)
            {
                _startState = _state;
                _startState.Ranges = Ranges?.ToList();
            }
        }

        public void EndBatchUpdate()
        {
            if (--_updateCount == 0)
            {
                if (SelectionChanged is object)
                {
                    IReadOnlyList<IndexRange>? selected = null;
                    IReadOnlyList<IndexRange>? deselected = null;

                    if (_startState.Ranges is null)
                    {
                        if (_startState.SelectedIndex != _state.SelectedIndex)
                        {
                            if (_state.SelectedIndex != -1)
                            {
                                selected = new[] { new IndexRange(_state.SelectedIndex) };
                            }

                            if (_startState.SelectedIndex != -1)
                            {
                                deselected = new[] { new IndexRange(_startState.SelectedIndex) };
                            }
                        }
                    }
                    else 
                    {
                        IndexRange.Diff(_startState.Ranges, _state.Ranges!, out deselected, out selected);
                    }

                    if (selected?.Count > 0 || deselected?.Count > 0)
                    {
                        var e = new SelectionModelSelectionChangedEventArgs<T>(
                            SelectedIndexes<T>.Create(deselected),
                            SelectedIndexes<T>.Create(selected),
                            SelectedItems<T>.Create(deselected, _startState.Items),
                            SelectedItems<T>.Create(selected, _state.Items));
                        SelectionChanged(this, e);
                    }
                }

                if (_startState.SelectedIndex != _state.SelectedIndex)
                {
                    RaisePropertyChanged(nameof(SelectedIndex));
                }

                if (_startState.AnchorIndex != _state.AnchorIndex)
                {
                    RaisePropertyChanged(nameof(AnchorIndex));
                }
            }
        }

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
            index = CoerceIndex(index);

            if (index == -1)
            {
                return;
            }

            if (SingleSelect || reset || SelectedIndex == -1)
            {
                if (_state.SelectedIndex != index)
                {
                    _state.SelectedIndex = index;

                    if (reset)
                    {
                        Ranges?.Clear();
                    }

                    Ranges?.Add(new IndexRange(index));

                    if (setAnchor)
                    {
                        _state.AnchorIndex = index;
                    }
                }
            }
            else if (index >= 0)
            {
                if (Ranges is null)
                {
                    throw new AvaloniaInternalException("Ranges was null but multiple selection is enabled.");
                }

                IndexRange.Add(Ranges, new IndexRange(index));

                if (setAnchor && _state.AnchorIndex != index)
                {
                    _state.AnchorIndex = index;
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

            if (SingleSelect)
            {
                if (_state.SelectedIndex == index)
                {
                    _state.SelectedIndex = -1;
                }
            }
            else
            {
                if (Ranges is null)
                {
                    throw new AvaloniaInternalException("Ranges was null but multiple selection is enabled.");
                }

                IndexRange.Remove(Ranges, new IndexRange(index));
            }
        }

        private void TrimInvalidSelections()
        {
            if (Items is null)
            {
                throw new AvaloniaInternalException("Cannot trim invalid selections on null source.");
            }

            using var o = new Operation(this);

            if (SingleSelect)
            {
                if (SelectedIndex >= Items.Count)
                {
                    ClearSelection();
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
            public ItemsSourceView<T> Items;
            public List<IndexRange>? Ranges;
        }

        private struct Operation : IDisposable
        {
            private readonly SelectionModel<T> _owner;

            public Operation(SelectionModel<T> owner)
            {
                _owner = owner;
                _owner.BeginBatchUpdate();
            }

            public void Dispose()
            {
                _owner.EndBatchUpdate();
            }
        }
    }
}
