namespace ImageLoader
{
    public class ImageWindow : Form, ICommit, IItemProvider<Button>, IElementHandle<Button>
    {
        public PictureBox Image { get; set; } = null!;
        public FlowLayoutPanel FlowLayOut { get; set; } = null!;
        private Dictionary<string, Button> Buttons { get; set; } = new();


        public Button GetItem(string name) => GetElement(name);
        public Button GetElement(string name)
        {
            if (Buttons.ContainsKey(name) == false)
            {
                MessageBox.Show("존재하지 않는 버튼에 대한 접근시도", "ERROR");
                return new() { Text = "오류" };
            }

            return Buttons[name];
        }
        public void SetElement(string name, Button button)
        {
            if(Buttons.ContainsKey(name) == true)
            {
                MessageBox.Show($"{name}을 이름으로 하는 버튼이 컨테이너 내부에 이미 존재합니다.", "ERROR");
                return;
            }

            button.Name = name;
            button.Text = name;
            Buttons.Add(name, button);
        }
        public void RemoveElement(string name)
        {
            // null 필터링
            if(Buttons.ContainsKey(name) == false)
                return;

            try
            {
                var elem = Buttons[name];
                Buttons.Remove(name);
                this.FlowLayOut.Controls.Remove(elem);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR");
                return;
            }
        }

        public void Commit()
        {
            this.Controls.Add(Image);

            foreach (var button in Buttons)
                this.FlowLayOut.Controls.Add(button.Value);
            this.Controls.Add(this.FlowLayOut);
        }
    }
}
