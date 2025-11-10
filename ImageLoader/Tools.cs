namespace ImageLoader
{
    public class Tools : IControlMountable<ToolStrip>
    {
        public required ToolStripDropDownButton Tool {  get; set; }
        public required List<ToolStripMenuItem> Items { get; set; }

        public void MountTo(ToolStrip control)
        {
            foreach (ToolStripMenuItem item in Items)
            {
                Tool.DropDownItems.Add(item);
            }

            control.Items.Add(Tool);
        }

        public void DupliCate(string name)
        {
            if (Items.Count == 0) throw new InvalidOperationException("복사 할 객체 없음");

            var item = Copy(name);

            Items.Add(item);
        }
        private ToolStripMenuItem Copy(string name) => new ToolStripMenuItem
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
