using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImageLoader
{

    // --- (RawParameters.cs) ---
    public class RawParameters
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("steps")]
        public int Steps { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("scale")]
        public double Scale { get; set; }

        [JsonPropertyName("uncond_scale")]
        public double UncondScale { get; set; }

        [JsonPropertyName("cfg_rescale")]
        public double CfgRescale { get; set; }

        [JsonPropertyName("seed")]
        public long Seed { get; set; }

        [JsonPropertyName("n_samples")]
        public int NSamples { get; set; }

        [JsonPropertyName("noise_schedule")]
        public string NoiseSchedule { get; set; }

        [JsonPropertyName("v4_prompt")]
        public V4Prompt V4Prompt { get; set; }

        [JsonPropertyName("v4_negative_prompt")]
        public V4NegativePrompt V4NegativePrompt { get; set; }

        [JsonPropertyName("sampler")]
        public string Sampler { get; set; }

        [JsonPropertyName("request_type")]
        public string RequestType { get; set; }

        [JsonPropertyName("signed_hash")]
        public string SignedHash { get; set; }
    }
    public class ExtraPassthroughTesting
    {
        [JsonPropertyName("prompt")]
        public object Prompt { get; set; }

        [JsonPropertyName("uc")]
        public object Uc { get; set; }

        [JsonPropertyName("hide_debug_overlay")]
        public bool HideDebugOverlay { get; set; }

        [JsonPropertyName("r")]
        public double R { get; set; }

        [JsonPropertyName("eta")]
        public double Eta { get; set; }

        [JsonPropertyName("negative_momentum")]
        public double NegativeMomentum { get; set; }

        [JsonPropertyName("director_reference_images")]
        public object DirectorReferenceImages { get; set; }

        [JsonPropertyName("director_reference_descriptions")]
        public object DirectorReferenceDescriptions { get; set; }

        [JsonPropertyName("director_reference_information_extracted")]
        public object DirectorReferenceInformationExtracted { get; set; }

        [JsonPropertyName("director_reference_strengths")]
        public object DirectorReferenceStrengths { get; set; }

        [JsonPropertyName("director_reference_secondary_strengths")]
        public object DirectorReferenceSecondaryStrengths { get; set; }
    }
    public class V4Prompt
    {
        [JsonPropertyName("caption")]
        public Caption Caption { get; set; }
    }
    public class V4NegativePrompt
    {
        [JsonPropertyName("caption")]
        public Caption Caption { get; set; }
    }
    public class Caption
    {
        [JsonPropertyName("base_caption")]
        public string BaseCaption { get; set; }

        [JsonPropertyName("char_captions")]
        public List<CharCaption> CharCaptions { get; set; }
    }
    public class CharCaption
    {
        [JsonPropertyName("char_caption")]
        public string CharCaptionText { get; set; }
    }
    public class Center
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }
    }
}
