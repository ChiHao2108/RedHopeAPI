using HK_RedHope.Data;
using HK_RedHope.DTOs;
using HK_RedHope.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HK_RedHope.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User,Admin")]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }


        private void UpdateCampaignStatus(DonationBlood c)
        {
            c.Status = (DateTime.Today > c.RegistrationDeadline) ? "Closed" : "Open";
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (user.IsApproved && user.LastProfileUpdate.HasValue &&
                user.LastProfileUpdate.Value.AddMonths(3) <= DateTime.Now)
            {
                user.HasDonatedBefore = false;
                user.LastDonationDate = null;
                user.MedicalHistory = null;
                user.RiskBehavior = null;
                user.CurrentHealthStatus = null;
                user.IsPregnant = null;

                user.IsApproved = false;
                user.LastProfileUpdate = DateTime.Now;

                await _userManager.UpdateAsync(user);
            }

            string profileStatus;
            if (user.IsRejected)
                profileStatus = "Đã từ chối";
            else if (user.IsApproved)
                profileStatus = "Đã duyệt";
            else
                profileStatus = "Chưa duyệt";

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                Role = roles,
                user.FullName,
                user.Email,
                user.Gender,
                user.PhoneNumber,
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
                LastProfileUpdate = user.LastProfileUpdate?.ToString("dd/MM/yyyy HH:mm:ss"),
                ProfileStatus = profileStatus
            });
        }


        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (user.IsRejected)
                return BadRequest(new { message = "Hồ sơ đã bị từ chối, không thể cập nhật." });

            if (string.IsNullOrWhiteSpace(dto.FullName) ||
                string.IsNullOrWhiteSpace(dto.Gender) ||
                string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
                string.IsNullOrWhiteSpace(dto.Address) ||
                string.IsNullOrWhiteSpace(dto.City) ||
                !dto.DateOfBirth.HasValue ||
                !dto.BloodVolumeToDonate.HasValue ||
                !dto.Weight.HasValue ||
                string.IsNullOrWhiteSpace(dto.MedicalHistory) ||
                string.IsNullOrWhiteSpace(dto.RiskBehavior) ||
                string.IsNullOrWhiteSpace(dto.CurrentHealthStatus) ||
                !dto.IsPregnant.HasValue ||
                !dto.HasDonatedBefore.HasValue)
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ tất cả các trường." });
            }

            var validGender = new[] { "male", "female" };
            if (!validGender.Contains(dto.Gender.ToLower()))
                return BadRequest(new { message = "Giới tính chỉ được nhập 'male' hoặc 'female'." });

            var birth = dto.DateOfBirth.Value;
            if (birth > DateTime.Today)
                return BadRequest(new { message = "Ngày sinh không hợp lệ." });

            int age = DateTime.Today.Year - birth.Year;
            if (birth.Date > DateTime.Today.AddYears(-age)) age--;
            if (age < 18 || age > 60)
                return BadRequest(new { message = "Độ tuổi phải từ 18 đến 60." });

            if (dto.BloodVolumeToDonate < 250 || dto.BloodVolumeToDonate > 450)
                return BadRequest(new { message = "Lượng máu hiến phải từ 250ml đến 450ml." });

            if (dto.Weight < 45)
                return BadRequest(new { message = "Cân nặng phải từ 45kg trở lên." });

            if (dto.Gender.ToLower() != "female")
            {
                if (dto.IsPregnant == true)
                    return BadRequest(new { message = "Chỉ người dùng nữ mới được chọn đang mang thai." });

                dto.IsPregnant = false;
            }

            if (dto.HasDonatedBefore == true)
            {
                if (!dto.LastDonationDate.HasValue)
                    return BadRequest(new { message = "Người từng hiến máu phải nhập ngày hiến gần nhất." });

                if (dto.LastDonationDate.Value > DateTime.Today)
                    return BadRequest(new { message = "Ngày hiến gần nhất không được lớn hơn ngày hiện tại." });
            }

            if (dto.HasDonatedBefore == false && dto.LastDonationDate.HasValue)
                return BadRequest(new { message = "Người chưa từng hiến máu không được nhập ngày hiến." });

            user.FullName = dto.FullName;
            user.Gender = dto.Gender;
            user.PhoneNumber = dto.PhoneNumber.Trim();
            user.Address = dto.Address;
            user.City = dto.City;
            user.DateOfBirth = dto.DateOfBirth;
            user.BloodType = dto.BloodType;
            user.BloodVolumeToDonate = dto.BloodVolumeToDonate;
            user.Weight = dto.Weight;
            user.MedicalHistory = dto.MedicalHistory;
            user.RiskBehavior = dto.RiskBehavior;
            user.CurrentHealthStatus = dto.CurrentHealthStatus;
            user.IsPregnant = dto.IsPregnant;
            user.HasDonatedBefore = dto.HasDonatedBefore.Value;
            user.LastDonationDate = dto.LastDonationDate;

            user.LastProfileUpdate = DateTime.Now;
            user.IsApproved = false;
            user.IsRejected = false;

            await _userManager.UpdateAsync(user);

            var futureRegistrations = await _context.DonationHistories
                .Include(dh => dh.Donation)
                .Where(dh => dh.UserId == user.Id
                             && (dh.Status == DonationStatus.Registered
                                 || dh.Status == DonationStatus.Scheduled)
                             && dh.Donation != null
                             && dh.Donation.Date >= DateTime.Today)
                .ToListAsync();

            foreach (var reg in futureRegistrations)
            {
                reg.Status = DonationStatus.Cancelled;

                if (reg.Donation != null && reg.Donation.RegisteredCount > 0)
                    reg.Donation.RegisteredCount--;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật hồ sơ thành công! Vui lòng chờ Admin duyệt."
            });
        }


        [HttpGet("record-donation")]
        public async Task<IActionResult> GetDonationHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

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

    }
}
