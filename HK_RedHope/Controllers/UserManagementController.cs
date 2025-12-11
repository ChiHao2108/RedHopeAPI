using HK_RedHope.Data;
using HK_RedHope.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HK_RedHope.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]

    public class UserManagementController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _context = context;
        }

        private string GetStatusText(DonationStatus status)
        {
            return status switch
            {
                DonationStatus.Registered => "Đã đăng ký, đợi phản hồi",
                DonationStatus.Scheduled => "Hoàn tất đăng ký, vui lòng đến đúng lịch",
                DonationStatus.Approved => "Đủ điều kiện",
                DonationStatus.Rejected => "Không đủ điều kiện",
                DonationStatus.NotArrived => "Không đến khám",
                DonationStatus.Completed => "Đã hiến máu",
                DonationStatus.Cancelled => "Người dùng hủy lịch",
                _ => "Không xác định"
            };
        }

        public class AssignRoleDto
        {
            public string UserId { get; set; } = string.Empty;
        }

        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var list = new List<object>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);

                list.Add(new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    Roles = roles,
                   
                });
            }

            return Ok(list);
        }


        [HttpGet("user-detail/{userId}")]
        public async Task<IActionResult> GetUserDetail(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Gender,
                user.Address,
                user.City,
                DateOfBirth = user.DateOfBirth?.ToString("dd/MM/yyyy"),
                user.BloodType,
                user.HasDonatedBefore,
                LastDonationDate = user.LastDonationDate?.ToString("dd/MM/yyyy"),
                user.BloodVolumeToDonate,
                user.Weight,
                user.MedicalHistory,
                user.RiskBehavior,
                user.CurrentHealthStatus,
                user.IsPregnant,
                RegistrationDate = user.RegistrationDate.ToString("dd/MM/yyyy"),
                Status = user.IsRejected ? "Đã từ chối" : user.IsApproved ? "Đã duyệt" : "Chưa duyệt"
            });
        }

        [HttpGet("record-donation/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDonationHistoryForAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Người dùng không tồn tại." });

            var donationRecords = await _context.DonationHistories
                .Include(dh => dh.Donation)
                .Where(dh => dh.UserId == user.Id && dh.Status != DonationStatus.Cancelled)
                .ToListAsync();

            var result = donationRecords.Select(dh => new
            {
                RecordId = dh.Id,
                DonationName = dh.Donation!.CampaignName,
                dh.DonationBloodId,
                Status = GetStatusText(dh.Status),
                QueueNumber = dh.QueueNumber,
                CreatedAt = dh.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                Donation = new
                {
                    dh.Donation.Id,
                    dh.Donation.CampaignName,
                    Date = dh.Donation.Date.ToString("dd/MM/yyyy"),
                    RegistrationDeadline = dh.Donation.RegistrationDeadline.ToString("dd/MM/yyyy"),
                    dh.Donation.Address,
                    dh.Donation.City,
                    dh.Donation.TimeRange,
                    dh.Donation.RequiredBloodType,
                    dh.Donation.RequiredBloodVolume,
                    dh.Donation.SupportGift,
                    dh.Donation.MaxRegistrations,
                    dh.Donation.RegisteredCount,
                    dh.Donation.Status
                }
            }).ToList();

            return Ok(result);
        }


        [HttpPost("assign-Admin")]
        public async Task<IActionResult> AssignAdminRole([FromBody] AssignRoleDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy user." });

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = $"Đã cấp quyền Admin cho {user.Email}." });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "Xóa tài khoản thành công" });
        }
    }
}
