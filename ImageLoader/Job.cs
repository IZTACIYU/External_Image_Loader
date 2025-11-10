namespace ImageLoader
{
    public class Job
    {
        public string Url { get; set; }
        public required Dictionary<string, string> Tokens { get; set; }
    }
}
