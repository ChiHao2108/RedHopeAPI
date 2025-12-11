using HK_RedHope.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HK_RedHope.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ProfileReviewController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;

        public ProfileReviewController(UserManager<ApplicationUser> userManager,
                              SignInManager<ApplicationUser> signInManager,
                              IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        public class AssignRoleDto
        {
            public string UserId { get; set; } = string.Empty;
        }


        [HttpGet("pending-users")]
        public async Task<IActionResult> GetPendingUsers()
        {
            var allUsers = await _userManager.GetUsersInRoleAsync("User");

            var pendingUsers = allUsers
                .Where(u => !u.IsApproved && !u.IsRejected)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Gender,
                    u.DateOfBirth,
                    u.BloodType,
                    u.HasDonatedBefore,
                    u.LastDonationDate,
                    u.BloodVolumeToDonate,
                    u.MedicalHistory,
                    u.RiskBehavior,
                    u.CurrentHealthStatus,
                    u.IsPregnant,
                    Status = "Chưa duyệt"
                })
                .ToList();

            return Ok(pendingUsers);
        }


        [HttpGet("approved-users")]
        public async Task<IActionResult> GetApprovedUsers()
        {
            var allUsers = await _userManager.GetUsersInRoleAsync("User");

            var approvedUsers = allUsers
                .Where(u => u.IsApproved)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Gender,
                    u.DateOfBirth,
                    u.BloodType,
                    u.HasDonatedBefore,
                    u.LastDonationDate,
                    u.BloodVolumeToDonate,
                    u.MedicalHistory,
                    u.RiskBehavior,
                    u.CurrentHealthStatus,
                    u.IsPregnant,
                    u.IsApproved,
                    Status = "Đã duyệt"
                })
                .ToList();

            return Ok(approvedUsers);
        }


        [HttpGet("rejected-users")]
        public async Task<IActionResult> GetRejectedUsers()
        {
            var allUsers = await _userManager.GetUsersInRoleAsync("User");

            var rejectedUsers = allUsers
                .Where(u => u.IsRejected)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Gender,
                    u.DateOfBirth,
                    u.BloodType,
                    u.HasDonatedBefore,
                    u.LastDonationDate,
                    u.BloodVolumeToDonate,
                    u.MedicalHistory,
                    u.RiskBehavior,
                    u.CurrentHealthStatus,
                    u.IsPregnant,
                    Status = "Đã từ chối"
                })
                .ToList();

            return Ok(rejectedUsers);
        }

        
        [HttpPut("approve/{userId}")]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!IsProfileCompleted(user))
                return BadRequest(new { message = "Không thể duyệt. Người dùng chưa cập nhật đầy đủ hồ sơ." });

            user.IsApproved = true;
            user.IsRejected = false;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = $"Đã duyệt hồ sơ của {user.FullName ?? user.Email}" });
        }


        [HttpPut("reject/{userId}")]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!IsProfileCompleted(user))
                return BadRequest(new { message = "Không thể từ chối. Người dùng chưa cập nhật đầy đủ hồ sơ để xét duyệt." });

            user.IsApproved = false;
            user.IsRejected = true;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return StatusCode(500, new { message = "Cập nhật trạng thái thất bại" });

            return Ok(new
            {
                message = $"Đã từ chối hồ sơ của {user.FullName ?? user.Email}"
            });
        }

        private bool IsProfileCompleted(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.FullName) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.Gender) ||
                string.IsNullOrWhiteSpace(user.PhoneNumber) ||
                string.IsNullOrWhiteSpace(user.Address) ||
                string.IsNullOrWhiteSpace(user.City) ||
                !user.DateOfBirth.HasValue ||
                !user.BloodVolumeToDonate.HasValue ||
                !user.Weight.HasValue ||
                string.IsNullOrWhiteSpace(user.MedicalHistory) ||
                string.IsNullOrWhiteSpace(user.RiskBehavior) ||
                string.IsNullOrWhiteSpace(user.CurrentHealthStatus) ||
                !user.IsPregnant.HasValue ||
                !user.HasDonatedBefore.HasValue)
                return false;

            if (user.HasDonatedBefore == true)
            {
                if (!user.LastDonationDate.HasValue) return false;
                if (user.LastDonationDate > DateTime.Today) return false;
            }

            if (user.HasDonatedBefore == false && user.LastDonationDate.HasValue)
                return false;

            return true;
        }
    }
}
