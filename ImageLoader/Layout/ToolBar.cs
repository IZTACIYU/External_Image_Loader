namespace ImageLoader
{
    public class ToolBar : ToolStrip, IControlMountable<Control.ControlCollection>
    {
        public required List<Tools> Tools { get; set; }

        public void MountTo(Control.ControlCollection control)
        {
            foreach (var tool in Tools)
            {
                tool.MountTo(this);
            }

            control.Add(this);
        }
    }
}
