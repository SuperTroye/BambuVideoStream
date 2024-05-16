namespace BambuVideoStream;

public static class Constants
{
    public static class OBS
    {
        public const string BambuScene = "BambuStream";
        public const string BambuStreamSource = "BambuStreamSource";

        public const string TextInputType = "text_gdiplus_v2";
        public const string ImageInputType = "image_source";
        public const string ColorInputType = "color_source_v3";
        public const string VideoInputType = "ffmpeg_source";

        public const string FfmpegOptions = "protocol_whitelist=file,udp,rtp";

        public const int NotFoundErrorCode = 600;

        public const int VideoWidth = 1920;
        public const int VideoHeight = 1080;
    }
}
