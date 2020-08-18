using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class TreeSelectionNode<T> : SelectionNodeBase<T>
    {
        private readonly TreeSelectionModel<T> _owner;
        private List<TreeSelectionNode<T>?>? _children;

        public TreeSelectionNode(TreeSelectionModel<T> owner)
            : base(new NodeState())
        {
            _owner = owner;
        }

        public TreeSelectionNode(
            TreeSelectionModel<T> owner,
            TreeSelectionNode<T> parent,
            [AllowNull] T item)
            : this(owner)
        {
            _owner = owner;
        }

        public override IEnumerable<T>? Source 
        { 
            get => base.Source;
            set
            {
                base.Source = value;

                if (_children is null)
                {
                    return;
                }

                foreach (var child in _children)
                {

                }
            }
        }

        public bool TryGetNode(
            IndexPath path,
            int depth,
            bool realize,
            [NotNullWhen(true)] out TreeSelectionNode<T>? result)
        {
            if (depth == path.GetSize() - 1)
            {
                result = this;
                return true;
            }

            var index = path.GetAt(depth);

            if (realize)
            {
                _children ??= new List<TreeSelectionNode<T>?>();

                if (Items is null)
                {
                    if (_children.Count < index + 1)
                    {
                        Resize(_children, index + 1);
                    }

                    result = _children[index] ??= new TreeSelectionNode<T>(
                        _owner,
                        this,
                        default);
                    return true;
                }
                else
                {
                    if (Items.Count > index)
                    {
                        if (_children[index] is TreeSelectionNode<T> n)
                        {
                            result = n;
                            return true;
                        }
                    }
                }
            }
            else
            {
                if (_children?.Count > index)
                {
                    result = _children[index];
                    return result is object;
                }
            }

            result = null;
            return false;
        }

        protected override void OnIndexesChanged(int shiftIndex, int shiftDelta)
        {
            throw new NotImplementedException();
        }

        protected override void OnItemsReset()
        {
            throw new NotImplementedException();
        }

        private protected override void OnSelectionChanged(IReadOnlyList<T> deselectedItems)
        {
            throw new NotImplementedException();
        }

        private protected override void OnSelectionChanged(ItemsSourceView<T>? deselectedItems, ItemsSourceView<T>? selectedItems, List<IndexRange>? deselectedIndexes, List<IndexRange>? selectedIndexes)
        {
            throw new NotImplementedException();
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
