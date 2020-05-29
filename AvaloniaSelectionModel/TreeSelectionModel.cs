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
        private readonly TreeSelectionNode<T> _root = new TreeSelectionNode<T>(default);
        private TreeSelectedIndexes<T>? _selectedIndexes;
        private TreeSelectedItems<T>? _selectedItems;
        private State _state;

        public TreeSelectionModel()
        {
        }

        public TreeSelectionModel(IEnumerable<T>? source)
        {
            Source = source;
        }

        public IndexPath AnchorIndex { get; set; }
        
        public IndexPath SelectedIndex 
        {
            get => _state.SelectedIndex;
            set => _state.SelectedIndex = _root.SetSelectedIndex(value, 0) ? value : default;
        }

        [MaybeNull]
        public T SelectedItem => default;

        public IReadOnlyList<IndexPath> SelectedIndexes => _selectedIndexes ??= new TreeSelectedIndexes<T>(_root);
        public IReadOnlyList<T> SelectedItems => _selectedItems ??= new TreeSelectedItems<T>(_root);

        public bool SingleSelect { get; set; }

        public IEnumerable<T>? Source 
        {
            get => _root.Source;
            set => _root.Source = value;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<SelectionModelSelectionChangedEventArgs<T>>? SelectionChanged;

        private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
