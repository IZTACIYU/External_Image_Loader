using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLoader
{
    public class ImageMetaData
    {
        public string Software { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;

        public string Prompt { get; set; } = string.Empty;

        public string Steps { get; set; } = string.Empty;
        public string Height { get; set; } = string.Empty;
        public string Width { get; set; } = string.Empty;

        public string Scale { get; set; } = string.Empty;
        public string Uncond_Scale { get; set; } = string.Empty;
        public string Cfg_rescale { get; set; } = string.Empty;
        public string Seed { get; set; }

        public string N_Samples { get; set; } = string.Empty;

        public string Noise_Schedule { get; set; } = string.Empty; // Karras
        public string Sampler { get; set; } = string.Empty;        // Euler
    }
}
