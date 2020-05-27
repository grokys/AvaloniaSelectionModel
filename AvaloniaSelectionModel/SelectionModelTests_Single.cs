using Avalonia.Controls.Selection;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Selection
{
    public class SelectionModelTests_Single
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

        private static SelectionModel<string> CreateTarget(bool createData = true)
        {
            var result = new SelectionModel<string> { SingleSelect = true };

            if (createData)
            {
                result.Source = new[] { "foo", "bar", "baz" };
            }

            return result;
        }
    }
}
