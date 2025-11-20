namespace ImageLoader
{
    public class ImageTile : Panel, IControlMountable<Control.ControlCollection>
    {
        public PictureBox Image { get; set; } = null!;
        public Label Title { get; set; } = null!;
        public LinkLabel Meta { get; set; } = null!;
        public Label ExifLabel { get; set; } = null!;


        public void SetBorder(bool ok)
        {
            var borderColor = ok ? COLOR.EXIF_EXIST_TRUE : COLOR.EXIF_EXIST_FALSE;

            this.Paint += (s, e) =>
            {
                using var pen = new Pen(borderColor, 4);
                e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 2, this.Height - 2);
            };

        }

        public void MountTo(ControlCollection control)
        {
            this.Controls.Add(Image);
            this.Controls.Add(Title);
            this.Controls.Add(Meta);
            this.Controls.Add(ExifLabel);
            control.Add(this);
        }
    }
}
