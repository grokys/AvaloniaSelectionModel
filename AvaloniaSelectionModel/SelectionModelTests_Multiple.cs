using Avalonia.Controls.Selection;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Selection
{
    public class SelectionModelTests_Multiple
    {
        public class No_Source
        {
            [Fact]
            public void Can_Select_Multiple_Items_Before_Source_Assigned()
            {
                var target = CreateTarget(false);
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    var index = raised switch
                    {
                        0 => 5,
                        1 => 10,
                        2 => 100,
                    };

                    Assert.Empty(e.DeselectedIndices);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { index }, e.SelectedIndices);
                    Assert.Equal(new string?[] { null }, e.SelectedItems);
                    ++raised;
                };

                target.SelectedIndex = 5;
                target.Select(10);
                target.Select(100);

                Assert.Equal(5, target.SelectedIndex);
                Assert.Equal(new[] { 5, 10, 100 }, target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Equal(new string?[] { null, null, null }, target.SelectedItems);
                Assert.Equal(3, raised);
            }

            [Fact]
            public void Initializing_Source_Retains_Valid_Selection_And_Removes_Invalid()
            {
                var target = CreateTarget(false);
                var raised = 0;

                target.SelectedIndex = 1;
                target.Select(2);
                target.Select(10);
                target.Select(100);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 10, 100 }, e.DeselectedIndices);
                    Assert.Equal(new string?[] { null, null }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.Source = new[] { "foo", "bar", "baz" };

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1, 2 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Initializing_Source_Coerces_SelectedIndex()
            {
                var target = CreateTarget(false);
                var raised = 0;

                target.SelectedIndex = 100;
                target.Select(2);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++raised;
                    }
                };

                target.Source = new[] { "foo", "bar", "baz" };

                Assert.Equal(2, target.SelectedIndex);
                Assert.Equal(new[] { 2 }, target.SelectedIndexes);
                Assert.Equal("baz", target.SelectedItem);
                Assert.Equal(new[] { "baz" }, target.SelectedItems);
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
                Assert.Equal(new[] { "bar" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }
        }

        public class Select
        {
            [Fact]
            public void Select_Adds_To_Selection()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 1 }, e.SelectedIndices);
                    Assert.Equal(new string[] { "bar" }, e.SelectedItems);
                    ++raised;
                };

                target.Select(1);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0, 1 }, target.SelectedIndexes);
                Assert.Equal("foo", target.SelectedItem);
                Assert.Equal(new[] { "foo", "bar" }, target.SelectedItems);
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
            public void SelectRange_Selects_Items()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 1, 2 }, e.SelectedIndices);
                    Assert.Equal(new string[] { "bar", "baz" }, e.SelectedItems);
                    ++raised;
                };

                target.SelectRange(1, 2);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1, 2 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);
                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void SelectRange_Ignores_Out_Of_Bounds_Items()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 1, 2 }, e.SelectedIndices);
                    Assert.Equal(new string[] { "bar", "baz" }, e.SelectedItems);
                    ++raised;
                };

                target.SelectRange(1, 20);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1, 2 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);
                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void SelectRange_Does_Nothing_For_Non_Intersecting_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) => ++raised;

                target.SelectRange(11, 20);

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Equal(-1, target.AnchorIndex);
                Assert.Equal(0, raised);
            }
        }

        public class Deselect
        {
            [Fact]
            public void Deselect_Clears_Selected_Item()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;
                target.Select(1);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1 }, e.DeselectedIndices);
                    Assert.Equal(new string[] { "bar" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.Deselect(1);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0 }, target.SelectedIndexes);
                Assert.Equal("foo", target.SelectedItem);
                Assert.Equal(new[] { "foo" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }
        }

        public class DeselectRange
        {
            [Fact]
            public void DeselectRange_Clears_Identical_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectRange(1, 2);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1, 2 }, e.DeselectedIndices);
                    Assert.Equal(new string[] { "bar", "baz" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.DeselectRange(1, 2);

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void DeselectRange_Clears_Intersecting_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectRange(1, 2);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1 }, e.DeselectedIndices);
                    Assert.Equal(new string[] { "bar" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.DeselectRange(0, 1);

                Assert.Equal(2, target.SelectedIndex);
                Assert.Equal(new[] { 2 }, target.SelectedIndexes);
                Assert.Equal("baz", target.SelectedItem);
                Assert.Equal(new[] { "baz" }, target.SelectedItems);
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
                target.Select(2);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1, 2 }, e.DeselectedIndices);
                    Assert.Equal(new[] { "bar", "baz" }, e.DeselectedItems);
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

                target.SelectedIndex = 0;

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
            public void SelectRange_Doesnt_Overwrite_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.AnchorIndex = 0;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.AnchorIndex))
                    {
                        ++raised;
                    }
                };

                target.SelectRange(1, 2);

                Assert.Equal(0, target.AnchorIndex);
                Assert.Equal(0, raised);
            }

            [Fact]
            public void Deselect_Doesnt_Clear_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.Select(0);
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

        private static SelectionModel<string> CreateTarget(bool createData = true)
        {
            var result = new SelectionModel<string> { SingleSelect = false };

            if (createData)
            {
                result.Source = new[] { "foo", "bar", "baz" };
            }

            return result;
        }
    }
}
