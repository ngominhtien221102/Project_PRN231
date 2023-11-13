using Service.MangaOnline.Models;
using Service.MangaOnline.ResponseModels;

namespace Service.MangaOnline.ResponseModels
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsActive { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Avatar { get; set; }
        public int? RoleId { get; set; }

        public virtual UserToken? UserToken { get; set; }
    }

    public class DataListUserResponse
    {
        public int status { get; set; }
        public bool success { get; set; }
        public List<UserResponse> data { get; set; }
        public int lastPage { get; set; }
    }
}
