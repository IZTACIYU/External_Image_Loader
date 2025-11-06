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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class Job
    {
        public string Url { get; set; }
        public Dictionary<string, string> Tokens { get; set; }
    }
    
}
