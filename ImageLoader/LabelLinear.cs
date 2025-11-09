namespace ImageLoader
{
    public class LabelLinear : IControlMountable    // 추후 테마 적용 확장 고려
    {
        public required InputField InputField;
        public required Button Button;

        public void MountTo(Control.ControlCollection control)
        {
            InputField.MountTo(control);
            control.Add(Button);
        }
    }
}
