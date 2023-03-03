using System;
using System.Collections.Generic;

namespace BambuVideoStream
{
    public class Ipcam
    {
        /// <summary>
        /// status
        /// </summary>
        public string ipcam_dev { get; set; }
        public string ipcam_record { get; set; }
        public string resolution { get; set; }
        public string timelapse { get; set; }


        public string GetIPCamInfo
        {
            get 
            {
                switch (ipcam_dev)
                {
                    case "1":
                        return "On";
                    case "0":
                        return "Off";
                    default:
                        return ipcam_dev;
                }
            }
        }


    }
}
