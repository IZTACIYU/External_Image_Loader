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

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"<b>Title</b>: <i>{Title}</i>");
            sb.AppendLine($"<b>Description</b>: <i>{Description}</i>");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"<b>Software</b>: <i>{Software}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Source</b>: <i>{Source}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Request Type</b>: <i>{RequestType}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Prompt</b>: <i>{Prompt}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Undesired Content</b>: <i>{UndesiredContent}</i>");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(Character1Prompt))
            {
                sb.AppendLine($"<b>Character 1 Prompt</b>: <i>{Character1Prompt}</i>");
                sb.AppendLine($"<b>Character 1 UC</b>: <i>{Character1UC}</i>");
            }
            if (!string.IsNullOrEmpty(Character2Prompt))
            {
                sb.AppendLine($"<b>Character 1 Prompt</b>: <i>{Character2Prompt}</i>");
                sb.AppendLine($"<b>Character 1 UC</b>: <i>{Character2UC}</i>");
            }
            if (!string.IsNullOrEmpty(Character3Prompt))
            {
                sb.AppendLine($"<b>Character 3 Prompt</b>: <i>{Character3Prompt}</i>");
                sb.AppendLine($"<b>Character 3 UC</b>: <i>{Character3UC}</i>");
            }
            if (!string.IsNullOrEmpty(Character4Prompt))
            {
                sb.AppendLine($"<b>Character 4 Prompt</b>: <i>{Character4Prompt}</i>");
                sb.AppendLine($"<b>Character 4 UC</b>: <i>{Character4UC}</i>");
            }
            if (!string.IsNullOrEmpty(Character5Prompt))
            {
                sb.AppendLine($"<b>Character 5 Prompt</b>: <i>{Character5Prompt}</i>");
                sb.AppendLine($"<b>Character 5 UC</b>: <i>{Character5UC}</i>");
            }
            if (!string.IsNullOrEmpty(Character6Prompt))
            {
                sb.AppendLine($"<b>Character 6 Prompt</b>: <i>{Character6Prompt}</i>");
                sb.AppendLine($"<b>Character 6 UC</b>: <i>{Character6UC}</i>");
            }

            sb.AppendLine();
            sb.AppendLine($"<b>Resolution</b>: <i>{Resolution}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Seed</b>: <i>{Seed}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Steps</b>: <i>{Steps}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Sampler</b>: <i>{Sampler}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Prompt Guidance</b>: <i>{PromptGuidance}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Prompt Guidance Rescale</b>: <i>{PromptGuidanceRescale}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Undesired Content Strength</b>: <i>{UndesiredContentStrength}</i>");

            return sb.ToString();
        }
    }
}
