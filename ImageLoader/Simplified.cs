using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ImageLoader
{
    public class Simplified
    {   // 딕셔너리 - 유동적  구조로 변경
        public string Title { get; set; }
        public string Description { get; set; }
        public string Software { get; set; }
        public string Source { get; set; }
        public string RequestType { get; set; }
        public string Prompt { get; set; }
        public string UndesiredContent { get; set; }
        public string Character1Prompt { get; set; }
        public string Character1UC { get; set; }
        public string Character2Prompt { get; set; }
        public string Character2UC { get; set; }
        public string Character3Prompt { get; set; }
        public string Character3UC { get; set; }
        public string Character4Prompt { get; set; }
        public string Character4UC { get; set; }
        public string Character5Prompt { get; set; }
        public string Character5UC { get; set; }
        public string Character6Prompt { get; set; }
        public string Character6UC { get; set; }
        public string Resolution { get; set; }
        public string Seed { get; set; }
        public string Steps { get; set; }
        public string Sampler { get; set; }
        public string PromptGuidance { get; set; }
        public string PromptGuidanceRescale { get; set; }
        public string UndesiredContentStrength { get; set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"Title: {Title}");
            sb.AppendLine($"Description: {Description}");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"Software: {Software}");
            sb.AppendLine();
            sb.AppendLine($"Source: {Source}");
            sb.AppendLine();
            sb.AppendLine($"Request Type: {RequestType}");
            sb.AppendLine();
            sb.AppendLine($"Prompt: {Prompt}");
            sb.AppendLine();
            sb.AppendLine($"Undesired Content: {UndesiredContent}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(Character1Prompt))
            {
                sb.AppendLine($"Character 1 Prompt: {Character1Prompt}");
                sb.AppendLine($"Character 1 UC: {Character1UC}");
            }
            if (!string.IsNullOrEmpty(Character2Prompt))
            {
                sb.AppendLine($"Character 1 Prompt: {Character2Prompt}");
                sb.AppendLine($"Character 1 UC: {Character2UC}");
            }
            if (!string.IsNullOrEmpty(Character3Prompt))
            {
                sb.AppendLine($"Character 3 Prompt: {Character3Prompt}");
                sb.AppendLine($"Character 3 UC: {Character3UC}");
            }
            if (!string.IsNullOrEmpty(Character4Prompt))
            {
                sb.AppendLine($"Character 4 Prompt: {Character4Prompt}");
                sb.AppendLine($"Character 4 UC: {Character4UC}");
            }
            if (!string.IsNullOrEmpty(Character5Prompt))
            {
                sb.AppendLine($"Character 5 Prompt: {Character5Prompt}");
                sb.AppendLine($"Character 5 UC: {Character5UC}");
            }
            if (!string.IsNullOrEmpty(Character6Prompt))
            {
                sb.AppendLine($"Character 6 Prompt: {Character6Prompt}");
                sb.AppendLine($"Character 6 UC: {Character6UC}");
            }

            sb.AppendLine();
            sb.AppendLine($"Resolution: {Resolution}");
            sb.AppendLine();
            sb.AppendLine($"Seed: {Seed}");
            sb.AppendLine();
            sb.AppendLine($"Steps: {Steps}");
            sb.AppendLine();
            sb.AppendLine($"Sampler: {Sampler}");
            sb.AppendLine();
            sb.AppendLine($"Prompt Guidance: {PromptGuidance}");
            sb.AppendLine();
            sb.AppendLine($"Prompt Guidance Rescale: {PromptGuidanceRescale}");
            sb.AppendLine();
            sb.AppendLine($"Undesired Content Strength: {UndesiredContentStrength}");
            sb.AppendLine();

            return sb.ToString();

        }
    }
}
