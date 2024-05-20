namespace BambuVideoStream.Models.Mqtt;

public class McPrintMessage
{
    public McPrint mc_print { get; set; }
}

public class McPrint
{
    public string command { get; set; }
    public string param { get; set; }
    public string sequence_id { get; set; }
}
