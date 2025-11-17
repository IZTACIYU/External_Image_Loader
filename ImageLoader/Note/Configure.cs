namespace ImageLoader
{
    public class LoaderConfig
    {
        public string BaseURL { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NameToken { get; set; } = string.Empty;
        public string SituationToken { get; set; } = string.Empty;
        public int St_Num { get; set; } = 0;
        public int En_Num { get; set; } = 0;
        public int Multi_Call_Num { get; set; } = 0;
    }

    public class DirectoryConfig
    {
        public string InputDirectory { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
    }
    public class Configurence
    {
        public string InputPath { get; set; } = string.Empty;
        public string OutputPath{ get; set; } = string.Empty;
    }
}
