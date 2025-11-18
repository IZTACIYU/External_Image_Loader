using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageLoader
{
    public class UpdateChecker
    {
        private const string VERSION_URL = "https://raw.githubusercontent.com/IZTACIYU/External_Image_Loader/main/version.txt";
        private const string RELEASE_PAGE_URL = "https://github.com/IZTACIYU/External_Image_Loader/releases/latest";

        public async Task CheckAsync()
        {
            try
            {
                using var client = new HttpClient();
                string requestUrl = $"{VERSION_URL}?t={DateTime.Now.Ticks}";

                client.Timeout = TimeSpan.FromSeconds(3);

                string remoteVersionString = await client.GetStringAsync(requestUrl);
                remoteVersionString = remoteVersionString.Trim().TrimStart('v');

                if (!Version.TryParse(remoteVersionString, out var latestVer)) return;

                var currentVer = Assembly.GetExecutingAssembly().GetName().Version;

                if (latestVer > currentVer)
                {
                    var res = MessageBox.Show(
                        $"새로운 버전이 감지되었습니다!\n\n" +
                        $"현재 버전: {currentVer}\n" +
                        $"최신 버전: {latestVer}\n\n" +
                        $"다운로드 페이지로 이동하시겠습니까?",
                        "업데이트 알림",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information
                    );

                    if (res == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = RELEASE_PAGE_URL,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch
            {
                MessageBox.Show("ERROR");
            }
        }
    }
}