using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace BambuVideoStream.Models.Mqtt;



public class PrintMessage
{
    public Print print { get; set; }
}



public class Print
{
    public Ams ams { get; set; }
    public int ams_rfid_status { get; set; }
    public int ams_status { get; set; }
    public double bed_target_temper { get; set; }
    public double bed_temper { get; set; }
    /// <summary>
    /// Aux fan
    /// </summary>
    public string big_fan1_speed { get; set; }
    /// <summary>
    /// Chamber fan
    /// </summary>
    public string big_fan2_speed { get; set; }
    public double chamber_temper { get; set; }
    public string command { get; set; }
    public string cooling_fan_speed { get; set; }
    public string fail_reason { get; set; }
    public int fan_gear { get; set; }
    public bool force_upgrade { get; set; }
    public string gcode_file { get; set; }
    public string gcode_file_prepare_percent { get; set; }
    public string gcode_start_time { get; set; }
    public string gcode_state { get; set; }
    public string heatbreak_fan_speed { get; set; }
    public List<Hms> hms { get; set; }
    public int home_flag { get; set; }
    public int hw_switch_state { get; set; }
    public int layer_num { get; set; }
    public string lifecycle { get; set; }
    public int maintain { get; set; }
    public int mc_percent { get; set; }
    public string mc_print_error_code { get; set; }
    public string mc_print_stage { get; set; }
    public int mc_print_sub_stage { get; set; }
    public int mc_remaining_time { get; set; }
    public string mess_production_state { get; set; }
    public double nozzle_target_temper { get; set; }
    public double nozzle_temper { get; set; }
    public int print_error { get; set; }
    public int print_gcode_action { get; set; }
    // TODO - I think this tracks whether filament is changing...but I'm not sure
    public int print_real_action { get; set; }
    public string print_type { get; set; }
    public string profile_id { get; set; }
    public string project_id { get; set; }
    public bool sdcard { get; set; }
    public string sequence_id { get; set; }
    public int spd_lvl { get; set; }

    /// <summary>
    /// speed magnitude/modifier %
    /// </summary>
    public int spd_mag { get; set; }
    public List<int> stg { get; set; }
    public int stg_cur { get; set; }
    public string subtask_id { get; set; }
    public string subtask_name { get; set; }
    public string task_id { get; set; }
    public int total_layer_num { get; set; }
    public string wifi_signal { get; set; }
    public string xcam_status { get; set; }



    public List<LightsReport> lights_report { get; set; }
    public Ipcam ipcam { get; set; }
    public Online online { get; set; }
    public UpgradeState upgrade_state { get; set; }
    public Upload upload { get; set; }
    public Xcam xcam { get; set; }

    // TODO: For some reason stg_cur doesn't update to status 4 when filament is changing w/ AMS...
    public PrintStage current_stage => (PrintStage)stg_cur;

    public string current_stage_str
    {
        get
        {
            if (!Enum.IsDefined(current_stage))
            {
                return "Undefined";
            }
            return typeof(PrintStage)
                .GetField(current_stage.ToString())?
                .GetCustomAttribute<DescriptionAttribute>()?
                .Description ?? stg_cur.ToString();
        }
    }


    public decimal GetFanSpeed(string fanspeedvar)
    {
        decimal fanSpeed = Convert.ToDecimal(fanspeedvar);

        var percent = Math.Round(fanSpeed / 15, 1) * 100;

        return percent;
    }

    public SpeedLevel speed_level => (SpeedLevel)spd_lvl;

    public string speed_level_str => Enum.IsDefined(speed_level) ? speed_level.ToString() : "Undefined";
}

public enum PrintStage
{
    [Description("Idle")]
    Idle = -1,
    [Description("Printing")]
    Printing = 0,
    [Description("Auto bed leveling")]
    AutoBedLeveling = 1,
    [Description("Heatbed preheating")]
    HeatbedPreheating = 2,
    [Description("Sweeping XY mech mode")]
    SweepingXYMechMode = 3,
    [Description("Changing filament")]
    ChangingFilament = 4,
    [Description("M400 pause")]
    M400Pause = 5,
    [Description("Paused due to filament runout")]
    PausedDueToFilamentRunout = 6,
    [Description("Heating hotend")]
    HeatingHotend = 7,
    [Description("Calibrating extrusion")]
    CalibratingExtrusion = 8,
    [Description("Scanning bed surface")]
    ScanningBedSurface = 9,
    [Description("Inspecting first layer")]
    InspectingFirstLayer = 10,
    [Description("Identifying build plate type")]
    IdentifyingBuildPlateType = 11,
    [Description("Calibrating Micro Lidar")]
    CalibratingMicroLidar = 12,
    [Description("Homing toolhead")]
    HomingToolhead = 13,
    [Description("Cleaning nozzle tip")]
    CleaningNozzleTip = 14,
    [Description("Checking extruder temperature")]
    CheckingExtruderTemperature = 15,
    [Description("Printing was paused by the user")]
    PrintingWasPausedByTheUser = 16,
    [Description("Pause of front cover falling")]
    PauseOfFrontCoverFalling = 17,
    [Description("Calibrating the micro lidar")]
    CalibratingTheMicroLidar = 18,
    [Description("Calibrating extrusion flow")]
    CalibratingExtrusionFlow = 19,
    [Description("Paused due to nozzle temperature malfunction")]
    PausedDueToNozzleTemperatureMalfunction = 20,
    [Description("Paused due to heat bed temperature malfunction")]
    PausedDueToHeatBedTemperatureMalfunction = 21
}

public enum SpeedLevel
{
    Silent = 1,
    Standard = 2,
    Sport = 3,
    Ludicrous = 4
}