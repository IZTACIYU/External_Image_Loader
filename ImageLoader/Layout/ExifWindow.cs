namespace ImageLoader
{
    public class ExifWindow : Form, ICommit, IItemProvider<ToolStripButton>
    {
        public required SwitchTools ToolBars { get; set; }
        public required Panel BackGround { get; set; }
        public required RichTextBox Prompts { get; set; }
        public FlowLayoutPanel? Characters { get; set; }

        // ICommit
        public void Commit()
        {
            ToolBars.MountTo(this.Controls);
            BackGround.Controls.Add(Prompts);
            this.Controls.Add(BackGround);
        }

        // IItemProvider
        public ToolStripButton GetItem(string Name)
        {
            return ((IItemProvider<ToolStripButton>)ToolBars).GetItem(Name);
        }

        public void ToExifPanel(Simplified data)
        {
            var kvp = data.KVP();

            this.Prompts.Clear();

            Font baseFont = this.Prompts.Font;
            Color baseColor = this.Prompts.ForeColor;

            // 폰트 객체 캐싱
            // *** Font 객체는 GDI 리소스를 사용하므로, 나중에 꼭 Dispose() 해야함 ***
            Font keyFont = new Font(baseFont, FontStyle.Bold);
            Font valueFont = new Font(baseFont, FontStyle.Italic);

            try
            {
                foreach (var kvpValue in kvp)
                {
                    var key = kvpValue.Key;
                    var value = kvpValue.Value ?? "N/A";

                    // 키값 서식 설정 (Bold, Gold)
                    this.Prompts.SelectionFont = keyFont;
                    this.Prompts.SelectionColor = COLOR.NAI_HEADER;
                    this.Prompts.AppendText(key + ": ");

                    // 벨류값 서식 설정 (Italic, LightGray)
                    this.Prompts.SelectionFont = valueFont;
                    this.Prompts.SelectionColor = COLOR.NAI_VALUE;
                    this.Prompts.AppendText(value);

                    this.Prompts.AppendText("\n");
                }
            }
            finally
            {
                // 폰트 객체 리소스 해제
                keyFont.Dispose();
                valueFont.Dispose();

                // 기본값 초기화
                this.Prompts.SelectionFont = baseFont;
                this.Prompts.SelectionColor = baseColor;

                // 캐럿 최상단 이동
                this.Prompts.Select(0, 0);
            }
        }
    }
}