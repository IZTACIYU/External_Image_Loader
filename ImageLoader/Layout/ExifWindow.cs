namespace ImageLoader
{
    public class ExifWindow : Form, ICommit, IItemProvider<ToolStripButton>
    {
        public required SwitchTools ToolBars { get; set; }
        public required Panel BackGround { get; set; } // Label로 바꾸기
        public required RichTextBox Prompts { get; set; }
        public FlowLayoutPanel? Characters { get; set; }

        public void Commit()
        {
            ToolBars.MountTo(this.Controls);
            BackGround.Controls.Add(Prompts);
            this.Controls.Add(BackGround);
        }

        public ToolStripButton GetItem(string Name)
        {
            return ((IItemProvider<ToolStripButton>)ToolBars).GetItem(Name);
        }
    }
}


//using System.Windows.Forms;

//namespace ImageLoader
//{
//    public class ExifWindow : Form, ICommit, IItemProvider<Switch> // ToolStripButton -> Switch
//    {
//        public required SwitchTools ToolBars { get; set; }
//        public required RichTextBox Informations { get; set; } // Prompts -> Informations
//        public required Panel Contents { get; set; } // BackGround -> Contents
//        public required RichTextBox Context { get; set; } // 신규
//        public FlowLayoutPanel? Characters { get; set; }

//        public void Commit()
//        {
//            // Dock 순서: Fill -> Top -> Top

//            // 1. Context를 Contents 패널에 채웁니다.
//            this.Contents.Controls.Add(this.Context);

//            // 2. Contents 패널을 폼에 채웁니다 (Dock.Fill).
//            this.Controls.Add(this.Contents);

//            // 3. Info 패널을 Contents 위에 배치합니다 (Dock.Top).
//            this.Controls.Add(this.Informations);

//            // 4. 툴바를 Info 패널 위에 배치합니다 (Dock.Top).
//            ToolBars.MountTo(this.Controls);

//            // 툴바가 가려지는 것을 방지하기 위해 맨 앞으로 가져옵니다.
//            ToolBars.BringToFront();
//        }

//        public Switch GetItem(string Name)
//        {
//            return ToolBars.GetItem(Name); // 인터페이스 구현
//        }
//    }
//}