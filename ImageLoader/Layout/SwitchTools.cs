namespace ImageLoader
{
    public class SwitchTools : ToolStrip, IControlMountable<Control.ControlCollection>, IItemProvider<Switch>
    {
        public required List<Switch> Buttons { get; set; }

        public void MountTo(Control.ControlCollection control)
        {
            foreach (var button in Buttons)
                this.Items.Add(button);
            control.Add(this);
        }

        public Switch GetItem(string name)
        {
            Switch value;

            try
            {
                value = Buttons.First(item => item.Name == name);
            }
            catch
            {
                MessageBox.Show("NAME TAG DOES NOT EXISTS");
                return new() { Code = "ERROR" };
            }

            return value;
        }
    }
}