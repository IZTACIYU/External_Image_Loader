namespace ImageLoader
{
    public class InputField : IControlMountable
    {
        public required Label Header { get; set; }
        public required TextBox InputBox { get; set; }

        public void MountTo(Control.ControlCollection control)
        {
            control.Add(Header);
            control.Add(InputBox);
        }
    }
}
