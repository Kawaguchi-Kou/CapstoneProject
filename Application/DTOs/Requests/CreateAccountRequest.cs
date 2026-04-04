using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests
{
    /// <summary>
    /// Chỉ 5 thuộc tính: Email, Name, Role (RoleId), IsActive, CreatedAt.
    /// Mật khẩu mặc định do hệ thống gán (đổi sau khi đăng nhập / reset).
    /// </summary>
    public class CreateAccountRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
