using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class TreeSelectionNode<T> : SelectionModel<T>
    {
        private List<TreeSelectionNode<T>?>? _children;

        public TreeSelectionNode(IndexPath path)
        {
            IndexPath = path;
        }

        public IndexPath IndexPath { get; }

        public IReadOnlyList<TreeSelectionNode<T>> Children
        {
            get
            {
                return (IReadOnlyList<TreeSelectionNode<T>>?)_children ??
                    Array.Empty<TreeSelectionNode<T>>();
            }
        }

        public int GetSelectionCount()
        {
            var count = SelectedIndexes.Count;

            if (_children is object)
            {
                foreach (var child in _children)
                {
                    if (child is object)
                    {
                        count += child.GetSelectionCount();
                    }
                }
            }

            return count;
        }

        public bool SetSelectedIndex(IndexPath path, int depth)
        {
            if (path.GetSize() == 0)
            {
                return false;
            }

            if (depth == path.GetSize() - 1)
            {
                var leaf = path.GetLeaf()!.Value;
                SelectedIndex = leaf;
                return SelectedIndex == leaf;
            }
            else
            {
                var child = GetChild(path.GetAt(depth), true);
                return child?.SetSelectedIndex(path, depth + 1) ?? false;
            }
        }

        private TreeSelectionNode<T>? GetChild(int index, bool realize)
        {
            if (realize)
            {
                _children ??= new List<TreeSelectionNode<T>?>();
            }
            else
            {
                return _children?[index];
            }

            if (Items is null)
            {
                if (_children.Count <= index + 1)
                {
                    Resize(_children, index + 1);
                }

                _children[index] ??= new TreeSelectionNode<T>(IndexPath.CloneWithChildIndex(index));
                return _children[index];
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
