using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class SelectedItems<T> : IReadOnlyList<T>
    {
        private readonly SelectionModel<T> _owner;

        public SelectedItems(SelectionModel<T> owner) => _owner = owner;

        [MaybeNull]
        public T this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException("The index was out of range.");
                }

                if (_owner.SingleSelect)
                {
                    return _owner.SelectedItem;
                }
                else if (_owner.Items is object)
                {
                    return _owner.Items[index];
                }
                else
                {
                    return default;
                }
            }
        }

        public int Count
        {
            get
            {
                if (_owner.SingleSelect)
                {
                    return _owner.SelectedIndex == -1 ? 0 : 1;
                }
                else
                {
                    return _owner.Ranges is object ? IndexRange.GetCount(_owner.Ranges) : 0;
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_owner.SingleSelect)
            {
                if (_owner.SelectedIndex >= 0)
                {
                    yield return _owner.SelectedItem;
                }
            }
            else
            {
                if (_owner.Ranges is null)
                {
                    throw new AvaloniaInternalException("Ranges was null but multiple selection is enabled.");
                }

                var items = _owner.Items;

                foreach (var range in _owner.Ranges)
                {
                    for (var i = range.Begin; i <= range.End; ++i)
                    {
                        yield return items != null ? items[i] : default;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
