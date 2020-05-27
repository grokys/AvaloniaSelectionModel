using System;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls.Selection;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Selection
{
    public class SelectionModelTests_Single
    {
        public class No_Source
        {
            [Fact]
            public void Can_Select_Item_Before_Source_Assigned()
            {
                var target = CreateTarget(false);
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 5 }, e.SelectedIndices);
                    Assert.Equal(new string?[] { null }, e.SelectedItems);
                    ++raised;
                };

                target.SelectedIndex = 5;

                Assert.Equal(5, target.SelectedIndex);
                Assert.Null(target.SelectedItem);
                Assert.Equal(new string?[] { null }, target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Initializing_Source_Retains_Valid_Selection()
            {
                var target = CreateTarget(false);
                var raised = 0;

                target.SelectedIndex = 1;

                target.SelectionChanged += (s, e) => ++raised;

                target.Source = new[] { "foo", "bar", "baz" };

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new string[] { "bar" }, target.SelectedItems);
                Assert.Equal(0, raised);
            }

            [Fact]
            public void Initializing_Source_Removes_Invalid_Selection()
            {
                var target = CreateTarget(false);
                var raised = 0;

                target.SelectedIndex = 5;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 5 }, e.DeselectedIndices);
                    Assert.Equal(new string?[] { null }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.Source = new[] { "foo", "bar", "baz" };

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(1, raised);
            }
        }

        public class SelectedIndex
        {
            [Fact]
            public void SelectedIndex_Larger_Than_Source_Clears_Selection()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 1;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1 }, e.DeselectedIndices);
                    Assert.Equal(new string[] { "bar" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.SelectedIndex = 5;

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Negative_SelectedIndex_Is_Coerced_To_Minus_1()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) => ++raised;

                target.SelectedIndex = -5;

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(0, raised);
            }

            [Fact]
            public void Setting_SelectedIndex_Clears_Old_Selection()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 0 }, e.DeselectedIndices);
                    Assert.Equal(new string[] { "foo" }, e.DeselectedItems);
                    Assert.Equal(new[] { 1 }, e.SelectedIndices);
                    Assert.Equal(new string[] { "bar" }, e.SelectedItems);
                    ++raised;
                };

                target.SelectedIndex = 1;

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new string[] { "bar" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }
        }

        public class Select
        {
            [Fact]
            public void Select_Clears_Old_Selection()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 0 }, e.DeselectedIndices);
                    Assert.Equal(new string[] { "foo" }, e.DeselectedItems);
                    Assert.Equal(new[] { 1 }, e.SelectedIndices);
                    Assert.Equal(new string[] { "bar" }, e.SelectedItems);
                    ++raised;
                };

                target.Select(1);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new string[] { "bar" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Select_With_Invalid_Index_Does_Nothing()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;

                target.PropertyChanged += (s, e) => ++raised;
                target.SelectionChanged += (s, e) => ++raised;

                target.Select(5);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0 }, target.SelectedIndexes);
                Assert.Equal("foo", target.SelectedItem);
                Assert.Equal(new[] { "foo" }, target.SelectedItems);
                Assert.Equal(0, raised);
            }

            [Fact]
            public void Selecting_Already_Selected_Item_Doesnt_Raise_SelectionChanged()
            {
                var target = CreateTarget();
                var raised = 0;

                target.Select(2);
                target.SelectionChanged += (s, e) => ++raised;
                target.Select(2);

                Assert.Equal(0, raised);
            }
        }

        public class SelectRange
        {
            [Fact]
            public void SelectRange_Throws()
            {
                var target = CreateTarget();

                Assert.Throws<InvalidOperationException>(() => target.SelectRange(0, 10));
            }
        }

        public class Deselect
        {
            [Fact]
            public void Deselect_Clears_Current_Selection()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 0 }, e.DeselectedIndices);
                    Assert.Equal(new string[] { "foo" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.Deselect(0);

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Deselect_Does_Nothing_For_Nonselected_Item()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 1;
                target.SelectionChanged += (s, e) => ++raised;
                target.Deselect(0);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new string[] { "bar" }, target.SelectedItems);
                Assert.Equal(0, raised);
            }
        }

        public class DeselectRange
        {
            [Fact]
            public void DeselectRange_Clears_Current_Selection_For_Intersecting_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 0 }, e.DeselectedIndices);
                    Assert.Equal(new string[] { "foo" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.DeselectRange(0, 2);

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void DeselectRange_Does_Nothing_For_Nonintersecting_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;
                target.SelectionChanged += (s, e) => ++raised;
                target.DeselectRange(1, 2);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0 }, target.SelectedIndexes);
                Assert.Equal("foo", target.SelectedItem);
                Assert.Equal(new string[] { "foo" }, target.SelectedItems);
                Assert.Equal(0, raised);
            }
        }

        public class ClearSelection
        {
            [Fact]
            public void ClearSelection_Raises_SelectionChanged()
            {
                var target = CreateTarget();
                var raised = 0;

                target.Select(1);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1 }, e.DeselectedIndices);
                    Assert.Equal(new[] { "bar" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.ClearSelection();

                Assert.Equal(1, raised);
            }
        }

        public class AnchorIndex
        {
            [Fact]
            public void Setting_SelectedIndex_Sets_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.AnchorIndex))
                    {
                        ++raised;
                    }
                };

                target.SelectedIndex = 1;

                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Setting_SelectedIndex_To_Minus_1_Clears_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 1;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.AnchorIndex))
                    {
                        ++raised;
                    }
                };

                target.SelectedIndex = -1;

                Assert.Equal(-1, target.AnchorIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Select_Sets_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.AnchorIndex))
                    {
                        ++raised;
                    }
                };

                target.Select(1);

                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Deselect_Doesnt_Clear_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.Select(1);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.AnchorIndex))
                    {
                        ++raised;
                    }
                };

                target.Deselect(1);

                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(0, raised);
            }
        }

        public class SingleSelect
        {
            [Fact]
            public void Converting_To_Multiple_Selection_Preserves_Selection()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 1;

                target.SelectionChanged += (s, e) => ++raised;

                target.SingleSelect = false;

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new string[] { "bar" }, target.SelectedItems);
                Assert.Equal(0, raised);
            }
        }

        public class CollectionChanges
        {
            [Fact]
            public void Removing_Selected_Item_Updates_State()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var selectedIndexRaised = 0;

                target.Source = data;
                target.Select(1);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++selectedIndexRaised;
                    }
                };

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Equal(new[] { "bar" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                    ++selectionChangedRaised;
                };

                data.RemoveAt(1);

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(-1, target.AnchorIndex);
                Assert.Equal(1, selectionChangedRaised);
                Assert.Equal(1, selectedIndexRaised);
            }

            [Fact]
            public void Removing_Unselected_Item_Before_Selected_Item_Updates_Indexes()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var indexesChangedraised = 0;

                target.SelectedIndex = 1;

                target.SelectionChanged += (s, e) => ++selectionChangedRaised;

                target.IndexesChanged += (s, e) =>
                {
                    Assert.Equal(0, e.StartIndex);
                    Assert.Equal(-1, e.Delta);
                    ++indexesChangedraised;
                };

                data.RemoveAt(0);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar" }, target.SelectedItems);
                Assert.Equal(0, target.AnchorIndex);
                Assert.Equal(1, indexesChangedraised);
                Assert.Equal(0, selectionChangedRaised);
            }

            [Fact]
            public void Removing_Unselected_Item_After_Selected_Doesnt_Raise_Events()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var raised = 0;

                target.SelectedIndex = 1;

                target.PropertyChanged += (s, e) => ++raised;
                target.SelectionChanged += (s, e) => ++raised;
                target.IndexesChanged += (s, e) => ++raised;

                data.RemoveAt(2);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new string[] { "bar" }, target.SelectedItems);
                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(0, raised);
            }
        }

        private static SelectionModel<string> CreateTarget(bool createData = true)
        {
            var result = new SelectionModel<string> { SingleSelect = true };

            if (createData)
            {
                result.Source = new AvaloniaList<string> { "foo", "bar", "baz" };
            }

            return result;
        }
    }
}
