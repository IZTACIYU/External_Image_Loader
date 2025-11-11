namespace ImageLoader
{
    public class SwitchTools : ToolStrip, IControlMountable<Control.ControlCollection>, IItemProvider<ToolStripButton>
    {
        public required List<ToolStripButton> Buttons { get; set; }

        public void MountTo(Control.ControlCollection control)
        {
            foreach (var button in Buttons)
                this.Items.Add(button);
            control.Add(this);
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