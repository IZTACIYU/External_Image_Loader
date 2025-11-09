namespace ImageLoader
{
    static public class Util
    {
        static public void ToExifPanel(this RichTextBox richText, Dictionary<string, string?> kvp)
        {
            richText.Clear();

            Font baseFont = richText.Font;
            Color baseColor = richText.ForeColor;

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
                    richText.SelectionFont = keyFont;
                    richText.SelectionColor = Color.Gold;
                    richText.AppendText(key + ": ");

                    // 벨류값 서식 설정 (Italic, LightGray)
                    richText.SelectionFont = valueFont;
                    richText.SelectionColor = Color.LightGray;
                    richText.AppendText(value);

                    richText.AppendText("\n");
                }
            }
            finally
            {
                // 폰트 객체 리소스 해제
                keyFont.Dispose();
                valueFont.Dispose();

                // 기본값 초기화
                richText.SelectionFont = baseFont;
                richText.SelectionColor = baseColor;

                // 캐럿 최상단 이동
                richText.Select(0, 0);
            }
        }
        static public void ToExifPanel(this RichTextBox richText, Simplified data)
        {
            var kvp = data.KVP();

            richText.Clear();

            Font baseFont = richText.Font;
            Color baseColor = richText.ForeColor;

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
                    richText.SelectionFont = keyFont;
                    richText.SelectionColor = COLOR.NAI_HEADER;
                    richText.AppendText(key + ": ");

                    // 벨류값 서식 설정 (Italic, LightGray)
                    richText.SelectionFont = valueFont;
                    richText.SelectionColor = COLOR.NAI_VALUE;
                    richText.AppendText(value);

                    richText.AppendText("\n");
                }
            }
            finally
            {
                // 폰트 객체 리소스 해제
                keyFont.Dispose();
                valueFont.Dispose();

                // 기본값 초기화
                richText.SelectionFont = baseFont;
                richText.SelectionColor = baseColor;

                // 캐럿 최상단 이동
                richText.Select(0, 0);
            }
        }
    }
}