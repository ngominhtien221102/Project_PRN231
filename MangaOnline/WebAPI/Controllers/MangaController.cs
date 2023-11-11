using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using Service.MangaOnline.Commons;
using Service.MangaOnline.Extensions;
using Service.MangaOnline.FilterPermissions;
using Service.MangaOnline.Models;
using Service.MangaOnline.RequestModels;
using Service.MangaOnline.ResponseModels;
using System.Globalization;

namespace Service.MangaOnline.Controllers;

[ApiController]
[Route("[controller]")]
public class MangaController : ODataController
{
    private readonly MangaOnlineV1DevContext _context;
    private readonly IExtensionManga _extensionManga;
    private readonly IMapObject _map;
    
    public MangaController(MangaOnlineV1DevContext mangaOnlineV1DevContext,
        IExtensionManga extensionManga, IMapObject mapObject)
    {
        _context = mangaOnlineV1DevContext;
        _extensionManga = extensionManga;
        _map = mapObject;

    }

    [EnableQuery]
    [HttpGet("list")]
    public IActionResult List()
    {
        var res = _context.Mangas
             .Include(x => x.CategoryMangas)
             .ThenInclude(x => x.Category)
             .Include(x => x.Chapteres)
             .Include(x => x.Author)
             .Where(x=> x.IsActive == true)
             .ToList();
        return Ok(res.Select(x => _map.MapMangaResponse(x)).ToList().AsQueryable());
    }

    [HttpGet("HomeManga")]
    public IActionResult HomeManga()
    {
        // topMonthManga
        var topMonthManga = _context.Mangas
            .Include(x => x.CategoryMangas)
            .ThenInclude(x => x.Category)
            .Include(x => x.Chapteres)
            .Include(x => x.Author)
            .Where(x => x.ModifiedAt!.Value.Month == DateTime.Now.Month
                        && x.ModifiedAt!.Value.Year == DateTime.Now.Year && x.IsActive == true)
            .OrderByDescending(x => x.Star).Skip(0).Take(8).ToList();
        if (topMonthManga.Count <= 4)
        {
            topMonthManga = _context.Mangas
                .Include(x => x.CategoryMangas)
                .ThenInclude(x => x.Category)
                .Include(x => x.Chapteres)
                .Include(x => x.Author)
                .Where(x => x.ModifiedAt!.Value.Year == DateTime.Now.Year && x.IsActive == true)
                .OrderByDescending(x => x.Star).Skip(0).Take(8).ToList();
        }

        var newUpdateMangas = _context.Mangas
            .Include(x => x.CategoryMangas)
            .ThenInclude(x => x.Category)
            .Include(x => x.Chapteres)
            .Include(x => x.Author)
            .Where(x=> x.IsActive == true)
            .OrderByDescending(x => x.ModifiedAt).Skip(0).Take(12).ToList();
        var newDoneMangas = _context.Mangas
            .Include(x => x.CategoryMangas)
            .ThenInclude(x => x.Category)
            .Include(x => x.Chapteres)
            .Include(x => x.Author)
            .Where(x => x.Status == (int)MangaStatus.Done && x.IsActive == true)
            .OrderByDescending(x => x.ModifiedAt).Skip(0).Take(12).ToList();
        var topViewMangas = _context.Mangas
            .Include(x => x.CategoryMangas)
            .ThenInclude(x => x.Category)
            .Include(x => x.Chapteres)
            .Include(x => x.Author)
            .Where(x=> x.IsActive == true)
            .OrderByDescending(x => x.ViewCount).Skip(0).Take(6).ToList();
        return Ok(new HomeResponse
        {
            TopMonthManga = topMonthManga.Select(x => _map.MapMangaResponse(x)).ToList(),
            NewUpdateMangas = newUpdateMangas.Select(x => _map.MapMangaResponse(x)).ToList(),
            NewDoneMangas = newDoneMangas.Select(x => _map.MapMangaResponse(x)).ToList(),
            TopViewMangas = topViewMangas.Select(x => _map.MapMangaResponse(x)).ToList()
        });
    }

