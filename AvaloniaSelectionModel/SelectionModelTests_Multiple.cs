using Avalonia.Controls.Selection;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Selection
{
    public class SelectionModelTests_Multiple
    {
        [Fact]
        public void Can_Select_Multiple_Items_Before_Source_Assigned()
        {
            var target = CreateTarget(false);

            target.SelectedIndex = 5;
            target.Select(10);
            target.Select(100);

            Assert.Equal(5, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Initializing_Source_Retains_Valid_Selection_Removes_Invalid()
        {
            var target = CreateTarget(false);

            target.SelectedIndex = 1;
            target.Select(2);
            target.Select(10);
            target.Select(100);
            target.Source = new[] { "foo", "bar", "baz" };

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { 1, 2 }, target.SelectedIndexes);
            Assert.Equal("bar", target.SelectedItem);
        }

        [Fact]
        public void SelectedIndex_Larger_Than_Source_Clears_Selection()
        {
            var target = CreateTarget();

            target.SelectedIndex = 1;
            target.SelectedIndex = 5;

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Empty(target.SelectedIndexes);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Negative_SelectedIndex_Is_Coerced_To_Minus_1()
        {
            var target = CreateTarget();

            target.SelectedIndex = -5;

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Empty(target.SelectedIndexes);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Setting_SelectedIndex_Clears_Old_Selection()
        {
            var target = CreateTarget();

            target.SelectedIndex = 0;
            target.SelectedIndex = 1;

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { 1 }, target.SelectedIndexes);
            Assert.Equal("bar", target.SelectedItem);
        }

        [Fact]
        public void Select_Adds_To_Selection()
        {
            var target = CreateTarget();

            target.SelectedIndex = 0;
            target.Select(1);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { 0, 1 }, target.SelectedIndexes);
            Assert.Equal("bar", target.SelectedItem);
        }

        [Fact]
        public void Deselect_Clears_Selected_Item()
        {
            var target = CreateTarget();

            target.SelectedIndex = 0;
            target.Select(1);
            target.Deselect(1);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { 0 }, target.SelectedIndexes);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Setting_SelectedIndex_Sets_AnchorIndex()
        {
            var target = CreateTarget();

            target.SelectedIndex = 1;

            Assert.Equal(1, target.AnchorIndex);
        }

        [Fact]
        public void Setting_SelectedIndex_To_Minus_1_Clears_AnchorIndex()
        {
            var target = CreateTarget();

            target.SelectedIndex = 1;
            target.SelectedIndex = -1;

            Assert.Equal(-1, target.AnchorIndex);
        }

        [Fact]
        public void Select_Sets_AnchorIndex()
        {
            var target = CreateTarget();

            target.SelectedIndex = 0;
            target.Select(1);

            Assert.Equal(1, target.AnchorIndex);
        }

        [Fact]
        public void Deselect_Doesnt_Clear_AnchorIndex()
        {
            var target = CreateTarget();

            target.Select(0);
            target.Select(1);
            target.Deselect(1);

            Assert.Equal(1, target.AnchorIndex);
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
