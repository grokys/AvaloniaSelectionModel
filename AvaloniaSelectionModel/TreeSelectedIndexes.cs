using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class TreeSelectedIndexes<T> : IReadOnlyList<IndexPath>
    {
        private readonly TreeSelectionModel<T> _model;

        public TreeSelectedIndexes(TreeSelectionModel<T> model) => _model = model;

        public int Count
        {
            get
            {
                if (_model.SingleSelect)
                {
                    return _model.SelectedIndex.GetSize() > 0 ? 1 : 0;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public IndexPath this[int index]
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

        public IEnumerator<IndexPath> GetEnumerator()
        {
            if (_model.SingleSelect)
            {
                if (_model.SelectedIndex.GetSize() > 0)
                {
                    yield return _model.SelectedIndex;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
