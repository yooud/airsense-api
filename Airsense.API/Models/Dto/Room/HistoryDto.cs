namespace Airsense.API.Models.Dto.Room;

public class HistoryDto
{
    public object Data { get; set; }
    
    public HistoryMetadata Metadata { get; set; }
    
    public class HistoryMetadata
    {
        public long From { get; set; }

        public long To { get; set; }

        public string Interval { get; set; }
    }
}