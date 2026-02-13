namespace oamswlatifose.Server.Services
{
    /// <summary>
    /// Standardized paginated result wrapper for list operations.
    /// Provides metadata about the paginated dataset.
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Collection of items for the current page
        /// </summary>
        public IEnumerable<T> Items { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Current page number (1-indexed)
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// First item index on current page (1-indexed)
        /// </summary>
        public int FirstItemIndex => (PageNumber - 1) * PageSize + 1;

        /// <summary>
        /// Last item index on current page (1-indexed)
        /// </summary>
        public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalCount);
    }
}
