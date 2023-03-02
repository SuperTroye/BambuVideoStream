using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace OBSProject
{


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




        public string current_stage
        {
            get
            {
                switch (stg_cur)
                {
                    case -1:
                        return "Idle";
                    case 0:
                        return "Printing";
                    case 1:
                        return "Auto bed leveling";
                    case 2:
                        return "Heatbed preheating";
                    case 3:
                        return "Sweeping XY mech mode";
                    case 4:
                        return "Changing filament";
                    case 5:
                        return "M400 pause";
                    case 6:
                        return "Paused due to filament runout";
                    case 7:
                        return "Heating hotend";
                    case 8:
                        return "Calibrating extrusion";
                    case 9:
                        return "Scanning bed surface";
                    case 10:
                        return "Inspecting first layer";
                    case 11:
                        return "Identifying build plate type";
                    case 12:
                        return "Calibrating Micro Lidar";
                    case 13:
                        return "Homing toolhead";
                    case 14:
                        return "Cleaning nozzle tip";
                    case 15:
                        return "Checking extruder temperature";
                    case 16:
                        return "Printing was paused by the user";
                    case 17:
                        return "Pause of front cover falling";
                    case 18:
                        return "Calibrating the micro lidar";
                    case 19:
                        return "Calibrating extrusion flow";
                    case 20:
                        return "Paused due to nozzle temperature malfunction";
                    case 21:
                        return "Paused due to heat bed temperature malfunction";
                    default:
                        return stg_cur.ToString();
                }
            }
        }



        public decimal GetFanSpeed(string fanspeedvar)
        {
            decimal fanSpeed = Convert.ToDecimal(fanspeedvar);

            var percent = Math.Round(fanSpeed / 15, 1) * 100;

            return percent;
        }



        public string GetSpeedLevel
        {
            get
            {
                switch (spd_lvl)
                {
                    case 1:
                        return "Silent";
                    case 2:
                        return "Standard";
                    case 3:
                        return "Sport";
                    case 4:
                        return "Ludicrous";
                    default:
                        return "Undefined";
                }
            }
        }

    }


}
