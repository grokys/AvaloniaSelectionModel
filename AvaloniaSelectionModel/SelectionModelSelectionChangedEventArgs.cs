// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls
{
    public class SelectionModelSelectionChangedEventArgs<T> : EventArgs
    {
        public SelectionModelSelectionChangedEventArgs(
            IReadOnlyList<int>? deselectedIndices,
            IReadOnlyList<int>? selectedIndices,
            IReadOnlyList<T>? deselectedItems,
            IReadOnlyList<T>? selectedItems)
        {
            DeselectedIndices = deselectedIndices ?? Array.Empty<int>();
            SelectedIndices = selectedIndices ?? Array.Empty<int>();
            DeselectedItems = deselectedItems ?? Array.Empty<T>();
            SelectedItems= selectedItems ?? Array.Empty<T>();
        }

        /// <summary>
        /// Gets the indices of the items that were removed from the selection.
        /// </summary>
        public IReadOnlyList<int> DeselectedIndices { get; }

        /// <summary>
        /// Gets the indices of the items that were added to the selection.
        /// </summary>
        public IReadOnlyList<int> SelectedIndices { get; }

        /// <summary>
        /// Gets the items that were removed from the selection.
        /// </summary>
        public IReadOnlyList<T> DeselectedItems { get; }

        /// <summary>
        /// Gets the items that were added to the selection.
        /// </summary>
        public IReadOnlyList<T> SelectedItems { get; }
    }
}
