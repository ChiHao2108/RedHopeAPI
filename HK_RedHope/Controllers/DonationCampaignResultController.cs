using HK_RedHope.Data;
using HK_RedHope.DTOs;
using HK_RedHope.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HK_RedHope.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Doctor")]
    public class DonationCampaignResultController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public DonationCampaignResultController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private string ConvertStatus(DonationStatus status)
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

        [HttpGet("{donationId:guid}/list-user")]
        public async Task<IActionResult> GetRegisteredUsers(Guid donationId)
        {
            var donation = await _context.DonationBloods.FindAsync(donationId);
            if (donation == null)
                return NotFound(new { message = "Không tìm thấy đợt hiến máu." });

            var registrations = await _context.DonationHistories
                .Include(h => h.User)
                .Where(h => h.DonationBloodId == donationId && h.Status != DonationStatus.Cancelled)
                .ToListAsync();

            var result = registrations.Select(h => new
            {
                RecordId = h.Id,
                h.UserId,
                FullName = h.User?.FullName,
                Email = h.User?.Email,
                Status = GetStatusText(h.Status),
                CreatedAt = h.CreatedAt.ToString("dd/MM/yyyy")
            });

            return Ok(result);
        }


        [HttpPut("update/{donationRecordId}")]
        public async Task<IActionResult> UpdateBloodTest(Guid donationRecordId, [FromBody] ExaminationUpdateDto dto)
        {
            var history = await _context.DonationHistories
                .Include(h => h.User)
                .Include(h => h.Donation)
                .FirstOrDefaultAsync(h => h.Id == donationRecordId);

            if (history == null || history.User == null || history.Donation == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ đăng ký hoặc thông tin liên quan." });

            var user = history.User;
            var donation = history.Donation;

            if (donation.Status != "Closed")
                return BadRequest(new { message = "Đợt hiến máu chưa đóng đăng ký. Không thể cập nhật kết quả." });

            if (history.Status != DonationStatus.Scheduled)
                return BadRequest(new { message = "Chỉ có hồ sơ đang ở trạng thái hoàn tất đăng ký mới được cập nhật kết quả." });


            user.MedicalHistory = dto.MedicalHistory ?? user.MedicalHistory;
            user.CurrentHealthStatus = dto.CurrentHealthStatus ?? user.CurrentHealthStatus;
            user.RiskBehavior = dto.RiskBehavior ?? user.RiskBehavior;
            user.IsPregnant = dto.IsPregnant ?? user.IsPregnant;

            if (!string.IsNullOrWhiteSpace(dto.BloodType))
                user.BloodType = dto.BloodType;

            var existingResult = await _context.DonationBloodResults
                .FirstOrDefaultAsync(r => r.DonationHistoryId == history.Id);

            if (!dto.IsEligible)
            {
                history.Status = DonationStatus.Rejected;
                history.UpdatedAt = DateTime.Now;

                user.IsApproved = false;
                user.IsRejected = true;

                if (existingResult == null)
                {
                    existingResult = new DonationBloodResult
                    {
                        DonationHistoryId = history.Id,
                        IsEligible = false,
                        BloodType = dto.BloodType ?? user.BloodType,
                        MedicalHistory = dto.MedicalHistory,
                        CurrentHealthStatus = dto.CurrentHealthStatus,
                        RiskBehavior = dto.RiskBehavior,
                        IsPregnant = dto.IsPregnant,
                        UpdatedAt = DateTime.Now
                    };

                    _context.DonationBloodResults.Add(existingResult);
                }
                else
                {
                    existingResult.IsEligible = false;
                    existingResult.BloodType = dto.BloodType ?? existingResult.BloodType;
                    existingResult.MedicalHistory = dto.MedicalHistory;
                    existingResult.CurrentHealthStatus = dto.CurrentHealthStatus;
                    existingResult.RiskBehavior = dto.RiskBehavior;
                    existingResult.IsPregnant = dto.IsPregnant;
                    existingResult.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật: KHÔNG ĐỦ ĐIỀU KIỆN" });
            }

            history.Status = DonationStatus.Completed;
            history.UpdatedAt = DateTime.Now;

            user.LastDonationDate = donation.Date;
            user.IsApproved = true;
            user.IsRejected = false;
            user.HasDonatedBefore = true;

            if (existingResult == null)
            {
                existingResult = new DonationBloodResult
                {
                    DonationHistoryId = history.Id,
                    IsEligible = true,
                    BloodType = dto.BloodType ?? user.BloodType,
                    MedicalHistory = dto.MedicalHistory,
                    CurrentHealthStatus = dto.CurrentHealthStatus,
                    RiskBehavior = dto.RiskBehavior,
                    IsPregnant = dto.IsPregnant,
                    UpdatedAt = DateTime.Now
                };

                _context.DonationBloodResults.Add(existingResult);
            }
            else
            {
                existingResult.IsEligible = true;
                existingResult.BloodType = dto.BloodType ?? existingResult.BloodType;
                existingResult.MedicalHistory = dto.MedicalHistory;
                existingResult.CurrentHealthStatus = dto.CurrentHealthStatus;
                existingResult.RiskBehavior = dto.RiskBehavior;
                existingResult.IsPregnant = dto.IsPregnant;
                existingResult.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xác nhận hiến máu thành công cho {user.FullName}!" });
        }



        [HttpPut("cancel/{donationRecordId}")]
        public async Task<IActionResult> MarkNotArrived(Guid donationRecordId)
        {
            var history = await _context.DonationHistories
                .Include(h => h.Donation)
                .FirstOrDefaultAsync(h => h.Id == donationRecordId);

            if (history == null)
                return NotFound(new { message = "Không tìm thấy phiếu đăng ký" });

            if (history.Donation == null)
                return NotFound(new { message = "Không tìm thấy đợt hiến máu liên quan" });

            if (history.Donation.Status != "Closed")
                return BadRequest(new { message = "Đợt hiến máu chưa đóng đăng ký. Không thể đánh dấu không đến." });

            if (history.Status != DonationStatus.Scheduled)
                return BadRequest(new { message = "Chỉ có hồ sơ đang ở trạng thái hoàn tất đăng ký mới được hủy/đánh dấu không đến." });


            history.Status = DonationStatus.NotArrived;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã cập nhật: Người đăng ký KHÔNG ĐẾN KHÁM" });
        }
    }
}
