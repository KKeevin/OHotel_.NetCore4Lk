using Microsoft.EntityFrameworkCore;

namespace OHotel.NETCoreMVC.Helper
{
    /// <summary>分頁清單</summary>
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; }
        public int TotalPages { get; }
        public int TotalCount { get; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedList(List<T> items, int count, int pageIndex, int pageAmount)
        {
            PageIndex = pageIndex;
            TotalCount = count;
            TotalPages = (int)Math.Ceiling(count / (double)pageAmount);
            AddRange(items);
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageAmount)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageAmount).Take(pageAmount).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageAmount);
        }

        public static PaginatedList<T> Create(IQueryable<T> source, int pageIndex, int pageAmount)
        {
            var count = source.Count();
            var items = source.Skip((pageIndex - 1) * pageAmount).Take(pageAmount).ToList();
            return new PaginatedList<T>(items, count, pageIndex, pageAmount);
        }

        public static PaginatedList<T> Create(IEnumerable<T> source, int pageIndex, int pageAmount)
        {
            var list = source.ToList();
            var count = list.Count;
            var items = list.Skip((pageIndex - 1) * pageAmount).Take(pageAmount).ToList();
            return new PaginatedList<T>(items, count, pageIndex, pageAmount);
        }
    }
}
