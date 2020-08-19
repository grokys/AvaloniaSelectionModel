using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class TreeSelectionNode<T> : SelectionNodeBase<T>
    {
        private readonly TreeSelectionModel<T> _owner;
        private List<TreeSelectionNode<T>?>? _children;

        public TreeSelectionNode(TreeSelectionModel<T> owner)
        {
            _owner = owner;
            RangesEnabled = true;
        }

        public TreeSelectionNode(
            TreeSelectionModel<T> owner,
            TreeSelectionNode<T> parent,
            int index)
            : this(owner)
        {
            Path = parent.Path.CloneWithChildIndex(index);

            if (parent.ItemsView is object)
            {
                // HACK
                Source = _owner.ChildSelector.GetChildrenObservable(parent.ItemsView[index]).Single();
            }
        }

        public IndexPath Path { get; }

        public new void CommitSelect(IndexRange range) => base.CommitSelect(range);
        public new void CommitDeselect(IndexRange range) => base.CommitDeselect(range);

        public IndexPath CoerceIndex(IndexPath path, int depth)
        {
            if (depth == path.GetSize() - 1)
            {
                var leaf = path.GetLeaf()!.Value;
                return leaf < ItemsView?.Count ? path : default;
            }

            var index = path.GetAt(depth++);
            var child = GetChild(index, false);

            if (child is object)
            {
                return child.CoerceIndex(path, depth);
            }

            var items = (IEnumerable<T>?)ItemsView;

            while (items is object)
            {
                var count = items.Count();

                if (index < count)
                {
                    items = _owner.ChildSelector.GetChildren(items.ElementAt(index));

                    if (depth == path.GetSize() - 1)
                    {
                        return path;
                    }
                    else
                    {
                        index = path.GetAt(depth++);
                    }
                }
                else
                {
                    return default;
                }
            }

            return default;
        }

        public bool TryGetNode(
            IndexPath path,
            int depth,
            bool realize,
            [NotNullWhen(true)] out TreeSelectionNode<T>? result)
        {
            if (depth == path.GetSize())
            {
                result = this;
                return true;
            }

            var index = path.GetAt(depth);
            result = GetChild(index, realize);
            return result is object;
        }

        protected override void OnSourceCollectionChangeFinished()
        {
        }

        private protected override void OnIndexesChanged(int shiftIndex, int shiftDelta)
        {
            _owner.OnIndexesChanged(Path, shiftIndex, shiftDelta);
        }

        private protected override void OnItemsReset()
        {
            throw new NotImplementedException();
        }

        private protected override void OnSelectionChanged(IReadOnlyList<T> deselectedItems)
        {
            throw new NotImplementedException();
        }

        private TreeSelectionNode<T>? GetChild(int index, bool realize)
        {
            if (realize)
            {
                _children ??= new List<TreeSelectionNode<T>?>();

                if (ItemsView is null)
                {
                    if (_children.Count < index + 1)
                    {
                        Resize(_children, index + 1);
                    }

                    return _children[index] ??= new TreeSelectionNode<T>(_owner, this, index);
                }
                else
                {
                    if (_children.Count > ItemsView.Count)
                    {
                        throw new Exception("!!!");
                    }

                    Resize(_children, ItemsView.Count);
                    return _children[index] ??= new TreeSelectionNode<T>(_owner, this, index);
                }
            }
            else
            {
                if (_children?.Count > index)
                {
                    return _children[index];
                }
            }

            return null;
        }

        private static void Resize(List<TreeSelectionNode<T>?> list, int count)
        {
            int current = list.Count;

            if (count < current)
            {
                list.RemoveRange(count, current - count);
            }
            else if (count > current)
            {
                if (count > list.Capacity)
                {
                    list.Capacity = count;
                }

                list.AddRange(Enumerable.Repeat<TreeSelectionNode<T>?>(null, count - current));
            }
        }
    }
}
