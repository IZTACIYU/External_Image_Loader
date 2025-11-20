using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageLoader
{
    public class UpdateChecker
    {
        private const string RELEASES_JSON_URL = "https://raw.githubusercontent.com/IZTACIYU/External_Image_Loader/refs/heads/main/version.json";
        private const string DOWNLOAD_PAGE_URL = "https://github.com/IZTACIYU/External_Image_Loader/releases/latest";

       public async Task CheckAsync()
        {
            try
            {
                using var client = new HttpClient();
                string requestUrl = $"{RELEASES_JSON_URL}?t={DateTime.Now.Ticks}"; // 캐시 방지
                client.Timeout = TimeSpan.FromSeconds(5);

                string json = await client.GetStringAsync(requestUrl);
                var releases = JsonSerializer.Deserialize<List<ReleaseInfo>>(json);

                if (releases == null || releases.Count == 0) return;

                var currentVer = Assembly.GetExecutingAssembly().GetName().Version;

                // 상위버전 필터링
                var newReleases = releases
                    .Where(r => Version.TryParse(r.VersionString, out var v) && v > currentVer)
                    .OrderByDescending(r => Version.Parse(r.VersionString))
                    .ToList();

                if (newReleases.Count == 0) return; // 업데이트 없음

                var sb = new StringBuilder();
                sb.AppendLine("새로운 버전이 업로드 되었습니다.");
                sb.AppendLine();
                sb.AppendLine($"현재 버전: {currentVer}");
                sb.AppendLine($"최신 버전: {newReleases[0].VersionString}");
                sb.AppendLine();
                sb.AppendLine("--- 변경 내역 ---");

                foreach (var release in newReleases)
                {
                    sb.AppendLine($"[v{release.VersionString}]"); // 버전 헤더
                    if (release.Notes != null)
                    {
                        foreach (var note in release.Notes)
                        {
                            sb.AppendLine($"- {note}"); // 내용
                        }
                    }
                    sb.AppendLine(); // 버전 간 간격
                }

                sb.AppendLine("다운로드 페이지로 이동하시겠습니까?");

                var res = MessageBox.Show(
                    sb.ToString(),
                    "업데이트 알림",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (res == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = DOWNLOAD_PAGE_URL,
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
                MessageBox.Show("Code:Room913 에서 오류가 발생했습니다. 문의해주세요.", "오류");
            }
        }

        // JSON 파싱용 데이터 클래스
        private class ReleaseInfo
        {
            [JsonPropertyName("version")]
            public string VersionString { get; set; }

            [JsonPropertyName("notes")]
            public List<string> Notes { get; set; }
        }
    }
}