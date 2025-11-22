namespace ImageLoader
{
    public class Tools : IControlMountable<ToolStrip>, IItemProvider<ToolStripMenuItem>,ICopyable<ToolStripMenuItem>
    {
        public required ToolStripDropDownButton Tool {  get; set; }
        public required List<ToolStripMenuItem> Items { get; set; }

        // IControlMountable
        public void MountTo(ToolStrip control)
        {
            foreach (ToolStripMenuItem item in Items)
            {
                Tool.DropDownItems.Add(item);
            }

            control.Items.Add(Tool);
        }

        // IItemProvider
        public ToolStripMenuItem GetItem(string name)
        {
            ToolStripMenuItem value;

            try
            {
                value = Items.FirstOrDefault(item => item.Name == name);
            }
            catch
            {
                MessageBox.Show("NAME TAG DOES NOT EXISTS");
                return new();
            }

            return value;
        }

        // ICopyable
        public ToolStripMenuItem Copy(string name)
        {
            if (Items.Count == 0) throw new InvalidOperationException("복사 할 객체 없음");
            
            
            return new ToolStripMenuItem
            {
                Name = name,
                Text = name,
                Enabled = Items[0].Enabled,
                Checked = Items[0].Checked,
                CheckOnClick = Items[0].CheckOnClick,
                TextAlign = Items[0].TextAlign,
                AutoSize = Items[0].AutoSize,
                Size = Items[0].Size,
                Margin = Items[0].Margin,
                Padding = Items[0].Padding,
                Tag = Items[0].Tag
                //ShortcutKeys = Items[0].ShortcutKeys,
                //ShowShortcutKeys = Items[0].ShowShortcutKeys,
            };
        }
    }
}
