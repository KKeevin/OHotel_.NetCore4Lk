namespace OHotel.NETCoreMVC.Models
{
    public class NewsType
    {
        public int NTNo { get; set; } = 0;
        public string TName { get; set; } = "";
        public string CTime { get; set; } = "";
    }

    public partial class NewsTypePaginated : StatusMessage
    {
        public ICollection<NewsType>? Result { get; set; }
        public int PageIndex { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
    }
}
