using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public class TreeSelectionModel<T> : INotifyPropertyChanged
    {
        private readonly TreeSelectionNode<T> _root;
        private IndexPath _anchorIndex;
        private IndexPath _selectedIndex;
        private Operation? _operation;
        private TreeSelectedIndexes<T>? _selectedIndexes;
        private TreeSelectedItems<T>? _selectedItems;

        public TreeSelectionModel(Expression<Func<T, IEnumerable<T>?>> childSelector)
        {
            _root = new TreeSelectionNode<T>(this);
            ChildSelector = new InpcTreeChildSelector<T>(childSelector);
        }

        public TreeSelectionModel(Expression<Func<T, IEnumerable<T>?>> childSelector, IEnumerable<T>? source)
            : this(childSelector)
        {
            Source = source;
        }

        public IEnumerable<T>? Source
        {
            get => _root.Source as IEnumerable<T>;
            set
            {
                if (_root.Source != value)
                {
                    _root.Source = value;
                }
            }
        }

        public bool SingleSelect { get; set; } = true;

        public IndexPath SelectedIndex 
        {
            get => _selectedIndex;
            set => SetSelectedIndex(value);
        }

        public IReadOnlyList<IndexPath> SelectedIndexes => _selectedIndexes ??= new TreeSelectedIndexes<T>(this);

        [MaybeNull]
        public T SelectedItem => GetItemAt(_selectedIndex);

        public IReadOnlyList<T> SelectedItems => _selectedItems ??= new TreeSelectedItems<T>(this);

        public IndexPath AnchorIndex 
        {
            get => _anchorIndex;
            set => _anchorIndex = value;
        }

        internal ITreeChildSelector<T> ChildSelector { get; }

        public event EventHandler<TreeSelectionModelSelectionChangedEventArgs<T>>? SelectionChanged;
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
                ////if (!IsSourceCollectionChanging)
                {
                    CommitOperation(_operation);
                }
            }
        }
        
        internal void OnIndexesChanged(IndexPath parentPath, int shiftIndex, int shiftDelta)
        {
            if (ShiftIndex(parentPath, shiftIndex, shiftDelta, ref _selectedIndex))
            {
                RaisePropertyChanged(nameof(SelectedIndex));
            }

            if (ShiftIndex(parentPath, shiftIndex, shiftDelta, ref _anchorIndex))
            {
                RaisePropertyChanged(nameof(AnchorIndex));
            }
        }

        private bool ShiftIndex(IndexPath parentPath, int shiftIndex, int shiftDelta, ref IndexPath path)
        {
            if (parentPath.IsAncestorOf(path) && path.GetAt(parentPath.GetSize()) >= shiftIndex)
            {
                var indexes = path.ToArray();
                ++indexes[parentPath.GetSize()];
                path = new IndexPath(indexes);
                return true;
            }

            return false;
        }

        private void SetSelectedIndex(IndexPath value, bool updateAnchor = true)
        {
            if (_selectedIndex == value)
            {
                return;
            }

            using var update = BatchUpdate();
            var o = update.Operation;
            var index = CoerceIndex(value);

            o.SelectedIndex = index;

            if (updateAnchor)
            {
                update.Operation.AnchorIndex = index;
            }
        }

        [return: MaybeNull]
        private T GetItemAt(in IndexPath path)
        {
            if (Source is null || path == default)
            {
                return default;
            }

            if (path != default)
            {
                var node = GetNode(path.GetParent());

                if (node is object)
                {
                    return node.ItemsView![path.GetLeaf()!.Value];
                }
            }

            throw new ArgumentOutOfRangeException();
        }

        private TreeSelectionNode<T>? GetNode(in IndexPath path)
        {
            if (path == default)
            {
                return _root;
            }

            if (_root.TryGetNode(path, 0, false, out var result))
            {
                return result;
            }

            return null;
        }

        private TreeSelectionNode<T> RealizeNode(in IndexPath path)
        {
            if (path == default)
            {
                return _root;
            }

            if (_root.TryGetNode(path, 0, true, out var result))
            {
                return result;
            }

            throw new ArgumentOutOfRangeException();
        }

        private IndexPath CoerceIndex(IndexPath path)
        {
            if (Source is null)
            {
                return path;
            }

            return _root.CoerceIndex(path, 0);
        }

        private void CommitSelect(IndexPath path)
        {
            var node = RealizeNode(path.GetParent());
            node.CommitSelect(new IndexRange(path.GetLeaf()!.Value));
        }

        private void CommitDeselect(IndexPath path)
        {
            var node = RealizeNode(path.GetParent());
            node.CommitDeselect(new IndexRange(path.GetLeaf()!.Value));
        }

        private void CommitOperation(Operation operation)
        {
            var oldAnchorIndex = _anchorIndex;
            var oldSelectedIndex = _selectedIndex;

            _selectedIndex = operation.SelectedIndex;
            _anchorIndex = operation.AnchorIndex;

            if (_selectedIndex != oldSelectedIndex)
            {
                if (oldSelectedIndex != default)
                {
                    CommitDeselect(oldSelectedIndex);
                }

                if (_selectedIndex != default)
                {
                    CommitSelect(_selectedIndex);
                }
            }

            if (SelectionChanged is object)
            {
                IReadOnlyList<IndexPath>? deselected = null;////operation.DeselectedRanges;
                IReadOnlyList<IndexPath>? selected = null;////operation.SelectedRanges;

                if (SingleSelect && oldSelectedIndex != _selectedIndex)
                {
                    if (oldSelectedIndex != default)
                    {
                        deselected = new[] { oldSelectedIndex };
                    }

                    if (_selectedIndex != default)
                    {
                        selected = new[] { _selectedIndex };
                    }
                }

                if (deselected?.Count > 0 || selected?.Count > 0)
                {
                    var e = new TreeSelectionModelSelectionChangedEventArgs<T>(
                        deselected,
                        selected,
                        null,
                        null);
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
            private readonly TreeSelectionModel<T> _owner;
            private bool _isDisposed;

            public BatchUpdateOperation(TreeSelectionModel<T> owner)
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
            public Operation(TreeSelectionModel<T> owner)
            {
                AnchorIndex = owner.AnchorIndex;
                SelectedIndex = owner.SelectedIndex;
            }

            public int UpdateCount { get; set; }
            public bool IsSourceUpdate { get; set; }
            public IndexPath AnchorIndex { get; set; }
            public IndexPath SelectedIndex { get; set; }
        }
    }
}
