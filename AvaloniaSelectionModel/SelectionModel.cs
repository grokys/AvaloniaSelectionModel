using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using AvaloniaSelectionModel;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public class SelectionModel<T> : SelectionNodeBase<T>, INotifyPropertyChanged
    {
        private SelectedIndexes<T>? _selectedIndexes;
        private SelectedItems<T>? _selectedItems;
        private int _updateCount;

        public SelectionModel(IEnumerable<T>? source = null)
            : base(new ModelState())
        {
            State = (ModelState)base.State;
            Source = source;
        }

        public int AnchorIndex
        {
            get => State.AnchorIndex;
            set
            {
                value = CoerceIndex(value);

                if (State.AnchorIndex != value)
                {
                    State.AnchorIndex = value;

                    if (_updateCount == 0)
                    {
                        OnPropertyChanged();
                    }
                }
            }
        }

        public int SelectedIndex
        {
            get => State.SelectedIndex;
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
            get => (State.SelectedIndex >= 0 && Items?.Count > State.SelectedIndex) ? Items[State.SelectedIndex] : default;
        }

        public IReadOnlyList<int> SelectedIndexes => _selectedIndexes ??= new SelectedIndexes<T>(this);
        public IReadOnlyList<T> SelectedItems => _selectedItems ??= new SelectedItems<T>(this);

        public bool SingleSelect 
        {
            get => !EnableRanges;
            set
            {
                if (SingleSelect != value)
                {
                    if (_updateCount != 0)
                    {
                        throw new InvalidOperationException("Cannot change selection mode while update is in progress.");
                    }

                    EnableRanges = !value;

                    if (EnableRanges)
                    {
                        SelectImpl(SelectedIndex);
                    }

                    OnPropertyChanged();
                }
            }
        }

        private protected new ModelState State { get; }

        public event EventHandler<SelectionModelIndexesChangedEventArgs>? IndexesChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<SelectionModelSelectionChangedEventArgs<T>>? SelectionChanged;
        public event EventHandler? SelectionReset;

        public void ClearSelection()
        {
            using var o = new Operation(this);
            ClearSelectionImpl();
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
                State.SelectedIndex = start;
            }

            if (AnchorIndex == -1)
            {
                State.AnchorIndex = start;
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
            DeselectRangeImpl(start, end);
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
                if (SelectedIndex != index)
                {
                    State.SelectedIndex = index;

                    if (reset)
                    {
                        base.ClearSelectionImpl();
                    }

                    base.SelectImpl(index);

                    if (setAnchor)
                    {
                        State.AnchorIndex = index;
                    }
                }
            }
            else
            {
                base.SelectImpl(index);

                if (SelectedIndex == -1)
                {
                    State.SelectedIndex = index;
                }

                if (setAnchor)
                {
                    State.AnchorIndex = index;
                }
            }
        }

        protected override void ClearSelectionImpl()
        {
            State.SelectedIndex = State.AnchorIndex = -1;
            base.ClearSelectionImpl();
        }

        protected override void DeselectImpl(int index)
        {
            index = CoerceIndex(index);

            if (SelectedIndex == index)
            {
                State.SelectedIndex = GetFirstSelectedIndex();
            }

            base.DeselectImpl(index);
        }

        protected override void DeselectRangeImpl(int start, int end)
        {
            base.DeselectRangeImpl(start, end);

            if (SelectedIndex >= start && SelectedIndex <= end)
            {
                State.SelectedIndex = GetFirstSelectedIndex();
            }
        }

        protected override void OnIndexesChanged(int shiftIndex, int shiftDelta)
        {
            IndexesChanged?.Invoke(this, new SelectionModelIndexesChangedEventArgs(shiftIndex, shiftDelta));
        }

        protected override CollectionChangeState OnItemsAddedImpl(int index, IList items)
        {
            var count = items.Count;
            var shifted = SelectedIndex >= index;
            var shiftCount = shifted ? count : 0;

            State.SelectedIndex += shiftCount;
            State.AnchorIndex += shiftCount;

            var baseResult = base.OnItemsAddedImpl(index, items);
            shifted |= baseResult.ShiftDelta != 0;

            return new CollectionChangeState
            {
                ShiftIndex = index,
                ShiftDelta = shifted ? count : 0,
            };
        }

        protected override CollectionChangeState OnItemsRemovedImpl(int index, IList items)
        {
            var count = items.Count;
            var removedRange = new IndexRange(index, index + count - 1);
            var shifted = false;
            List<T>? removed;

            var baseResult = base.OnItemsRemovedImpl(index, items);
            shifted |= baseResult.ShiftDelta != 0;
            removed = baseResult.RemovedItems;

            if (removedRange.Contains(SelectedIndex))
            {
                if (SingleSelect)
                {
                    removed = new List<T> { (T)items[SelectedIndex - index] };
                }

                State.SelectedIndex = GetFirstSelectedIndex();
            }
            else if (SelectedIndex >= index)
            {
                State.SelectedIndex -= count;
                shifted = true;
            }

            if (removedRange.Contains(AnchorIndex))
            {
                State.AnchorIndex = GetFirstSelectedIndex();
            }
            else if (AnchorIndex >= index)
            {
                State.AnchorIndex -= count;
                shifted = true;
            }

            return new CollectionChangeState
            {
                ShiftIndex = index,
                ShiftDelta = shifted ? -count : 0,
                RemovedItems = removed,
            };
        }

        protected override void OnItemsReset()
        {
            ClearSelectionImpl();
            SelectionReset?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(this, new SelectionModelSelectionChangedEventArgs<T>());
        }

        protected override void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var startState = (ModelState)State.Clone();
            base.OnSourceCollectionChanged(sender, e);
            RaisePropertyChangedEvents(startState, State);
        }

        private protected override void OnSelectionChanged(IReadOnlyList<T> deselectedItems)
        {
            SelectionChanged?.Invoke(
                this,
                new SelectionModelSelectionChangedEventArgs<T>(deselectedItems: deselectedItems));
        }

        private protected override void OnSelectionChanged(
            ItemsSourceView<T>? deselectedItems = null,
            ItemsSourceView<T>? selectedItems = null,
            List<IndexRange>? deselectedIndexes = null,
            List<IndexRange>? selectedIndexes= null)
        {
            SelectionChanged?.Invoke(
                this,
                new SelectionModelSelectionChangedEventArgs<T>(
                    SelectedIndexes<T>.Create(deselectedIndexes),
                    SelectedIndexes<T>.Create(selectedIndexes),
                    SelectedItems<T>.Create(deselectedIndexes, deselectedItems),
                    SelectedItems<T>.Create(selectedIndexes, selectedItems)));
        }

        private protected override void RaiseEvents(NodeState beforeBase, NodeState afterBase)
        {
            var before = (ModelState)beforeBase;
            var after = (ModelState)afterBase;

            if (SelectionChanged is object)
            {
                if (SingleSelect)
                {
                    if (before.SelectedIndex != after.SelectedIndex)
                    {
                        OnSelectionChanged(
                            before.Items,
                            after.Items,
                            before.SelectedIndex >= 0 ?
                                new List<IndexRange> { new IndexRange(before.SelectedIndex) } :
                                null,
                            after.SelectedIndex >= 0 ?
                                new List<IndexRange> { new IndexRange(after.SelectedIndex) } :
                                null);
                    }
                }
                else
                {
                    base.RaiseEvents(beforeBase, afterBase);
                }
            }

            RaisePropertyChangedEvents(before, after);
        }

        protected override void TrimInvalidSelectionsImpl()
        {
            if (SelectedIndex >= Items!.Count)
            {
                State.SelectedIndex = GetFirstSelectedIndex();
            }

            if (AnchorIndex >= Items!.Count)
            {
                State.AnchorIndex = GetFirstSelectedIndex();
            }

            base.TrimInvalidSelectionsImpl();
        }

        private int GetFirstSelectedIndex() => State.Ranges?.Count > 0 ? State.Ranges[0].Begin : -1;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaisePropertyChangedEvents(ModelState before, ModelState after)
        {
            if (before.SelectedIndex != after.SelectedIndex)
            {
                OnPropertyChanged(nameof(SelectedIndex));
            }

            if (before.AnchorIndex != after.AnchorIndex)
            {
                OnPropertyChanged(nameof(AnchorIndex));
            }
        }

        private protected class ModelState : NodeState
        {
            public ModelState()
            {
                AnchorIndex = SelectedIndex = -1; ;
            }

            public ModelState(ModelState source)
                : base(source)
            {
                AnchorIndex = source.AnchorIndex;
                SelectedIndex = source.SelectedIndex;
            }

            public int AnchorIndex;
            public int SelectedIndex;

            public override NodeState Clone()
            {
                return new ModelState(this);
            }
        }
    }
}
