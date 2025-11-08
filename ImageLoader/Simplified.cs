namespace ImageLoader
{
    public class Simplified
    {
        public string Title { get; set; }                    = "";
        public string Description { get; set; }              = "";
        public string Software { get; set; }                 = "";
        public string Source { get; set; }                   = "";
        public string RequestType { get; set; }              = "";
        public string Prompt { get; set; }                   = "";
        public string UndesiredContent { get; set; }         = "";
        public string Character1Prompt { get; set; }         = "";
        public string Character1UC { get; set; }             = "";
        public string Character2Prompt { get; set; }         = "";
        public string Character2UC { get; set; }             = "";
        public string Character3Prompt { get; set; }         = "";
        public string Character3UC { get; set; }             = "";
        public string Character4Prompt { get; set; }         = "";
        public string Character4UC { get; set; }             = "";
        public string Character5Prompt { get; set; }         = "";
        public string Character5UC { get; set; }             = "";
        public string Character6Prompt { get; set; }         = "";
        public string Character6UC { get; set; }             = "";
        public string Resolution { get; set; }               = "";
        public string Seed { get; set; }                     = "";
        public string Steps { get; set; }                    = "";
        public string Sampler { get; set; }                  = "";
        public string PromptGuidance { get; set; }           = "";
        public string PromptGuidanceRescale { get; set; }    = "";
        public string UndesiredContentStrength { get; set; } = "";


        public Dictionary<string, string?> KVP()
        {
            var kvp = new Dictionary<string, string?>()
            {
                { "Title", Title },
                { "Description", Description },
                { "Software", Software },
                { "Source", Source },
                { "RequestType", RequestType },
                { "Prompt", Prompt },
                { "UndesiredContent", UndesiredContent },
            };

            if (!string.IsNullOrEmpty(Character1Prompt))
            {
                kvp.Add("Character 1 Prompt", Character1Prompt);
                kvp.Add("Character 1 UC", Character1UC);
            }
            if (!string.IsNullOrEmpty(Character2Prompt))
            {
                kvp.Add("Character 2 Prompt", Character2Prompt);
                kvp.Add("Character 2 UC", Character2UC);
            }
            if (!string.IsNullOrEmpty(Character3Prompt))
            {
                kvp.Add("Character 3 Prompt", Character3Prompt);
                kvp.Add("Character 3 UC", Character3UC);
            }
            if (!string.IsNullOrEmpty(Character4Prompt))
            {
                kvp.Add("Character 4 Prompt", Character4Prompt);
                kvp.Add("Character 4 UC", Character4UC);
            }
            if (!string.IsNullOrEmpty(Character5Prompt))
            {
                kvp.Add("Character 5 Prompt", Character4Prompt);
                kvp.Add("Character 5 UC", Character4UC);
            }
            if (!string.IsNullOrEmpty(Character6Prompt))
            {
                kvp.Add("Character 6 Prompt", Character5Prompt);
                kvp.Add("Character 6 UC", Character5UC);
            }

            kvp.Add("Resolution", Resolution);
            kvp.Add("Seed", Seed);
            kvp.Add("Steps", Steps);
            kvp.Add("Sampler", Sampler);
            kvp.Add("PromptGuidance", PromptGuidance);
            kvp.Add("PromptGuidanceRescale", PromptGuidanceRescale);
            kvp.Add("UndesiredContentStrength", UndesiredContentStrength);

            return kvp;
        }
    }
}
