


export interface PrintMessage {
  print: Print;
}


export interface Print {
  ams: Ams;
  ams_rfid_status: number;
  ams_status: number;
  bed_target_temper: number;
  bed_temper: number;
  big_fan1_speed: string;
  big_fan2_speed: string;
  chamber_temper: number;
  command: string;
  cooling_fan_speed: string;
  fail_reason: string;
  fan_gear: number;
  force_upgrade: boolean;
  gcode_file: string;
  gcode_file_prepare_percent: string;
  gcode_start_time: string;
  gcode_state: string;
  heatbreak_fan_speed: string;
  hms?: (null)[] | null;
  home_flag: number;
  hw_switch_state: number;
  ipcam: Ipcam;
  layer_num: number;
  lifecycle: string;
  lights_report?: (LightsReportEntity)[] | null;
  maintain: number;
  mc_percent: number;
  mc_print_error_code: string;
  mc_print_stage: string;
  mc_print_sub_stage: number;
  mc_remaining_time: number;
  mess_production_state: string;
  nozzle_target_temper: number;
  nozzle_temper: number;
  online: Online;
  print_error: number;
  print_gcode_action: number;
  print_real_action: number;
  print_type: string;
  profile_id: string;
  project_id: string;
  sdcard: boolean;
  sequence_id: string;
  spd_lvl: number;
  spd_mag: number;
  stg?: (null)[] | null;
  stg_cur: number;
  subtask_id: string;
  subtask_name: string;
  task_id: string;
  total_layer_num: number;
  upgrade_state: UpgradeState;
  upload: Upload;
  wifi_signal: string;
  xcam: Xcam;
  xcam_status: string;
  current_stage: string;
}


export interface Ams {
  ams?: (AmsEntity)[] | null;
  ams_exist_bits: string;
  insert_flag: boolean;
  power_on_flag: boolean;
  tray_exist_bits: string;
  tray_is_bbl_bits: string;
  tray_now: string;
  tray_read_done_bits: string;
  tray_reading_bits: string;
  tray_tar: string;
  version: number;
}


export interface AmsEntity {
  humidity: string;
  id: string;
  temp: string;
  tray?: (TrayEntity)[] | null;
}


export interface TrayEntity {
  bed_temp: string;
  bed_temp_type: string;
  drying_temp: string;
  drying_time: string;
  id: string;
  nozzle_temp_max: string;
  nozzle_temp_min: string;
  remain: number;
  tag_uid: string;
  tray_color: string;
  tray_diameter: string;
  tray_id_name: string;
  tray_info_idx: string;
  tray_sub_brands: string;
  tray_type: string;
  tray_uuid: string;
  tray_weight: string;
  xcam_info: string;
}


export interface Ipcam {
  ipcam_dev: string;
  ipcam_record: string;
  resolution: string;
  timelapse: string;
}


export interface LightsReportEntity {
  mode: string;
  node: string;
}


export interface Online {
  ahb: boolean;
  rfid: boolean;
}


export interface UpgradeState {
  ahb_new_version_number: string;
  ams_new_version_number: string;
  consistency_request: boolean;
  dis_state: number;
  err_code: number;
  force_upgrade: boolean;
  message: string;
  module: string;
  new_version_state: number;
  ota_new_version_number: string;
  progress: string;
  sequence_id: number;
  status: string;
}


export interface Upload {
  file_size: number;
  finish_size: number;
  message: string;
  oss_url: string;
  progress: number;
  sequence_id: string;
  speed: number;
  status: string;
  task_id: string;
  time_remaining: number;
  trouble_id: string;
}


export interface Xcam {
  allow_skip_parts: boolean;
  buildplate_marker_detector: boolean;
  first_layer_inspector: boolean;
  halt_print_sensitivity: string;
  print_halt: boolean;
  printing_monitor: boolean;
  spaghetti_detector: boolean;
}
