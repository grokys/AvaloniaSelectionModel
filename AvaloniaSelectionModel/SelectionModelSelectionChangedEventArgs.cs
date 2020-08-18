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
            IReadOnlyList<int>? deselectedIndices = null,
            IReadOnlyList<int>? selectedIndices = null,
            IReadOnlyList<T>? deselectedItems = null,
            IReadOnlyList<T>? selectedItems = null)
        {
            DeselectedIndexes = deselectedIndices ?? Array.Empty<int>();
            SelectedIndexes = selectedIndices ?? Array.Empty<int>();
            DeselectedItems = deselectedItems ?? Array.Empty<T>();
            SelectedItems= selectedItems ?? Array.Empty<T>();
        }

        /// <summary>
        /// Gets the indexes of the items that were removed from the selection.
        /// </summary>
        public IReadOnlyList<int> DeselectedIndexes { get; }

        /// <summary>
        /// Gets the indexes of the items that were added to the selection.
        /// </summary>
        public IReadOnlyList<int> SelectedIndexes { get; }

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
