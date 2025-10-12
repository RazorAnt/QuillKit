using QuillKit.Models;

namespace QuillKit.Services;

public interface IPostService
{
    Task<List<Post>> GetAllPostsAsync();
    Task<List<Post>> GetPublishedPostsAsync(int page = 1, int pageSize = 5, bool includeDrafts = false);
    Task<int> GetPublishedPostsCountAsync(bool includeDrafts = false);
    Task<Post?> GetPostBySlugAsync(string slug, bool includeDrafts = false);
    Task<Post?> GetPostByFileNameAsync(string fileName);
    Task<List<Post>> GetPostsByCategoryAsync(string category);
    Task<List<Post>> GetPostsByTagAsync(string tag);
    Task<List<Post>> GetPostsByAuthorAsync(string author);
    Task<List<Post>> SearchPostsAsync(string searchTerm);
    Task<Post> SavePostAsync(Post post);
    Task DeletePostAsync(string slug);
    Task<List<string>> GetAllCategoriesAsync();
    Task<List<string>> GetAllTagsAsync();
    Task<List<string>> GetAllAuthorsAsync();
    Task<List<string>> GetMediaFilesAsync();
    Task ReloadPostsAsync();
    Task<Dictionary<string, string>> GetParseErrorsAsync();
    Task<string?> GetRawFileContentAsync(string fileName);
    Task SaveRawFileAsync(string fileName, string content);
}
