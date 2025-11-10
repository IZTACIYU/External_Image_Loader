namespace ImageLoader
{
    public class LabelLinear : IControlMountable<Control.ControlCollection>    // 추후 테마 적용 확장 고려
    {
        public required InputField InputField {  get; set; }
        public required Button Button { get; set; }

        public void MountTo(Control.ControlCollection control)
        {
            InputField.MountTo(control);
            control.Add(Button);
        }
    }
}
