namespace BambuVideoStream.Models.Mqtt;

public class Upload
{
    public int file_size { get; set; }
    public int finish_size { get; set; }
    public string message { get; set; }
    public string oss_url { get; set; }
    public int progress { get; set; }
    public string sequence_id { get; set; }
    public int speed { get; set; }
    public string status { get; set; }
    public string task_id { get; set; }
    public int time_remaining { get; set; }
    public string trouble_id { get; set; }
}
