using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.MangaOnline.Commons;
using Service.MangaOnline.DTO;
using Service.MangaOnline.Extensions;
using Service.MangaOnline.Models;
using Service.MangaOnline.ResponseModels;

namespace Service.MangaOnline.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly MangaOnlineV1DevContext _context;
        private readonly IMapping _map;

        public UserController(MangaOnlineV1DevContext context, IMapping map)
        {
            _context = context;
            _map = map;
        }

        [HttpGet]
        public IActionResult GetUserProfile(Guid id)
        {
            User? user = _context.Users.Where(x => x.Id == id).FirstOrDefault();
            return Ok(user);
        }

        [HttpGet("GetAll")]
        public IActionResult GetAllUser(string? isActive, string? role, int index)
        {
            var pageSize = 6;

            var IsActive = string.IsNullOrEmpty(isActive) ? "Tất cả" : isActive;
            var Role = string.IsNullOrEmpty(role) ? "Tất cả" : role;

            var listUser = _context.Users.Include(x => x.UserToken).ToList();

            bool? isActiveEnum = IsActive switch
            {
                "Khóa" => false,
                "Bình thường" => true,
                _ => null
            };

            var roleEnum = Role switch
            {
                "Admin" => (int)UserRoleEnum.Admin,
                "Người dùng bình thường" => (int)UserRoleEnum.UserNormal,
                "Người dùng Vip" => (int)UserRoleEnum.UserVip,
                _ => -1
            };

            if (isActiveEnum is not null)
            {
                listUser = listUser.Where(x => x.IsActive == isActiveEnum).ToList();
            }
            if (Role != "Tất cả")
            {
                listUser = listUser.Where(x => x.RoleId == roleEnum).ToList();
            }

            var LastPage = listUser.Count / pageSize;
            if (listUser.Count % pageSize > 0)
            {
                LastPage += 1;
            }
            index = index == 0 ? 1 : index;
            // var PageIndex = index;
            if (index > LastPage && listUser.Count > 0)
            {
                return NotFound();
            }
            else
            {
                listUser = listUser.Skip(pageSize * (index - 1)).Take(pageSize).ToList();
                return Ok(new DataListUserResponse
                {
                    status = 200,
                    success = true,
                    data = listUser.Select(x => _map.MapUserResponse(x)).ToList(),
                    lastPage = LastPage
                });
            }
            
        }


        [HttpPut("ChangeIsActive")]
        public IActionResult ChangeIsActive(Guid id)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (user == null)
                return NotFound();
            user!.IsActive = !user.IsActive;
            _context.SaveChanges();
            return Ok();
        }

        [HttpGet("GetUserToken/{id}")]
        public IActionResult GetUserToken(Guid id)
        {
            UserToken? token = _context.UserTokens
                   .Where(x => x.UserId == id).FirstOrDefault();
            return Ok(token);
        }

        [HttpPost("UpdateUser")]
        public IActionResult UpdateUser(ProfileUser user)
        {
            User? current = _context.Users.Where(x => x.Id == user.Id).FirstOrDefault();
            if (current == null)
            {
                return NotFound();
            }

            current.FullName = user.FullName;
            current.PhoneNumber = user.PhoneNumber;
            if (user.Avatar != null)
                current.Avatar = user.Avatar;

            _context.SaveChanges();

            return Ok(current);
        }
    }
}
