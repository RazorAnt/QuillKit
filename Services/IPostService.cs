using QuillKit.Models;

namespace QuillKit.Services;

public interface IPostService
{
    Task<List<Post>> GetAllPostsAsync();
    Task<List<Post>> GetPublishedPostsAsync(int page = 1, int pageSize = 5);
    Task<int> GetPublishedPostsCountAsync();
    Task<Post?> GetPostBySlugAsync(string slug);
    Task<Post?> GetPostByFileNameAsync(string fileName);
    Task<List<Post>> GetPostsByCategoryAsync(string category);
    Task<List<Post>> GetPostsByTagAsync(string tag);
    Task<List<Post>> GetPostsByAuthorAsync(string author);
    Task<Post> SavePostAsync(Post post);
    Task DeletePostAsync(string slug);
    Task<List<string>> GetAllCategoriesAsync();
    Task<List<string>> GetAllTagsAsync();
    Task<List<string>> GetAllAuthorsAsync();
    Task<List<string>> GetMediaFilesAsync();
    Task ReloadPostsAsync();
}
