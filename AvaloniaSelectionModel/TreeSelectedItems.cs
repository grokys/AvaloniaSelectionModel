using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class TreeSelectedItems<T> : IReadOnlyList<T>
    {
        private readonly TreeSelectionNode<T> _root;
        private int? _count;

        public TreeSelectedItems(TreeSelectionNode<T> root) => _root = root;

        public int Count => _count ??= _root.GetSelectionCount();

        public T this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException("The index was out of range.");
                }

                throw new NotImplementedException();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var i in GetEnumerator(_root))
            {
                yield return i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<T> GetEnumerator(TreeSelectionNode<T> node)
        {
            foreach (var i in node.SelectedItems)
            {
                yield return i;
            }

            foreach (var child in node.Children)
            {
                if (child is object)
                {
                    foreach (var i in GetEnumerator(child))
                    {
                        yield return i;
                    }
                }
            }
        }
    }
}
