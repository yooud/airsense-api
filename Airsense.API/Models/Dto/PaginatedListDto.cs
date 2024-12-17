namespace Airsense.API.Models.Dto;

public class PaginatedListDto
{
    public object Data { get; set; }

    public Metadata Pagination { get; set; }

    public class Metadata
    {
        public int Total { get; set; }

        public int Skip { get; set; }

        public int Count { get; set; }
    }
}