using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public class TreeSelectionModel<T> : INotifyPropertyChanged
    {
        private readonly TreeSelectionNode<T> _root;
        private readonly ChildSelector _childSelector;
        private TreeSelectedIndexes<T>? _selectedIndexes;
        private TreeSelectedItems<T>? _selectedItems;
        private State _state;
        private State _startState;
        private int _updateCount;

        public TreeSelectionModel(ChildSelector childSelector, IEnumerable<T>? source = null)
        {
            _root = new TreeSelectionNode<T>(this);
            _childSelector = childSelector ?? throw new ArgumentNullException(nameof(childSelector));
            Source = source;
        }

        public delegate IEnumerable<T>? ChildSelector(T item);

        public IndexPath AnchorIndex { get; set; }
        
        public IndexPath SelectedIndex 
        {
            get => _state.SelectedIndex;
            set
            {
                if (SingleSelect)
                {
                    value = CoerceIndex(value);

                    using (new Operation(this))
                    {
                        _state.SelectedIndex = value;
                    }
                }
            }
        }

        [MaybeNull]
        public T SelectedItem => GetItem(SelectedIndex, false);
        
        public IReadOnlyList<IndexPath> SelectedIndexes => _selectedIndexes ??= new TreeSelectedIndexes<T>(this);
        public IReadOnlyList<T> SelectedItems => _selectedItems ??= new TreeSelectedItems<T>(this);

        public bool SingleSelect { get; set; }

        public IEnumerable<T>? Source 
        {
            get => _root.Source;
            set 
            {
                _root.Source = value;

                using (new Operation(this))
                {
                    _state.SelectedIndex = CoerceIndex(SelectedIndex);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<TreeSelectionModelSelectionChangedEventArgs<T>>? SelectionChanged;

        public void BeginBatchUpdate()
        {
            if (_updateCount++ == 0)
            {
                _startState = new State(_state);
            }
        }

        public void EndBatchUpdate()
        {
            if (--_updateCount == 0)
            {
                RaiseEvents(_startState, _state);
            }
        }

        private IndexPath CoerceIndex(in IndexPath path)
        {
            if (path == default)
            {
                return path;
            }

            if (_root.TryGetNode(path.GetParent(), 0, true, out var parent))
            {
                if (parent.Items is null)
                {
                    return path;
                }
                else
                {
                    return parent.Items.Count > path.GetLeaf() ? path : default;
                }
            }

            return default;
        }

        [return: MaybeNull]
        private T GetItem(in IndexPath path, bool realize)
        {
            if (_root.TryGetNode(path.GetParent(), 0, realize, out var parent))
            {
                if (parent.Items == null)
                {
                    return default;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return default;
        }

        private IReadOnlyList<T> GetItemAsList(IndexPath path)
        {
            //if (_root.TryGetItem(path, 0, false, out var value))
            //{
            //    return new[] { value };
            //}

            return Array.Empty<T>();
        }

        private void RaiseEvents(in State before, in State after)
        {
            if (SelectionChanged is object)
            {
                if (SingleSelect)
                {
                    if (before.SelectedIndex != after.SelectedIndex)
                    {
                        SelectionChanged?.Invoke(
                            this,
                            new TreeSelectionModelSelectionChangedEventArgs<T>(
                                ToList(before.SelectedIndex),
                                ToList(after.SelectedIndex),
                                GetItemAsList(before.SelectedIndex),
                                GetItemAsList(after.SelectedIndex)));
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            RaisePropertyChangedEvents(before, after);
        }

        private void RaisePropertyChangedEvents(in State before, in State after)
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

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static IReadOnlyList<IndexPath> ToList(IndexPath path)
        {
            return path.GetSize() > 0 ? new[] { path } : Array.Empty<IndexPath>();
        }

        private struct Operation : IDisposable
        {
            private readonly TreeSelectionModel<T> _owner;

            public Operation(TreeSelectionModel<T> owner)
            {
                _owner = owner;
                _owner.BeginBatchUpdate();
            }

            public void Dispose()
            {
                _owner.EndBatchUpdate();
            }
        }

        private struct State
        {
            public State(State s)
            {
                AnchorIndex = s.AnchorIndex;
                SelectedIndex = s.SelectedIndex;
            }

            public IndexPath AnchorIndex;
            public IndexPath SelectedIndex;
        }
    }
}
