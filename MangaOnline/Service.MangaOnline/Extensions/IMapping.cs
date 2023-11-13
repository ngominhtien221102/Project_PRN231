using Service.MangaOnline.Models;
using Service.MangaOnline.ResponseModels;

namespace Service.MangaOnline.Extensions;

public interface IMapping
{ 
    MangaResponse MapMangaResponse(Manga manga);
    
    ChapterResponse MapChapterResponse(Chaptere manga);

    UserResponse MapUserResponse(User user);
}