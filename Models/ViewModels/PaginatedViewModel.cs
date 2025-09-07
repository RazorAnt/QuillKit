namespace QuillKit.Models.ViewModels;

public class PaginatedViewModel<T>
{
    public PaginatedViewModel(List<T> items, int currentPage, int totalPages, int totalItems, int pageSize)
    {
        Items = items;
        CurrentPage = currentPage;
        TotalPages = totalPages;
        TotalItems = totalItems;
        PageSize = pageSize;
    }

    public List<T> Items { get; }
    public int CurrentPage { get; }
    public int TotalPages { get; }
    public int TotalItems { get; }
    public int PageSize { get; }
    
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool ShowPagination => TotalItems > PageSize;
    
    public int PreviousPage => HasPreviousPage ? CurrentPage - 1 : 1;
    public int NextPage => HasNextPage ? CurrentPage + 1 : TotalPages;
}
