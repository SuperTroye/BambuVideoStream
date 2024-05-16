using Newtonsoft.Json.Linq;

namespace BambuVideoStream.Models;

public class OBSSettings
{
    public string WsConnection { get; set; }
    public string WsPassword { get; set; }
    public string BambuScene { get; set; }
    public string BambuStreamSource { get; set; }
    public bool StartStreamOnStartup { get; set; }
    public bool StopStreamOnPrintComplete { get; set; }
    public bool ForceCreateInputs { get; set; }
    public bool LockInputs { get; set; }

    public string TextInputType { get; set; }
    public string ImageInputType { get; set; }
    public string ColorInputType { get; set; }
    public string VideoInputType { get; set; }

    public string FfmpegOptions { get; set; }

    public OutputDef Output { get; set; }

    public class OutputDef
    {
        public decimal VideoWidth { get; set; }
        public decimal VideoHeight { get; set; }
    }

    public class InputDef
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public JObject Data { get; set; }
        public JObject Transform { get; set; }
    }
}
