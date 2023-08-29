using System.Data.Entity;

namespace OHotel.NETCoreMVC.Helper
{
    public class PaginatedList<T> : List<T>
    {
        //當前頁數
        public int PageIndex { get; private set; }
        //總頁數
        public int TotalPages { get; private set; }
        //總比數
        public int TotalCount { get; private set; }
        public PaginatedList(List<T> items, int count, int pageIndex, int PageAmount)
        {
            PageIndex = pageIndex;//當前頁數
            TotalCount = count;
            TotalPages = (int)Math.Ceiling(count / (double)PageAmount);//計算種頁數=(數量/每頁幾筆)
            this.AddRange(items);
        }
        //--判斷是否有上一頁
        public bool HasPreviousPage
        {
            get
            {
                return (PageIndex > 1);
            }
        }
        //--判斷是否有下一頁
        public bool HasNextPage
        {
            get
            {
                return (PageIndex < TotalPages);
            }
        }

        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source, int pageIndex, int PageAmount)
        {
            var count = await source.CountAsync();
            //Skip 和 Take 陳述式來篩選伺服器上的資料，而不會擷取資料表中的所有資料列
            //Skip: 跳過來源序列中的N個項目，再把剩下的資料全部回傳。
            //Take: 取來源序列中N的項目
            //所以 Skip(10).Take(5) ,跳過前10筆序列項目回傳,剩下序列項目取前五筆
            var items = await source.Skip((pageIndex - 1) * PageAmount).Take(PageAmount).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, PageAmount);
        }

        public static PaginatedList<T> Create(
            IQueryable<T> source, int pageIndex, int PageAmount)
        {
            var count = source.Count();
            //Skip 和 Take 陳述式來篩選伺服器上的資料，而不會擷取資料表中的所有資料列
            //Skip: 跳過來源序列中的N個項目，再把剩下的資料全部回傳。
            //Take: 取來源序列中N的項目
            //所以 Skip(10).Take(5) ,跳過前10筆序列項目回傳,剩下序列項目取前五筆
            var items = source.Skip((pageIndex - 1) * PageAmount).Take(PageAmount).ToList();
            return new PaginatedList<T>(items, count, pageIndex, PageAmount);
        }
        public static PaginatedList<T> CreateIEnumerableAsync(IEnumerable<T> source, int pageIndex, int PageAmount)
        {
            var count = source.Count();
            //Skip 和 Take 陳述式來篩選伺服器上的資料，而不會擷取資料表中的所有資料列
            //Skip: 跳過來源序列中的N個項目，再把剩下的資料全部回傳。
            //Take: 取來源序列中N的項目
            //所以 Skip(10).Take(5) ,跳過前10筆序列項目回傳,剩下序列項目取前五筆
            var items = source.Skip((pageIndex - 1) * PageAmount).Take(PageAmount).ToList();
            return new PaginatedList<T>(items, count, pageIndex, PageAmount);
        }
    }
}
