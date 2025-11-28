using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
namespace ImageLoader
{
    static public class Program
    {
        [STAThread]
        static private void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new MainForm());

            using (var host = new WebUiHost())
            {
                host.Start(); // 브라우저 자동 실행 및 서버 시작

                // 콘솔 창이 바로 꺼지지 않게 대기 (또는 WinForms의 Hidden Form 사용)
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
        }
    }    
}
