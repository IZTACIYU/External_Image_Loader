namespace ImageLoader
{
    public class TextPanel
    {
        public required Panel LayOut { get; set; }          // 컨테이너 크기에 따라 유동적으로 크기가 변하는 패널
        public required RichTextBox Informations { get; set; }   // 텍스트 박스
        public required SwitchTools Buttons { get; set; }   // Simplified, Raw Params
        public required Panel Contents { get; set; }        // 현재 지정된 모드(버튼)에 따른 내용을 띄울 텍스트를 담기 위한 페널, 내용크기에 따라 매널 크기도 변함
        public required RichTextBox Context { get; set; }   // 텍스트 박스
    }
}
