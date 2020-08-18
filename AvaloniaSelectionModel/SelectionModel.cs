using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AvaloniaSelectionModel;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public class SelectionModel<T> : SelectionNodeBase<T>, INotifyPropertyChanged
    {
        private bool _singleSelect = true;
        private int _anchorIndex = -1;
        private int _selectedIndex = -1;
        private Operation? _operation;
        private SelectedIndexes<T>? _selectedIndexes;
        private SelectedItems<T>? _selectedItems;

        public override IEnumerable<T>? Source
        {
            get => base.Source;
            set
            {
                if (base.Source != value)
                {
                    if (_operation is object)
                    {
                        throw new InvalidOperationException("Cannot change source while update is in progress.");
                    }

                    base.Source = value;

                    using var update = BatchUpdate();
                    update.Operation.IsSourceUpdate = true;
                    TrimInvalidSelections(update.Operation);
                }
            }
        }

        public bool SingleSelect 
        {
            get => _singleSelect;
            set
            {
                if (_singleSelect != value)
                {
                    _singleSelect = value;
                    RangesEnabled = !value;

                    if (RangesEnabled && _selectedIndex >= 0)
                    {
                        CommitSelect(new IndexRange(_selectedIndex));
                    }
               }
            }
        }

        public int SelectedIndex 
        {
            get => _selectedIndex;
            set => SetSelectedIndex(value);
        }

        public IEnumerable<int> SelectedIndexes => _selectedIndexes ??= new SelectedIndexes<T>(this);

        [MaybeNull]
        public T SelectedItem => GetItemAt(_selectedIndex);

        public IEnumerable<T> SelectedItems => _selectedItems ??= new SelectedItems<T>(this);

        public int AnchorIndex 
        {
            get => _anchorIndex;
            set
            {
                using var update = BatchUpdate();
                var index = CoerceIndex(value);
                update.Operation.AnchorIndex = index;
            }
        }

        public event EventHandler<SelectionModelIndexesChangedEventArgs>? IndexesChanged;
        public event EventHandler<SelectionModelSelectionChangedEventArgs<T>>? SelectionChanged;
        public event EventHandler? SelectionReset;
        public event PropertyChangedEventHandler? PropertyChanged;

        public BatchUpdateOperation BatchUpdate() => new BatchUpdateOperation(this);

        public void BeginBatchUpdate()
        {
            _operation ??= new Operation(this);
            ++_operation.UpdateCount;
        }

        public void EndBatchUpdate()
        {
            if (_operation is null || _operation.UpdateCount == 0)
            {
                throw new InvalidOperationException("No batch update in progress.");
            }

            if (--_operation.UpdateCount == 0)
            {
                // If the collection is currently changing, commit the update when the
                // collection change finishes.
                if (!IsSourceCollectionChanging)
                {
                    CommitOperation(_operation);
                }
            }
        }

        public bool IsSelected(int index)
        {
            if (index < 0)
            {
                return false;
            }
            else if (SingleSelect)
            {
                return _selectedIndex == index;
            }
            else
            {
                return IndexRange.Contains(Ranges, index);
            }
        }

        public void Select(int index)
        {
            index = CoerceIndex(index);

            if (index < 0)
            {
                return;
            }

            if (SingleSelect)
            {
                SetSelectedIndex(index);
            }
            else if (!IsSelected(index))
            {
                using var update = BatchUpdate();
                var o = update.Operation;

                o.SelectedRanges ??= new List<IndexRange>();
                IndexRange.Add(o.SelectedRanges, new IndexRange(index));

                if (o.SelectedIndex == -1)
                {
                    o.SelectedIndex = index;
                }

                o.AnchorIndex = index;
            }
        }

        public void Deselect(int index)
        {
            if (IsSelected(index))
            {
                if (SingleSelect)
                {
                    SetSelectedIndex(-1, false);
                }
                else
                {
                    using var update = BatchUpdate();
                    var o = update.Operation;

                    o.DeselectedRanges ??= new List<IndexRange>();
                    IndexRange.Add(o.DeselectedRanges, new IndexRange(index));

                    if (o.SelectedIndex == index)
                    {
                        o.SelectedIndex = GetFirstSelectedIndexFromRanges(except: o.DeselectedRanges);
                    }
                }
            }
        }

        public void SelectRange(int start, int end)
        {
            if (SingleSelect)
            {
                throw new InvalidOperationException("Cannot select range with single selection.");
            }

            var range = CoerceRange(start, end);

            if (range.Begin == -1)
            {
                return;
            }

            using var update = BatchUpdate();
            var o = update.Operation;

            // Add the selected range to the operation.
            o.SelectedRanges ??= new List<IndexRange>();
            IndexRange.Add(o.SelectedRanges, range);
            
            // Remove any items that were already selected.
            IndexRange.Remove(o.SelectedRanges, Ranges);

            // Remove the selected items from the deselected items in the operation.
            if (o.DeselectedRanges is object)
            {
                foreach (var r in o.SelectedRanges)
                {
                    IndexRange.Remove(o.DeselectedRanges, r);
                }
            }

            // Update the AnchorIndex and SelectedIndex if necessary.
            if (o.SelectedIndex == -1)
            {
                o.SelectedIndex = range.Begin;
            }

            if (o.AnchorIndex == -1)
            {
                o.AnchorIndex = range.Begin;
            }
        }

        public void DeselectRange(int start, int end)
        {
            var range = CoerceRange(start, end);

            if (SingleSelect)
            {
                if (range.Contains(_selectedIndex))
                {
                    SelectedIndex = -1;
                }
            }
            else if (range.Begin != -1)
            {
                using var update = BatchUpdate();
                var o = update.Operation;

                // The deselected items are the intersection of the currently selected items
                // and the range to deselect.
                var deselected = Ranges.ToList();
                IndexRange.Intersect(deselected, range);

                // Add the deselected items to the operation.
                o.DeselectedRanges ??= new List<IndexRange>();
                IndexRange.Add(o.DeselectedRanges, deselected);

                // Remove the deselected items from the selected items in the operation.
                if (o.SelectedRanges is object)
                {
                    IndexRange.Remove(o.SelectedRanges, deselected);
                }

                // Update the AnchorIndex and SelectedIndex if necessary.
                var anchorIndexInvalidated = IndexRange.Contains(deselected, o.AnchorIndex);
                var selectedIndexInvalidated = IndexRange.Contains(deselected, o.SelectedIndex);

                if (anchorIndexInvalidated || selectedIndexInvalidated)
                {
                    var newValue = GetFirstSelectedIndexFromRanges(except: deselected);
                    o.SelectedIndex = selectedIndexInvalidated ? newValue : o.SelectedIndex;
                    o.AnchorIndex = anchorIndexInvalidated ? newValue : o.AnchorIndex;
                }
            }
        }

        public void Clear()
        {
            if (SingleSelect)
            {
                SelectedIndex = -1;
            }
            else
            {
                using var update = BatchUpdate();
                var o = update.Operation;
                o.DeselectedRanges = Ranges.ToList();
                o.SelectedRanges = null;
                o.SelectedIndex = o.AnchorIndex = 0;
            }
        }

        private protected override void OnIndexesChanged(int shiftIndex, int shiftDelta)
        {
            IndexesChanged?.Invoke(this, new SelectionModelIndexesChangedEventArgs(shiftIndex, shiftDelta));
        }

        private protected override void OnItemsReset()
        {
            _selectedIndex = _anchorIndex = -1;
            SelectionReset?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(this, new SelectionModelSelectionChangedEventArgs<T>());
        }

        private protected override void OnSelectionChanged(IReadOnlyList<T> deselectedItems)
        {
            SelectionChanged?.Invoke(
                this,
                new SelectionModelSelectionChangedEventArgs<T>(deselectedItems: deselectedItems));
        }

        private protected override CollectionChangeState OnItemsAdded(int index, IList items)
        {
            var count = items.Count;
            var shifted = SelectedIndex >= index;
            var shiftCount = shifted ? count : 0;

            _selectedIndex += shiftCount;
            _anchorIndex += shiftCount;

            var baseResult = base.OnItemsAdded(index, items);
            shifted |= baseResult.ShiftDelta != 0;

            return new CollectionChangeState
            {
                ShiftIndex = index,
                ShiftDelta = shifted ? count : 0,
            };
        }

        private protected override CollectionChangeState OnItemsRemoved(int index, IList items)
        {
            var count = items.Count;
            var removedRange = new IndexRange(index, index + count - 1);
            var shifted = false;
            List<T>? removed;

            var baseResult = base.OnItemsRemoved(index, items);
            shifted |= baseResult.ShiftDelta != 0;
            removed = baseResult.RemovedItems;

            if (removedRange.Contains(SelectedIndex))
            {
                if (SingleSelect)
                {
#pragma warning disable CS8604
                    removed = new List<T> { (T)items[SelectedIndex - index] };
#pragma warning restore CS8604
                }

                _selectedIndex = GetFirstSelectedIndexFromRanges();
            }
            else if (SelectedIndex >= index)
            {
                _selectedIndex -= count;
                shifted = true;
            }

            if (removedRange.Contains(AnchorIndex))
            {
                _anchorIndex = GetFirstSelectedIndexFromRanges();
            }
            else if (AnchorIndex >= index)
            {
                _anchorIndex -= count;
                shifted = true;
            }

            return new CollectionChangeState
            {
                ShiftIndex = index,
                ShiftDelta = shifted ? -count : 0,
                RemovedItems = removed,
            };
        }

        private protected override void OnSourceCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_operation?.UpdateCount > 0)
            {
                throw new InvalidOperationException("Source collection was modified during selection update.");
            }

            var oldAnchorIndex = _anchorIndex;
            var oldSelectedIndex = _selectedIndex;

            base.OnSourceCollectionChanged(e);

            if (oldSelectedIndex != _selectedIndex)
            {
                RaisePropertyChanged(nameof(SelectedIndex));
            }

            if (oldAnchorIndex != _anchorIndex)
            {
                RaisePropertyChanged(nameof(AnchorIndex));
            }
        }

        protected override void OnSourceCollectionChangeFinished()
        {
            if (_operation is object)
            {
                CommitOperation(_operation);
            }
        }

        private int GetFirstSelectedIndexFromRanges(List<IndexRange>? except = null)
        {
            if (RangesEnabled)
            {
                var count = IndexRange.GetCount(Ranges);
                var index = 0;

                while (index < count)
                {
                    var result = IndexRange.GetAt(Ranges, index++);

                    if (except is null || !IndexRange.Contains(except, result))
                    {
                        return result;
                    }
                }
            }

            return -1;
        }

        private void SetSelectedIndex(int value, bool updateAnchor = true)
        {
            using var update = BatchUpdate();
            var o = update.Operation;
            var index = CoerceIndex(value);
            
            o.SelectedIndex = index;

            if (RangesEnabled)
            {
                o.DeselectedRanges = Ranges.ToList();

                if (index >= 0)
                {
                    var range = new IndexRange(index);
                    o.SelectedRanges = new List<IndexRange> { range };
                    IndexRange.Remove(o.DeselectedRanges, range);
                }
            }

            if (updateAnchor)
            {
                update.Operation.AnchorIndex = index;
            }
        }

        [return: MaybeNull]
        private T GetItemAt(int index)
        {
            if (ItemsView is null || index < 0 || index >= ItemsView.Count)
            {
                return default;
            }

            return ItemsView.GetAt(index);
        }

        private int CoerceIndex(int index)
        {
            index = Math.Max(index, -1);

            if (ItemsView is object && index >= ItemsView.Count)
            {
                index = -1;
            }

            return index;
        }

        private IndexRange CoerceRange(int start, int end)
        {
            var max = ItemsView is object ? ItemsView.Count - 1 : int.MaxValue;

            if (start > max)
            {
                return new IndexRange(-1);
            }

            start = Math.Max(start, 0);
            end = Math.Min(end, max);

            return new IndexRange(start, end);
        }

        private void TrimInvalidSelections(Operation operation)
        {
            if (ItemsView is null)
            {
                return;
            }

            var max = ItemsView.Count - 1;

            if (operation.SelectedIndex > max)
            {
                operation.SelectedIndex = GetFirstSelectedIndexFromRanges();
            }

            if (operation.AnchorIndex > max)
            {
                operation.AnchorIndex = GetFirstSelectedIndexFromRanges();
            }

            if (RangesEnabled && Ranges.Count > 0)
            {
                var selected = Ranges.ToList();
                
                if (max < 0)
                {
                    operation.DeselectedRanges = selected;
                }
                else
                {
                    var valid = new IndexRange(0, max);
                    var removed = new List<IndexRange>();
                    IndexRange.Intersect(selected, valid, removed);
                    operation.DeselectedRanges = removed;
                }
            }
        }

        private void CommitOperation(Operation operation)
        {
            var oldAnchorIndex = _anchorIndex;
            var oldSelectedIndex = _selectedIndex;

            _selectedIndex = operation.SelectedIndex;
            _anchorIndex = operation.AnchorIndex;

            if (operation.SelectedRanges is object)
            {
                CommitSelect(operation.SelectedRanges);
            }

            if (operation.DeselectedRanges is object)
            {
                CommitDeselect(operation.DeselectedRanges);
            }

            if (SelectionChanged is object)
            {
                IReadOnlyList<IndexRange>? deselected = operation.DeselectedRanges;
                IReadOnlyList<IndexRange>? selected = operation.SelectedRanges;

                if (SingleSelect && oldSelectedIndex != _selectedIndex)
                {
                    if (oldSelectedIndex != -1)
                    {
                        deselected = new[] { new IndexRange(oldSelectedIndex) };
                    }

                    if (_selectedIndex != -1)
                    {
                        selected = new[] { new IndexRange(_selectedIndex) };
                    }
                }

                if (deselected?.Count > 0 || selected?.Count > 0)
                {
                    var deselectedSource = operation.IsSourceUpdate ? null : ItemsView;
                    var e = new SelectionModelSelectionChangedEventArgs<T>(
                        SelectedIndexes<T>.Create(deselected),
                        SelectedIndexes<T>.Create(selected),
                        SelectedItems<T>.Create(deselected, deselectedSource),
                        SelectedItems<T>.Create(selected, ItemsView));
                    SelectionChanged?.Invoke(this, e);
                }
            }

            if (oldSelectedIndex != _selectedIndex)
            {
                RaisePropertyChanged(nameof(SelectedIndex));
            }

            if (oldAnchorIndex != _anchorIndex)
            {
                RaisePropertyChanged(nameof(AnchorIndex));
            }

            _operation = null;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public struct BatchUpdateOperation : IDisposable
        {
            private readonly SelectionModel<T> _owner;
            private bool _isDisposed;

            public BatchUpdateOperation(SelectionModel<T> owner)
            {
                _owner = owner;
                _isDisposed = false;
                owner.BeginBatchUpdate();
            }

            internal Operation Operation => _owner._operation!;

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _owner?.EndBatchUpdate();
                    _isDisposed = true;
                }
            }
        }

        internal class Operation
        {
            public Operation(SelectionModel<T> owner)
            {
                SingleSelect = owner.SingleSelect;
                AnchorIndex = owner.AnchorIndex;
                SelectedIndex = owner.SelectedIndex;
            }

            public int UpdateCount { get; set; }
            public bool IsSourceUpdate { get; set; }
            public bool SingleSelect { get; }
            public int AnchorIndex { get; set; }
            public int SelectedIndex { get; set; }
            public List<IndexRange>? SelectedRanges { get; set; }
            public List<IndexRange>? DeselectedRanges { get; set; }
        }
    }
}
