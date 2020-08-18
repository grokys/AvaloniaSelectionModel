using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public interface ISelectionModel : INotifyPropertyChanged
    {
        IEnumerable? Source { get; set; }
        bool SingleSelect { get; set; }
        int SelectedIndex { get; set; }
        IReadOnlyList<int> SelectedIndexes { get; }
        object? SelectedItem { get; }
        IReadOnlyList<object?> SelectedItems { get; }
        int AnchorIndex { get; set; }

        public event EventHandler<SelectionModelIndexesChangedEventArgs>? IndexesChanged;
        public event EventHandler<SelectionModelSelectionChangedEventArgs>? SelectionChanged;
        public event EventHandler? SelectionReset;

        public void BeginBatchUpdate();
        public void EndBatchUpdate();
        bool IsSelected(int index);
        void Select(int index);
        void Deselect(int index);
        void SelectRange(int start, int end);
        void DeselectRange(int start, int end);
        void Clear();
    }
}
