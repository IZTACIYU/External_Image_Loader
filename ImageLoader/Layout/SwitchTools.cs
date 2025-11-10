namespace ImageLoader
{
    public class SwitchTools : IControlMountable<ToolStrip>, IItemProvider<ToolStripButton>
    {
        public required List<ToolStripButton> Buttons { get; set; }

        public void MountTo(ToolStrip control)
        {
            foreach (var button in Buttons)
                control.Items.Add(button);
        }

        public ToolStripButton GetItem(string name)
        {
            ToolStripButton value;

            try
            {
                value = Buttons.First(item => item.Name == name);
            }
            catch
            {
                MessageBox.Show("NAME TAG DOES NOT EXISTS");
                return new();
            }

            return value;
        }
    }
}