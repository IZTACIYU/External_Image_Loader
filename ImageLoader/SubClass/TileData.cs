namespace ImageLoader
{
    public class TileData
    {
        public Job Job { get; set; }
        public Image Image { get; set; }
        public byte[] ImgBytes { get; set; }
        public bool ExifSuccess { get; set; }
    }
}
