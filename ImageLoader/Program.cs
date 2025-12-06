using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
namespace ImageLoader
{
    static public class Program
    {
        private static WebUiHost _host;

        [STAThread]
        static void Main()
        {
            // 1. 콘솔 설정: 커서를 숨겨서 '입력 불가' 느낌을 줌
            try { Console.CursorVisible = false; } catch { }

            // 2. 서버 시작
            _host = new WebUiHost();

            try
            {
                _host.Start();

                // 3. 시작 로그 (WebUiHost 내부 로그와 별도로 구분)
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("==================================================");
                Console.WriteLine("   ImageFlow Server Running... (Logs Only)");
                Console.WriteLine("   - 창을 닫으면 종료됩니다.");
                Console.WriteLine("==================================================");
                Console.ResetColor();

                // [핵심 변경 사항]
                // 사용자 입력을 받지 않고, 프로세스를 영원히 대기 상태로 둡니다.
                // 엔터를 쳐도 꺼지지 않습니다.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Fatal Error] {ex.Message}");
                Console.ResetColor();

                // 에러가 났을 때만 키 입력을 받음 (에러 확인용)
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                _host?.Dispose();
            }
        }
    }    
}
