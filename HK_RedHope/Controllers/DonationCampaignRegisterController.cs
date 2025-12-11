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
    public class DonationCampaignRegisterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DonationCampaignRegisterController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        private void UpdateCampaignStatus(DonationBlood c)
        {
            c.Status = (DateTime.Today > c.RegistrationDeadline) ? "Closed" : "Open";
        }

        [HttpPost("filter")]
        public async Task<IActionResult> FilterDonations([FromBody] DonationFilterDto filter)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var query = _context.DonationBloods.AsQueryable();

            if (filter.Id.HasValue)
                query = query.Where(d => d.Id == filter.Id.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(d => d.Date >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(d => d.Date <= filter.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(d => d.City.Contains(filter.City));

            if (filter.RequiredBloodTypes != null && filter.RequiredBloodTypes.Any())
            {
                var list = query.ToList();

                list = list.Where(d =>
                    d.RequiredBloodType
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Any(bt => filter.RequiredBloodTypes
                            .Any(f => f.Equals(bt, StringComparison.OrdinalIgnoreCase)))
                ).ToList();

                return Ok(list);
            }



            var result = query.ToList();

            if (!result.Any())
                return NotFound(new { message = "Không tìm thấy đợt hiến máu phù hợp." });

            return Ok(result);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetDonations()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            bool isAdminOrDoctor = roles.Contains("Admin") || roles.Contains("Doctor");

            if (!isAdminOrDoctor && !user.IsApproved)
            {
                if (user.IsRejected)
                    return BadRequest(new { message = "Hồ sơ của bạn đã bị từ chối. Không thể xem danh sách." });

                return BadRequest(new { message = "Hồ sơ của bạn chưa được duyệt. Vui lòng chờ admin phê duyệt." });
            }

            var list = await _context.DonationBloods
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            foreach (var item in list)
            {
                if (item.Status == "Closed")
                    continue;

                if (item.RegistrationDeadline.Date < DateTime.Today)
                {
                    item.Status = "Closed";

                    var histories = await _context.DonationHistories
                        .Where(h => h.DonationBloodId == item.Id &&
                                    h.Status == DonationStatus.Registered)
                        .ToListAsync();

                    foreach (var h in histories)
                    {
                        h.Status = DonationStatus.Scheduled;
                        h.UpdatedAt = DateTime.Now;
                    }
                }
                else
                {
                    item.Status = "Open";
                }

                item.RegisteredCount = await _context.DonationHistories
                    .Where(h => h.DonationBloodId == item.Id &&
                                h.Status != DonationStatus.Cancelled)
                    .CountAsync();
            }


            await _context.SaveChangesAsync();

            var result = list.Select(d => new
            {
                d.Id,
                d.CampaignName,
                d.Address,
                d.City,
                RegistrationDeadline = d.RegistrationDeadline.ToString("dd/MM/yyyy"),
                Date = d.Date.ToString("dd/MM/yyyy"),
                d.TimeRange,
                d.RegisteredCount,
                d.MaxRegistrations,
                d.RequiredBloodType,
                d.RequiredBloodVolume,
                d.SupportGift,
                d.Status,
                CreatedAt = d.CreatedAt.ToString("dd/MM/yyyy")
            });

            return Ok(result);
        }

        
        [HttpPost("register/{donationId}")]
        public async Task<IActionResult> Register(Guid donationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(user.FullName) ||
                string.IsNullOrWhiteSpace(user.Gender) ||
                string.IsNullOrWhiteSpace(user.BloodType) ||
                !user.DateOfBirth.HasValue ||
                !user.BloodVolumeToDonate.HasValue ||
                !user.Weight.HasValue ||
                string.IsNullOrWhiteSpace(user.MedicalHistory) ||
                string.IsNullOrWhiteSpace(user.RiskBehavior) ||
                string.IsNullOrWhiteSpace(user.CurrentHealthStatus) ||
                (user.Gender == "Nữ" && !user.IsPregnant.HasValue)
            )
            {
                return BadRequest(new
                {
                    message = " Bạn phải cập nhật đầy đủ hồ sơ trước khi đăng ký hiến máu."
                });
            }

            if (!user.IsApproved)
            {
                if (user.IsRejected)
                {
                    return BadRequest(new
                    {
                        message = " Hồ sơ bị từ chối. Vui lòng cập nhật lại!"
                    });
                }
                return BadRequest(new
                {
                    message = " Hồ sơ đầy đủ nhưng chưa được admin duyệt."
                });
            }

            var existing = await _context.DonationHistories
                .Where(dh => dh.UserId == user.Id &&
                             dh.Status != DonationStatus.Completed &&
                             dh.Status != DonationStatus.Cancelled)
                .FirstOrDefaultAsync();

            if (existing != null)
                return BadRequest(new
                {
                    message = " Bạn đang có lịch hiến máu. Vui lòng đợi hiến hoàn tất hoặc có thể hủy lịch hiện tại để đăng ký mới."
                });

            var donation = await _context.DonationBloods.FindAsync(donationId);
            if (donation == null)
                return NotFound(new { message = "Không tìm thấy đợt hiến máu." });

            if (donation.RegistrationDeadline.Date < DateTime.Today ||
                donation.RegisteredCount >= donation.MaxRegistrations)
            {
                donation.Status = "Closed";

                var histories = await _context.DonationHistories
                    .Where(h => h.DonationBloodId == donation.Id && h.Status == DonationStatus.Registered)
                    .ToListAsync();

                foreach (var h in histories)
                {
                    h.Status = DonationStatus.Scheduled;
                    h.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }

            if (donation.Status == "Closed")
                return BadRequest(new { message = "Đợt hiến máu đã đóng đăng ký." });

            if (!string.IsNullOrWhiteSpace(user.BloodType) &&
                !string.IsNullOrWhiteSpace(donation.RequiredBloodType))
            {
                var allowedTypes = donation.RequiredBloodType
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().ToUpper())
                    .ToList();

                if (!allowedTypes.Contains(user.BloodType.ToUpper()))
                {
                    return BadRequest(new
                    {
                        message = $"Loại máu của bạn ({user.BloodType}) không phù hợp. " +
                                  $"Đợt này chỉ nhận: {donation.RequiredBloodType}"
                    });
                }
            }


            if (user.BloodVolumeToDonate < donation.RequiredBloodVolume)
            {
                return BadRequest(new
                {
                    message = $"Bạn cần tối thiểu {donation.RequiredBloodVolume}ml máu để đăng ký đợt này."
                });
            }

            if (donation.RegisteredCount >= donation.MaxRegistrations)
                return BadRequest(new { message = "Số lượng đăng ký tối đa đã đầy." });

            if (user.LastDonationDate.HasValue)
            {
                var daysSinceLastDonation = (DateTime.Today - user.LastDonationDate.Value).TotalDays;

                if (daysSinceLastDonation < 84) 
                {
                    var remaining = 84 - (int)daysSinceLastDonation;
                    return BadRequest(new
                    {
                        message = $"Bạn chỉ mới hiến máu {daysSinceLastDonation:F0} ngày trước. " +
                                  $"Cần chờ thêm {remaining} ngày nữa để đủ 3 tháng."
                    });
                }
            }

            donation.RegisteredCount++;

            var history = new DonationHistory
            {
                UserId = user.Id,
                DonationBloodId = donation.Id,
                Status = DonationStatus.Registered,
                QueueNumber = donation.RegisteredCount
            };

            _context.DonationHistories.Add(history);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $" {user.FullName} đã đăng ký '{donation.CampaignName}'!",
                queueNumber = history.QueueNumber,
                history = new
                {
                    id = history.Id,
                    userId = user.Id,
                    user = new
                    {
                        fullName = user.FullName,
                        gender = user.Gender,
                        identificationNumber = user.IdentificationNumber,
                        address = user.Address,
                        city = user.City,
                        dateOfBirth = user.DateOfBirth?.ToString("dd/MM/yyyy"), 
                        bloodType = user.BloodType,
                        hasDonatedBefore = user.HasDonatedBefore,
                        lastDonationDate = user.LastDonationDate?.ToString("dd/MM/yyyy"),
                        bloodVolumeToDonate = user.BloodVolumeToDonate,
                        weight = user.Weight,
                        medicalHistory = user.MedicalHistory,
                        riskBehavior = user.RiskBehavior,
                        currentHealthStatus = user.CurrentHealthStatus,
                        isPregnant = user.IsPregnant
                    },
                    donationBloodId = donation.Id,
                    donation = new
                    {
                        id = donation.Id,
                        campaignName = donation.CampaignName,
                        date = donation.Date.ToString("dd/MM/yyyy"),
                        registrationDeadline = donation.RegistrationDeadline.ToString("dd/MM/yyyy"),
                        requiredBloodType = donation.RequiredBloodType,
                        requiredBloodVolume = donation.RequiredBloodVolume,
                        address = donation.Address,
                        city = donation.City,
                        timeRange = donation.TimeRange,
                        registeredCount = donation.RegisteredCount,
                        maxRegistrations = donation.MaxRegistrations,
                        supportGift = donation.SupportGift,
                        status = donation.Status
                    },
                    status = history.Status,
                    createdAt = history.CreatedAt.ToString("dd/MM/yyyy"),
                    updatedAt = history.UpdatedAt.ToString("dd/MM/yyyy"),
                    queueNumber = history.QueueNumber
                }
            });
        }


        [HttpDelete("cancel/{donationRecordId:guid}")]
        public async Task<IActionResult> CancelDonation(Guid donationRecordId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!user.IsApproved)
            {
                if (user.IsRejected)
                    return BadRequest(new { message = " Hồ sơ bị từ chối. Không thể hủy lịch." });

                return BadRequest(new { message = " Hồ sơ chưa được duyệt. Vui lòng chờ admin phê duyệt." });
            }

            var donationRecord = await _context.DonationHistories
                .Include(h => h.Donation)
                .FirstOrDefaultAsync(h => h.Id == donationRecordId && h.UserId == user.Id);

            if (donationRecord == null)
                return NotFound();

            if (donationRecord.Donation == null)
                return BadRequest(new { message = " Đợt hiến máu không tồn tại hoặc đã bị xóa." });

            if (!CanCancel(donationRecord))
                return BadRequest(new { message = " Không thể hủy, còn ≤ 3 ngày đến ngày hiến máu!" });

            donationRecord.Status = DonationStatus.Cancelled;
            donationRecord.UpdatedAt = DateTime.Now;

            donationRecord.Donation.RegisteredCount--;

            await _context.SaveChangesAsync();

            return Ok(new { message = " Đã hủy đăng ký thành công." });
        }


        private bool CanCancel(DonationHistory history)
        {
            if (history.Status == DonationStatus.Registered)
                return true;

            if (history.Status == DonationStatus.Scheduled)
            {
                if (history.Donation == null)
                    return false;

                var daysUntilDonation = (history.Donation.Date - DateTime.Today).TotalDays;
                return daysUntilDonation > 3; 
            }

            return false;
        }

    }
}
