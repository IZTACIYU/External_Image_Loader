using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImageLoader
{
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

        [JsonPropertyName("legacy_v3_extend")]
        public bool LegacyV3Extend { get; set; }

        [JsonPropertyName("reference_information_extracted_multiple")]
        public List<object> ReferenceInformationExtractedMultiple { get; set; }

        [JsonPropertyName("reference_strength_multiple")]
        public List<object> ReferenceStrengthMultiple { get; set; }

        [JsonPropertyName("extra_passthrough_testing")]
        public ExtraPassthroughTesting ExtraPassthroughTesting { get; set; }

        [JsonPropertyName("v4_prompt")]
        public V4Prompt V4Prompt { get; set; }

        [JsonPropertyName("v4_negative_prompt")]
        public V4NegativePrompt V4NegativePrompt { get; set; }

        [JsonPropertyName("director_reference_strengths")]
        public object DirectorReferenceStrengths { get; set; }

        [JsonPropertyName("director_reference_descriptions")]
        public object DirectorReferenceDescriptions { get; set; }

        [JsonPropertyName("director_reference_information_extracted")]
        public object DirectorReferenceInformationExtracted { get; set; }

        [JsonPropertyName("director_reference_secondary_strengths")]
        public object DirectorReferenceSecondaryStrengths { get; set; }

        [JsonPropertyName("sampler")]
        public string Sampler { get; set; }

        [JsonPropertyName("controlnet_strength")]
        public double ControlnetStrength { get; set; }

        [JsonPropertyName("controlnet_model")]
        public object ControlnetModel { get; set; }

        [JsonPropertyName("dynamic_thresholding")]
        public bool DynamicThresholding { get; set; }

        [JsonPropertyName("dynamic_thresholding_percentile")]
        public double DynamicThresholdingPercentile { get; set; }

        [JsonPropertyName("dynamic_thresholding_mimic_scale")]
        public double DynamicThresholdingMimicScale { get; set; }

        [JsonPropertyName("sm")]
        public bool Sm { get; set; }

        [JsonPropertyName("sm_dyn")]
        public bool SmDyn { get; set; }

        [JsonPropertyName("skip_cfg_above_sigma")]
        public double? SkipCfgAboveSigma { get; set; }

        [JsonPropertyName("skip_cfg_below_sigma")]
        public double? SkipCfgBelowSigma { get; set; }

        [JsonPropertyName("lora_unet_weights")]
        public object LoraUnetWeights { get; set; }

        [JsonPropertyName("lora_clip_weights")]
        public object LoraClipWeights { get; set; }

        [JsonPropertyName("deliberate_euler_ancestral_bug")]
        public bool DeliberateEulerAncestralBug { get; set; }

        [JsonPropertyName("prefer_brownian")]
        public bool PreferBrownian { get; set; }

        [JsonPropertyName("cfg_sched_eligibility")]
        public string CfgSchedEligibility { get; set; }

        [JsonPropertyName("explike_fine_detail")]
        public bool ExplikeFineDetail { get; set; }

        [JsonPropertyName("minimize_sigma_inf")]
        public bool MinimizeSigmaInf { get; set; }

        [JsonPropertyName("uncond_per_vibe")]
        public bool UncondPerVibe { get; set; }

        [JsonPropertyName("wonky_vibe_correlation")]
        public bool WonkyVibeCorrelation { get; set; }

        [JsonPropertyName("stream")]
        public string Stream { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("uc")]
        public string Uc { get; set; }

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

        [JsonPropertyName("use_coords")]
        public bool UseCoords { get; set; }

        [JsonPropertyName("use_order")]
        public bool UseOrder { get; set; }

        [JsonPropertyName("legacy_uc")]
        public bool LegacyUc { get; set; }
    }
    public class V4NegativePrompt
    {
        [JsonPropertyName("caption")]
        public Caption Caption { get; set; }

        [JsonPropertyName("use_coords")]
        public bool UseCoords { get; set; }

        [JsonPropertyName("use_order")]
        public bool UseOrder { get; set; }

        [JsonPropertyName("legacy_uc")]
        public bool LegacyUc { get; set; }
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

        [JsonPropertyName("centers")]
        public List<Center> Centers { get; set; }
    }
    public class Center
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }
    }
}
