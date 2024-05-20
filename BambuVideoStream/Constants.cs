using System.IO;
using System;

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

        #region Text inputs

        public static readonly InitialTextSettings ChamberTempInitialSettings =
            new("ChamberTemp",
                defaultPositionX: 71,
                defaultPositionY: 1029);
        public static readonly InitialTextSettings BedTempInitialSettings =
            new("BedTemp",
                defaultPositionX: 277,
                defaultPositionY: 1029);
        public static readonly InitialTextSettings TargetBedTempInitialSettings =
            new("TargetBedTemp",
                defaultPositionX: 313,
                defaultPositionY: 1029);
        public static readonly InitialTextSettings NozzleTempInitialSettings =
            new("NozzleTemp",
                defaultPositionX: 527,
                defaultPositionY: 1028);
        public static readonly InitialTextSettings TargetNozzleTempInitialSettings =
            new("TargetNozzleTemp",
                defaultPositionX: 580,
                defaultPositionY: 1028);
        public static readonly InitialTextSettings PercentCompleteInitialSettings =
            new("PercentComplete",
                defaultPositionX: 1510,
                defaultPositionY: 1022);
        public static readonly InitialTextSettings LayersInitialSettings =
            new("Layers",
                defaultPositionX: 1652,
                defaultPositionY: 972);
        public static readonly InitialTextSettings TimeRemainingInitialSettings =
            new("TimeRemaining",
                defaultPositionX: 1791,
                defaultPositionY: 1024);
        public static readonly InitialTextSettings SubtaskNameInitialSettings =
            new("SubtaskName",
                defaultPositionX: 838,
                defaultPositionY: 971);
        public static readonly InitialTextSettings StageInitialSettings =
            new("Stage",
                defaultPositionX: 842,
                defaultPositionY: 1019);
        public static readonly InitialTextSettings PartFanInitialSettings =
            new("PartFan",
                defaultPositionX: 58,
                defaultPositionY: 971);
        public static readonly InitialTextSettings AuxFanInitialSettings =
            new("AuxFan",
                defaultPositionX: 277,
                defaultPositionY: 971);
        public static readonly InitialTextSettings ChamberFanInitialSettings =
            new("ChamberFan",
                defaultPositionX: 521,
                defaultPositionY: 971);
        public static readonly InitialTextSettings FilamentInitialSettings =
            new("Filament",
                defaultPositionX: 1437,
                defaultPositionY: 1022);
        public static readonly InitialTextSettings PrintWeightInitialSettings =
            new("PrintWeight",
                defaultPositionX: 1303,
                defaultPositionY: 1021);

        #endregion

        #region Icon inputs

        public static readonly string ImageDir = Path.Combine(Path.Combine(AppContext.BaseDirectory, "Images"));
        private static string GetPath(string filename) => Path.Combine(ImageDir, filename);
        public static readonly InitialToggleIconSettings NozzleTempIconInitialSettings =
            new("NozzleTempIcon",
                defaultPositionX: 471,
                defaultPositionY: 1025,
                defaultScaleFactor: 1m,
                icon_off: GetPath("monitor_nozzle_temp.png"),
                icon_on: GetPath("monitor_nozzle_temp_active.png"));
        public static readonly InitialToggleIconSettings BedTempIconInitialSettings =
            new("BedTempIcon",
                defaultPositionX: 222,
                defaultPositionY: 10251,
                defaultScaleFactor: 1m,
                icon_off: GetPath("monitor_bed_temp.png"),
                icon_on: GetPath("monitor_bed_temp_active.png"));
        public static readonly InitialToggleIconSettings PartFanIconInitialSettings =
            new("PartFanIcon",
                defaultPositionX: 10,
                defaultPositionY: 969,
                defaultScaleFactor: 1m + (2m / 3m),
                icon_off: GetPath("fan_off.png"),
                icon_on: GetPath("fan_icon.png"));
        public static readonly InitialToggleIconSettings AuxFanIconInitialSettings =
            new("AuxFanIcon",
                defaultPositionX: 227,
                defaultPositionY: 969,
                defaultScaleFactor: 1m + (2m / 3m),
                icon_off: GetPath("fan_off.png"),
                icon_on: GetPath("fan_icon.png"));
        public static readonly InitialToggleIconSettings ChamberFanIconInitialSettings =
            new("ChamberFanIcon",
                defaultPositionX: 475,
                defaultPositionY: 969,
                defaultScaleFactor: 1m + (2m / 3m),
                icon_off: GetPath("fan_off.png"),
                icon_on: GetPath("fan_icon.png"));
        public static readonly InitialToggleIconSettings PreviewImageInitialSettings =
            new("PreviewImage",
                defaultPositionX: 1667,
                defaultPositionY: 0,
                defaultScaleFactor: 0.5m,
                icon_off: GetPath("preview_placeholder.png"),
                icon_on: GetPath("preview.png")); // Created at runtime
        public static readonly InitialIconSettings ChamberTempIconInitialSettings =
            new("ChamberTempIcon",
                defaultPositionX: 9,
                defaultPositionY: 1021,
                defaultScaleFactor: 1m,
                icon: GetPath("monitor_frame_temp.png"));
        public static readonly InitialIconSettings TimeIconInitialSettings =
            new("TimeIcon",
                defaultPositionX: 1730,
                defaultPositionY: 1016,
                defaultScaleFactor: 1m,
                icon: GetPath("monitor_tasklist_time.png"));
        public static readonly InitialIconSettings FilamentIconInitialSettings =
            new("FilamentIcon",
                defaultPositionX: 1250,
                defaultPositionY: 1017,
                defaultScaleFactor: 1m,
                icon: GetPath("filament.png"));

        #endregion

        public class InitialTextSettings(
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
            string icon) : InitialTextSettings(name, defaultPositionX, defaultPositionY)
        {
            public string DefaultIconPath { get; } = icon;
            public decimal DefaultScaleFactor { get; } = defaultScaleFactor;
        }

        public class InitialToggleIconSettings(
            string name,
            decimal defaultPositionX,
            decimal defaultPositionY,
            decimal defaultScaleFactor,
            string icon_off,
            string icon_on) : InitialIconSettings(name, defaultPositionX, defaultPositionY, defaultScaleFactor, icon_off)
        {
            public string DefaultEnabledIconPath { get; } = icon_on;
        }
    }
}