    [HttpPost("CreateManga")]
    [FilterPermission(Action = ActionFilterEnum.CreateManga)]
    public IActionResult CreateManga([FromForm] AddMangaRequest request)
    {
        // add Manga
        try
        {
            var author = new Author()
            {
                Id = Guid.NewGuid(),
                Name = request.AuthorName
            };
            _context.Authors.Add(author);
            DateTime utcTime1 = new DateTime(request.CreatedAt, 1, 1);
            utcTime1 = DateTime.SpecifyKind(utcTime1, DateTimeKind.Utc);
            DateTimeOffset utcTime2 = utcTime1;
            var manga = new Manga
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                AuthorId = author.Id,
                ViewCount = 0,
                RateCount = 0,
                Star = 0,
                FollowCount = 0,
                Description = request.Description,
                CreatedAt = utcTime2,
                ModifiedAt = DateTimeOffset.Now,
                IsActive = request.IsActive,
                Status = request.Status,
                Image = request.Image
            };
            foreach (var categoryId in request.CategoriesId)
            {
                _context.CategoryMangas
                    .Add(new CategoryManga() { CategoryId = categoryId, MangaId = manga.Id });
            }

            _context.Mangas.Add(manga);
            _context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPut("UpdateManga")]
    [FilterPermission(Action = ActionFilterEnum.UpdateManga)]
    public IActionResult UpdateManga([FromForm] UpdateMangaRequest request)
    {
        // update manga
        var mangaOld =
            _context.Mangas
                .Include(x => x.Author)
                .FirstOrDefault(x => x.Id == request.MangaId);
        if (mangaOld != null)
        {
            mangaOld.Name = request.Name ?? mangaOld.Name;
            mangaOld.Author.Name = request.AuthorName ?? mangaOld.Author.Name;
            mangaOld.Description = request.Description ?? mangaOld.Description;
            mangaOld.IsActive = request.IsActive ?? mangaOld.IsActive;

            if (request.CategoriesId != null)
            {
                var categoryMangasOld =
                    _context.CategoryMangas.Where(x => x.MangaId == request.MangaId);
                _context.CategoryMangas.RemoveRange(categoryMangasOld);
                foreach (var categoryId in request.CategoriesId)
                {
                    _context.CategoryMangas
                        .Add(new CategoryManga { CategoryId = categoryId, MangaId = mangaOld.Id });
                }
            }

            if (request.CreatedAt != null)
            {
                DateTime utcTime1 = new DateTime((int)request.CreatedAt, 1, 1);
                utcTime1 = DateTime.SpecifyKind(utcTime1, DateTimeKind.Utc);
                DateTimeOffset utcTime2 = utcTime1;
                mangaOld.CreatedAt = utcTime2;
            }

            mangaOld.ModifiedAt = DateTimeOffset.Now;
            mangaOld.Image = request.Image ?? mangaOld.Image;
            mangaOld.Status = request.Status??1;
            _context.SaveChanges();
            return Ok();
        }

        return NotFound();
    }

    [HttpDelete("DeleteManga")]
    [FilterPermission(Action = ActionFilterEnum.DeleteManga)]
    public IActionResult DeleteManga([FromForm] Guid id)
    {
        var categoryManga = _context.CategoryMangas.Where(x => x.MangaId == id).ToList();
        if (categoryManga.Count > 0)
        {
            _context.RemoveRange(categoryManga);
        }

        var manga = _context.Mangas.FirstOrDefault(x => x.Id == id);
        if (manga != null)
        {
            _context.Mangas.Remove(manga);
            _context.SaveChanges();
            return Ok();
        }

        return BadRequest();
    }

    [HttpGet("GetManga")]
    public async Task<IActionResult> GetManga(Guid id)
    {
        var manga = await _context.Mangas
            .Include(x => x.Author)
            .Include(x => x.CategoryMangas)
            .ThenInclude(x => x.Category)
            .Include(x => x.Chapteres)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (manga != null)
        {
            manga.ViewCount++;
            _context.SaveChanges();
            return Ok(new DataMangaResponse
            {
                status = 200,
                success = true,
                data = _map.MapMangaResponse(manga)
            });
        }

        return NotFound();
    }

    [HttpGet("FollowManga")]
    public IActionResult FollowMangaList(Guid userId)
    {
        var ListFollow = _context.FollowLists.Where(x => x.UserId == userId).ToList();
        var list = _context.Mangas
            .Include(x => x.CategoryMangas)
            .ThenInclude(x => x.Category)
            .Include(x => x.Chapteres)
            .Include(x => x.Author)
            .Include(x => x.FollowLists)
            .Where(x => x.FollowLists.FirstOrDefault(y => y.UserId == userId) != null).ToList();
        return Ok(new FollowResponse { list = list.Select(x => _map.MapMangaResponse(x)).ToList() });
    }

    [HttpPost("FollowManga")]
    public IActionResult FollowMangaPost(Guid userId, Guid mangaId)
    {
        var manga = _context.Mangas.FirstOrDefault(x => x.Id == mangaId);
        var id = Guid.NewGuid();
        _context.FollowLists.Add(new FollowList
        {
            Id = id,
            UserId = userId,
            MangaId = mangaId
        });
        manga.FollowCount++;
        _context.SaveChanges();
        return Ok();
    }


