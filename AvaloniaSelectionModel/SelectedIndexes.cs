using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class SelectedIndexes<T> : IReadOnlyList<int>
    {
        private readonly SelectionModel<T> _owner;

        public SelectedIndexes(SelectionModel<T> owner) => _owner = owner;

        public int this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException("The index was out of range.");
                }

                if (_owner.SingleSelect)
                {
                    return _owner.SelectedIndex;
                }
                else
                {
                    return IndexRange.GetAt(_owner.Ranges!, index);
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

        public IEnumerator<int> GetEnumerator()
        {
            IEnumerator<int> SingleSelect()
            {
                if (_owner.SelectedIndex >= 0)
                {
                    yield return _owner.SelectedIndex;
                }
            }

            if(_owner.SingleSelect)
            {
                return SingleSelect();
            }
            else if (_owner.Ranges is object)
            {
                return IndexRange.EnumerateIndices(_owner.Ranges).GetEnumerator();
            }
            else
            {
                return Enumerable.Empty<int>().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
