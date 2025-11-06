using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLoader
{
    internal class SimpleSaveForm
    {
        public string BaseURL { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NameToken { get; set; } = string.Empty;
        public string SituationToken { get; set; } = string.Empty;
        public int St_Num { get; set; } = 0;
        public int En_Num { get; set; } = 0;
        public int Multi_Call_Num { get; set; } = 0;
    }
}
