using Avalonia.Controls.Selection;
using Xunit;

namespace Avalonia.Controls.UnitTests.Selection
{
    public class SelectionModelTests_Single
    {
        [Fact]
        public void Can_Select_Item_Before_Source_Assigned()
        {
            var target = CreateTarget(false);

            target.SelectedIndex = 5;
            
            Assert.Equal(5, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Initializing_Source_Retains_Valid_Selection()
        {
            var target = CreateTarget(false);

            target.SelectedIndex = 1;
            target.Source = new[] { "foo", "bar", "baz" };

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { 1 }, target.SelectedIndexes);
            Assert.Equal("bar", target.SelectedItem);
        }

        [Fact]
        public void Initializing_Source_Removes_Invalid_Selection()
        {
            var target = CreateTarget(false);

            target.SelectedIndex = 5;
            target.Source = new[] { "foo", "bar", "baz" };

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Empty(target.SelectedIndexes);
            Assert.Null(target.SelectedItem);
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
        public void Select_Clears_Old_Selection()
        {
            var target = CreateTarget();

            target.SelectedIndex = 0;
            target.Select(1);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { 1 }, target.SelectedIndexes);
            Assert.Equal("bar", target.SelectedItem);
        }

        [Fact]
        public void Deselect_Clears_Current_Selection()
        {
            var target = CreateTarget();

            target.SelectedIndex = 0;
            target.Deselect(0);

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Empty(target.SelectedIndexes);
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

            target.Select(1);

            Assert.Equal(1, target.AnchorIndex);
        }

        [Fact]
        public void Deselect_Doesnt_Clear_AnchorIndex()
        {
            var target = CreateTarget();

            target.Select(1);
            target.Deselect(1);

            Assert.Equal(1, target.AnchorIndex);
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
