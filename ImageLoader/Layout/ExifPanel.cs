namespace ImageLoader
{
    public class ExifPanel : Form, ICommit
    {
        public required SwitchTools ToolBars {  get; set; }
        public required RichTextBox Prompts {  get; set; }
        public FlowLayoutPanel? Characters { get; set; }

        public void Commit()
        {
            ToolBars.MountTo(this.Controls);
            this.Controls.Add(Prompts);
        }
    }
}
