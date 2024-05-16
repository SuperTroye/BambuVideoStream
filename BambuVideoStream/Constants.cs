using System.IO;
using System;

namespace BambuVideoStream;

public static class Constants
{
    public static class OBS
    {
        public const string BambuScene = "BambuStream";
        public const string BambuStreamSource = "BambuStreamSource";

        public static readonly InitialInputSettings ChamberTempInput = new("ChamberTemp", 71, 1029);
        public static readonly InitialInputSettings BedTempInput = new("BedTemp", 277, 1029);
        public static readonly InitialInputSettings TargetBedTempInput = new("TargetBedTemp", 313, 1029);
        public static readonly InitialInputSettings NozzleTempInput = new("NozzleTemp", 527, 1028);
        public static readonly InitialInputSettings TargetNozzleTempInput = new("TargetNozzleTemp", 580, 1028);
        public static readonly InitialInputSettings PercentCompleteInput = new("PercentComplete", 1510, 1022);
        public static readonly InitialInputSettings LayersInput = new("Layers", 1652, 972);
        public static readonly InitialInputSettings TimeRemainingInput = new("TimeRemaining", 1791, 1024);
        public static readonly InitialInputSettings SubtaskNameInput = new("SubtaskName", 838, 971);
        public static readonly InitialInputSettings StageInput = new("Stage", 842, 1019);
        public static readonly InitialInputSettings PartFanInput = new("PartFan", 58, 971);
        public static readonly InitialInputSettings AuxFanInput = new("AuxFan", 277, 971);
        public static readonly InitialInputSettings ChamberFanInput = new("ChamberFan", 521, 971);
        public static readonly InitialInputSettings FilamentInput = new("Filament", 1437, 1022);
        public static readonly InitialInputSettings PrintWeightInput = new("PrintWeight", 1303, 1021);

        public static readonly string ImageContentRootPath = Path.Combine(AppContext.BaseDirectory, "Images");
        public static readonly InitialIconSettings NozzleTempIconInput = new("NozzleTempIcon", 471, 1025, 1m, Path.Combine(ImageContentRootPath, "monitor_nozzle_temp.png"));
        public static readonly InitialIconSettings BedTempIconInput = new("BedTempIcon", 222, 10251, 1m, Path.Combine(ImageContentRootPath, "monitor_bed_temp.png"));
        public static readonly InitialIconSettings PartFanIconInput = new("PartFanIcon", 10, 969, 1m + (2m / 3m), Path.Combine(ImageContentRootPath, "fan_off.png"));
        public static readonly InitialIconSettings AuxFanIconInput = new("AuxFanIcon", 227, 969, 1m + (2m / 3m), Path.Combine(ImageContentRootPath, "fan_off.png"));
        public static readonly InitialIconSettings ChamberFanIconInput = new("ChamberFanIcon", 475, 969, 1m + (2m / 3m), Path.Combine(ImageContentRootPath, "fan_off.png"));
        public static readonly InitialIconSettings PreviewImageInput = new("PreviewImage", 1667, 0, 0.5m, Path.Combine(ImageContentRootPath, "preview_placeholder.png"));
        public static readonly InitialIconSettings ChamberTempIconInput = new("ChamberTempIcon", 9, 1021, 1m, Path.Combine(ImageContentRootPath, "monitor_frame_temp.png"));
        public static readonly InitialIconSettings TimeIconInput = new("TimeIcon", 1730, 1016, 1m, Path.Combine(ImageContentRootPath, "monitor_tasklist_time.png"));
        public static readonly InitialIconSettings FilamentIconInput = new("FilamentIcon", 1250, 1017, 1m, Path.Combine(ImageContentRootPath, "filament.png"));


        public const string TextInputType = "text_gdiplus_v2";
        public const string ImageInputType = "image_source";
        public const string ColorInputType = "color_source_v3";
        public const string VideoInputType = "ffmpeg_source";

        public const string FfmpegOptions = "protocol_whitelist=file,udp,rtp";

        public const int NotFoundErrorCode = 600;

        public const int VideoWidth = 1920;
        public const int VideoHeight = 1080;

        public class InitialInputSettings(
            string name,
            decimal defaultPositionX,
            decimal defaultPositionY)
        {
            public string Name { get; } = name;
            public decimal DefaultPositionX { get; } = defaultPositionX;
            public decimal DefaultPositionY { get; } = defaultPositionY;
        }

        public class InitialIconSettings(
            string name,
            decimal defaultPositionX,
            decimal defaultPositionY,
            decimal defaultScaleFactor,
            string icon) : InitialInputSettings(name, defaultPositionX, defaultPositionY)
        {
            public string Icon { get; } = icon;
            public decimal DefaultScaleFactor { get; } = defaultScaleFactor;
        }
    }
}
