namespace ImageLoader
{
    public class ExifPanel : Form, ICommit
    {
        public required SwitchTools ToolBars {  get; set; }
        public required Form BackGround { get; set; } // Label로 바꾸기
        public required RichTextBox Prompts {  get; set; }
        public FlowLayoutPanel? Characters { get; set; }

        public void Commit()
        {
            ToolBars.MountTo(this.Controls);
            BackGround.Controls.Add(Prompts);
            this.Controls.Add(BackGround);
        }
    }
}
