using System;
using System.Collections.Generic;

namespace OBSProject
{
    public class Xcam
    {
        public bool allow_skip_parts { get; set; }
        public bool buildplate_marker_detector { get; set; }
        public bool first_layer_inspector { get; set; }
        public string halt_print_sensitivity { get; set; }
        public bool print_halt { get; set; }
        public bool printing_monitor { get; set; }
        public bool spaghetti_detector { get; set; }
    }
}