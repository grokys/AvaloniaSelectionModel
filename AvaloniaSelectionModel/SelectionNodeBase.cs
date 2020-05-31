using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public abstract class SelectionNodeBase<T>
    {
        private IEnumerable<T>? _source;
        private bool _enableRanges;
        private int _updateCount;

        private protected SelectionNodeBase(StateBase state)
        {
            State = state;
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

                    if (State.Items is object)
                    {
                        State.Items.CollectionChanged -= OnSourceCollectionChanged;
                    }

                    using (new Operation(this))
                    {
                        _source = value;
                        State.Items?.Dispose();
                        State.Items = ItemsSourceView<T>.Create(value);

                        if (State.Items is object)
                        {
                            State.Items.CollectionChanged += OnSourceCollectionChanged;
                        }

                        if (Items != null)
                        {
                            TrimInvalidSelectionsImpl();
                        }
                    }
                }
            }
        }

        protected bool EnableRanges
        {
            get => _enableRanges;
            set
            {
                if (_updateCount != 0)
                {
                    throw new InvalidOperationException("Cannot change EnableRanges while update is in progress.");
                }

                if (_enableRanges != value)
                {
                    _enableRanges = value;

                    if (!value)
                    {
                        State.Ranges = null;
                    }
                }
            }
        }

        internal ItemsSourceView<T>? Items => State.Items;

        internal List<IndexRange>? Ranges => _enableRanges ? State.Ranges ??= new List<IndexRange>() : null;

        private protected StateBase State { get; }

        private protected StateBase? StartState { get; private set; }

        public void BeginBatchUpdate()
        {
            if (_updateCount++ == 0)
            {
                StartState = State.Clone();
            }
        }

        public void EndBatchUpdate()
        {
            if (--_updateCount == 0)
            {
                var startState = StartState!;
                StartState = null;
                RaiseEvents(startState, State);
            }
        }

        protected virtual void ClearSelectionImpl()
        {
            Ranges?.Clear();
        }

        protected virtual void SelectImpl(int index)
        {
            index = CoerceIndex(index);

            if (index == -1)
            {
                return;
            }

            if (index >= 0 && Ranges is object)
            {
                IndexRange.Add(Ranges, new IndexRange(index));
            }
        }

        protected virtual void SelectRangeImpl(int start, int end)
        {
            if (Ranges is null)
            {
                throw new InvalidOperationException("Cannot select range with single selection.");
            }

            var max = Items is object ? Items.Count - 1 : int.MaxValue;

            if (start > max)
            {
                return;
            }

            start = Math.Max(start, 0);
            end = Math.Min(end, max);
            IndexRange.Add(Ranges, new IndexRange(start, end));
        }

        protected virtual void DeselectImpl(int index)
        {
            index = CoerceIndex(index);

            if (index > 0 && Ranges is object)
            {
                IndexRange.Remove(Ranges, new IndexRange(index));
            }
        }

        protected virtual void DeselectRangeImpl(int start, int end)
        {
            if (Ranges is object)
            {
                start = Math.Max(start, 0);
                IndexRange.Remove(Ranges, new IndexRange(start, end));
            }
        }

        protected abstract void OnIndexesChanged(int shiftIndex, int shiftDelta);

        protected virtual CollectionChangeState OnItemsAddedImpl(int index, IList items)
        {
            var count = items.Count;
            var shifted = false;

            if (Ranges is object)
            {
                List<IndexRange>? toAdd = null;

                for (var i = 0; i < Ranges!.Count; ++i)
                {
                    var range = Ranges[i];

                    // The range is after the inserted items, need to shift the range right
                    if (range.End >= index)
                    {
                        int begin = range.Begin;

                        // If the index left of newIndex is inside the range,
                        // Split the range and remember the left piece to add later
                        if (range.Contains(index - 1))
                        {
                            range.Split(index - 1, out var before, out _);
                            (toAdd ??= new List<IndexRange>()).Add(before);
                            begin = index;
                        }

                        // Shift the range to the right
                        Ranges[i] = new IndexRange(begin + count, range.End + count);
                        shifted = true;
                    }
                }

                if (toAdd is object)
                {
                    foreach (var range in toAdd)
                    {
                        IndexRange.Add(Ranges, range);
                    }
                }
            }

            return new CollectionChangeState
            {
                ShiftIndex = index,
                ShiftDelta = shifted ? count : 0,
            };
        }

        protected virtual CollectionChangeState OnItemsRemovedImpl(int index, IList items)
        {
            var count = items.Count;
            var removedRange = new IndexRange(index, index + count - 1);
            bool shifted = false;
            List<T>? removed = null;

            if (Ranges is object)
            {
                var deselected = new List<IndexRange>();

                if (IndexRange.Remove(Ranges!, removedRange, deselected) > 0)
                {
                    removed = new List<T>();

                    foreach (var range in deselected)
                    {
                        for (var i = range.Begin; i <= range.End; ++i)
                        {
                            removed.Add((T)items[i - index]);
                        }
                    }
                }

                for (var i = 0; i < Ranges!.Count; ++i)
                {
                    var existing = Ranges[i];

                    if (existing.End > removedRange.Begin)
                    {
                        Ranges[i] = new IndexRange(existing.Begin - count, existing.End - count);
                        shifted = true;
                    }
                }
            }

            return new CollectionChangeState
            {
                ShiftIndex = index,
                ShiftDelta = shifted ? -count : 0,
                RemovedItems = removed,
            };
        }

        protected abstract void OnItemsReset();

        protected virtual void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var shiftDelta = 0;
            var shiftIndex = -1;
            var startState = State.Clone();
            List<T>? removed = null;

            if (_updateCount != 0)
            {
                throw new InvalidOperationException("Source collection was modified during selection update.");
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var change = OnItemsAddedImpl(e.NewStartingIndex, e.NewItems);
                        shiftIndex = change.ShiftIndex;
                        shiftDelta = change.ShiftDelta;
                        break;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        var change = OnItemsRemovedImpl(e.OldStartingIndex, e.OldItems);
                        shiftIndex = change.ShiftIndex;
                        shiftDelta = change.ShiftDelta;
                        removed = change.RemovedItems;
                        break;
                    }
                case NotifyCollectionChangedAction.Replace:
                    {
                        var removeChange = OnItemsRemovedImpl(e.OldStartingIndex, e.OldItems);
                        var addChange = OnItemsAddedImpl(e.NewStartingIndex, e.NewItems);
                        shiftIndex = removeChange.ShiftIndex;
                        shiftDelta = removeChange.ShiftDelta + addChange.ShiftDelta;
                        removed = removeChange.RemovedItems;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    OnItemsReset();
                    break;
            }

            if (shiftDelta != 0)
            {
                OnIndexesChanged(shiftIndex, shiftDelta);
            }

            if (removed is object)
            {
                OnSelectionChanged(removed);
            }
        }

        private protected abstract void OnSelectionChanged(IReadOnlyList<T> deselectedItems);

        private protected abstract void OnSelectionChanged(
            ItemsSourceView<T>? deselectedItems,
            ItemsSourceView<T>? selectedItems,
            List<IndexRange>? deselectedIndexes,
            List<IndexRange>? selectedIndexes);

        private protected virtual void RaiseEvents(StateBase before, StateBase after)
        {
            List<IndexRange>? selected = null;
            List<IndexRange>? deselected = null;

            if (before.Ranges is object && after.Ranges is object)
            {
                IndexRange.Diff(before.Ranges, after.Ranges, out deselected, out selected);
            }
            else if (after.Ranges is object)
            {
                selected = after.Ranges;
            }
            else if (before.Ranges is object)
            {
                deselected = before.Ranges;
            }

            if (selected?.Count > 0 || deselected?.Count > 0)
            {
                OnSelectionChanged(before.Items, after.Items, deselected, selected);
            }
        }

        protected virtual void TrimInvalidSelectionsImpl()
        {
            if (Items is null)
            {
                throw new AvaloniaInternalException("Cannot trim invalid selections on null source.");
            }

            if (Ranges is object)
            {
                var validRange = new IndexRange(0, Items.Count - 1);
                IndexRange.Intersect(Ranges, validRange);
            }
        }

        protected int CoerceIndex(int index)
        {
            index = Math.Max(index, -1);

            if (Items is object && index >= Items.Count)
            {
                index = -1;
            }

            return index;
        }

        protected struct Operation : IDisposable
        {
            private readonly SelectionNodeBase<T> _owner;

            public Operation(SelectionNodeBase<T> owner)
            {
                _owner = owner;
                _owner.BeginBatchUpdate();
            }

            public void Dispose()
            {
                _owner.EndBatchUpdate();
            }
        }

        protected struct CollectionChangeState
        {
            public int ShiftIndex;
            public int ShiftDelta;
            public List<T>? RemovedItems;
        }

        private protected abstract class StateBase
        {
            public StateBase()
            {
            }

            public StateBase(StateBase s)
            {
                Items = s.Items;
                Ranges = s.Ranges?.ToList();
            }

            public ItemsSourceView<T>? Items;
            public List<IndexRange>? Ranges;

            public abstract StateBase Clone();
        }
    }
}