    [HttpDelete("FollowManga")]
    public IActionResult FollowMangaDel(Guid userId, Guid mangaId)
    {
        try
        {
            var manga = _context.Mangas.FirstOrDefault(x => x.Id == mangaId);
            var followList = _context.FollowLists.FirstOrDefault(x => x.UserId == userId && x.MangaId == mangaId);
            manga.FollowCount--;
            _context.FollowLists.Remove(followList);
            _context.SaveChanges();
            return Ok();
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet("CheckFollowManga")]
    public IActionResult CheckFollowMangaDel(Guid userId, Guid mangaId)
    {
        try
        {
            var manga = _context.Mangas.FirstOrDefault(x => x.Id == mangaId);
            var follow = _context.FollowLists.FirstOrDefault(x => x.UserId == userId && x.MangaId == mangaId);
            if (follow is not null)
            {
                return Ok(new { 
                 code=200,
                 data=true,
                });
            }
            else
            {
                return Ok(new { 
                  code = 200,
                  data = false,
                });
            }
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpPost("AddChapter")]
    public IActionResult AddChapter([FromForm] int ChapterNumber, [FromForm] int SubId, [FromForm] Guid MangaId,
        [FromForm] string Name, [FromForm] int Status, [FromForm] bool IsActive, [FromForm] string FilePDF)
    {
        try
        {
            var chaptere = new Chaptere
            {
                ChapterNumber = ChapterNumber,
                Id = Guid.NewGuid(),
                SubId = SubId,
                MangaId = MangaId,
                Name = Name,
                CreatedAt = DateTimeOffset.Now,
                Status = Status,
                IsActive = IsActive,
                FilePdf = FilePDF
            };
            _context.Chapteres.Add(chaptere);
            _context.SaveChanges();
            return Ok();
        }
        catch
        {
            return NotFound();
        }
    }
    
    [HttpGet("GetChapter")]
    [FilterPermission(Action = ActionFilterEnum.GetChapter)]
    public IActionResult GetChapter(Guid id,string ipAddress)
    {
        var chapter = _context.Chapteres
            .FirstOrDefault(x => x.Id == id);
        
        if (chapter != null)
        {
            var roleUser = HttpContext.Session.GetString("RoleUser");
            UserRoleEnum userRole;
            Enum.TryParse(roleUser, out userRole);
            if (chapter.Status == (int)ChapterEnum.Vip)
            {
                if (userRole != UserRoleEnum.Admin 
                    && userRole != UserRoleEnum.UserVip 
                    && ipAddress != HttpContext.Session.GetString("IpAddress"))
                {
                    return NotFound();
                }
            }
            var manga = _context.Mangas.FirstOrDefault(x => x.Id == chapter.MangaId);
            if (manga!=null)
            {
                manga.ViewCount++;
                _context.SaveChanges();
            }
            return Ok(new
            {
                status = 200,
                success = true,
                data = chapter.FilePdf
            });
        }
        return NotFound();
    }
    
    [HttpGet("FollowMangaId")]
    public IActionResult FollowMangaIdList(Guid? userId)
    {
        if (userId is not null) {
            var list = _context.Mangas
                 .Include(x => x.FollowLists)
                 .Where(x => x.FollowLists.FirstOrDefault(y => y.UserId == userId) != null)
                 .Select(x => x.Id.ToString()).ToList();
            return Ok(list);
        }
     return BadRequest();
    }

    [HttpGet("CheckRating")]
    public IActionResult CheckRate(Guid? userId, Guid mangaId, int rate)
    {
        try
        {
            var manga = _context.Mangas.FirstOrDefault(x => x.Id == mangaId);
            var track = _context.IpUserVotes.FirstOrDefault(x => x.UserId == userId && x.MangaId == mangaId);
            if (track is not null)
            {
                return Ok(new
                {
                    code = 200,
                    data = true,
                    value = track.Rate,
                });
            }
            else
            {
                return Ok(new
                {
                    code = 200,
                    data = false,
                });
            }
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet("Rating")]
    public IActionResult Rate(Guid? userId, Guid mangaId, int rate)
    {
        try
        {
            var manga = _context.Mangas.FirstOrDefault(x => x.Id == mangaId);
            var _track = _context.IpUserVotes.FirstOrDefault(x => x.UserId == userId && x.MangaId == mangaId);
          /*  if (_track is not null) {
                manga.Star = (manga.Star * manga.RateCount - _track.Rate + rate) / manga.RateCount;
                _track.Rate = rate;
            }
            else {
                IpUserVote track = new IpUserVote();
                track.UserId = userId;
                track.MangaId = mangaId;
                track.Rate = rate;
            }*/
            return Ok();
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet("Comment")]
    public IActionResult Comment(Guid mangaId)
    {
        try
        {
            var _track = _context.Comments.Where(x => x.MangaId == mangaId).OrderBy(x=>x.CreatedAt).ToList();
            List<CmtResponse> _list = new();
            foreach (var item in _track)
            {
                var user = _context.Users.FirstOrDefault(x => x.Id == item.UserId);
                var _item = new CmtResponse();
                _item.Id = item.Id;
                _item.MangaId = mangaId;
                _item.UserId = user.Id;
                _item.date =item.CreatedAt.ToString("dd/MM/yy");
                _item.Content = item.Content;
                _item.NameUser = user.FullName;
                _item.ImgUser = user.Avatar;
                _list.Add(_item);

            }
            return Ok(new
            {
                code = 200,
                data = _list,
            }); ;
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpPost("Comment")]
    public IActionResult PostComment(Guid userId,Guid mangaId,string value)
    {
        try
        {
            var cmt = new Comment();
            cmt.Id = Guid.NewGuid();
            cmt.MangaId = mangaId;
            cmt.CreatedAt = DateTime.Now;
            cmt.Content = value;
            cmt.DislikedCount = 0;
            cmt.LikedCount = 0;
            cmt.UserId = userId;
            cmt.IsActive = true;
            _context.Comments.Add(cmt);
            _context.SaveChanges();
            return Ok();
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet("listManga")]
    public IActionResult ListManga(string? genre, 
        string? status, string? statusOff, string? sort,int index)
    {
        var pageSize = 6;
        
        // var c1 = Request.Url.AbsoluteUri;
        var categoryList = AllCategory.GetAll;
        var Genre = string.IsNullOrEmpty(genre) ? "Tất cả":genre;
        var Status = string.IsNullOrEmpty(status) ? "Tất cả" : status;
        var StatusOff = string.IsNullOrEmpty(statusOff) ? "Tất cả" : statusOff;
        var Sort = string.IsNullOrEmpty(sort) ? "Tất cả" : sort;

        var mList = _context.Mangas.ToList();
        var cmList = _context.CategoryMangas.ToList();
        var auList = _context.Authors.ToList();
        var listManga = new List<Manga>();
        if (Genre != "Tất cả")
        {
            listManga = _context.Mangas
                .Include(x => x.Author)
                .Include(x => x.CategoryMangas)
                .ThenInclude(x => x.Category)
                .Where(x=>x.CategoryMangas.FirstOrDefault(y=>y.Category.Name==Genre)!=null)
                .OrderByDescending(x => x.ModifiedAt).ToList();
        }
        else
        {
            listManga = _context.Mangas
                .Include(x => x.Author)
                .Include(x => x.CategoryMangas)
                .ThenInclude(x => x.Category)
                .OrderByDescending(x => x.ModifiedAt).ToList();
        }
        
        var statusEnum = Status switch
        {
            "Hoàn thành" => (int)MangaStatus.Done,
            "Đang cập nhật" => (int)MangaStatus.Updating,
            "Dừng cập nhật" => (int)MangaStatus.StopUpdating,
            _ => -1
        };
        bool? statusOffEnum = StatusOff switch
        {
            "Đang ẩn" => false,
            "Đang hiện" => true,
            _ => null
        };
        if (Status != "Tất cả")
        {
            listManga = listManga.Where(x=>x.Status==statusEnum).ToList();
        }
        if (statusOffEnum is not null)
        {
            listManga = listManga.Where(x=>x.IsActive==statusOffEnum).ToList();
        }

        switch (Sort)
        {
            case "Lượt xem cao":
                listManga = listManga.OrderByDescending(x => x.ViewCount).ToList();
                break;
            case "Lượt đánh giá cao":
                listManga = listManga.OrderByDescending(x => x.Star).ToList();
                break;
            case "Lượt theo dõi cao":
                listManga = listManga.OrderByDescending(x => x.FollowCount).ToList();
                break;
        }

        var LastPage = listManga.Count / pageSize;
        if (listManga.Count % pageSize > 0)
        {
            LastPage += 1;
        }
        index = index == 0 ? 1 : index;
        // var PageIndex = index;
        if (index <= LastPage)
        {
            listManga=listManga.Skip(pageSize * (index - 1)).Take(pageSize).ToList();
        }
        return Ok( new DataListMangaResponse
        {
            status = 200,
            success = true,
            data = listManga.Select(x => _map.MapMangaResponse(x)).ToList(),
            lastPage = LastPage
        });
    }

    [HttpPut("ChangeIsActive")]
    public IActionResult ChangeIsActive([FromForm]Guid mangaId)
    {
        var user = _context.Mangas.FirstOrDefault(x => x.Id == mangaId);

        user!.IsActive = !user.IsActive;
        _context.SaveChanges();
        return Ok();
    }
    
    [HttpPut("ChangeViewManga")]
    public IActionResult ChangeViewManga([FromForm]Guid mangaId)
    {
        

        return NotFound();
    }
}