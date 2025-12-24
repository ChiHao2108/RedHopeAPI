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
    [Authorize(Roles = "Admin,Doctor")]
    public class DonationCampaignManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DonationCampaignManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        private void UpdateCampaignStatus(DonationBlood c)
        {
            c.Status = (DateTime.Today > c.RegistrationDeadline) ? "Closed" : "Open";
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

        [HttpPost("create")]
        public async Task<IActionResult> CreateDonation([FromBody] CreateDonationDto? dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Dữ liệu đợt hiến máu không được để trống." });

            if (string.IsNullOrWhiteSpace(dto.CampaignName) ||
                string.IsNullOrWhiteSpace(dto.Address) ||
                string.IsNullOrWhiteSpace(dto.City) ||
                string.IsNullOrWhiteSpace(dto.TimeRange) ||
                string.IsNullOrWhiteSpace(dto.RequiredBloodType) ||
                dto.MaxRegistrations <= 0 ||
                dto.RequiredBloodVolume <= 0 ||
                dto.Date == default ||
                dto.RegistrationDeadline == default)
            {
                return BadRequest(new { message = "Vui lòng điền đầy đủ tất cả các trường trước khi tạo đợt hiến máu." });
            }

            if (dto.Date.Date < DateTime.Today)
                return BadRequest(new { message = "Ngày tổ chức hiến máu không được là ngày cũ." });

            if (dto.RegistrationDeadline.Date < DateTime.Today)
                return BadRequest(new { message = "Ngày đăng ký không được là ngày cũ." });

            if ((dto.Date - dto.RegistrationDeadline).TotalDays < 3)
                return BadRequest(new { message = "Ngày đăng ký phải cách ngày tổ chức ít nhất 3 ngày." });

            if (dto.RequiredBloodVolume < 250 || dto.RequiredBloodVolume > 450)
                return BadRequest(new { message = "Lượng máu hiến phải từ 250ml đến 450ml." });

            if (dto.MaxRegistrations <= 0)
                return BadRequest(new { message = "Số lượng người đăng ký phải lớn hơn 0." });

            var parts = dto.TimeRange.Split("-");
            if (parts.Length != 2 ||
                !TimeSpan.TryParse(parts[0].Trim(), out _) ||
                !TimeSpan.TryParse(parts[1].Trim(), out _))
            {
                return BadRequest(new { message = "TimeRange không đúng định dạng 'HH:mm - HH:mm'." });
            }

            var donation = new DonationBlood
            {
                CampaignName = dto.CampaignName,
                Date = dto.Date,
                RequiredBloodType = dto.RequiredBloodType,
                Address = dto.Address,
                City = dto.City,
                TimeRange = dto.TimeRange,
                MaxRegistrations = dto.MaxRegistrations,
                RequiredBloodVolume = dto.RequiredBloodVolume,
                SupportGift = dto.SupportGift ?? string.Empty,
                RegistrationDeadline = dto.RegistrationDeadline,
                Status = "Open",
                CreatedAt = DateTime.Now
            };

            _context.DonationBloods.Add(donation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo đợt hiến máu thành công!",
                donation = new
                {
                    donation.CampaignName,
                    donation.Address,
                    donation.City,
                    RegistrationDeadline = donation.RegistrationDeadline.ToString("dd/MM/yyyy"),
                    Date = donation.Date.ToString("dd/MM/yyyy"),
                    donation.TimeRange,
                    donation.MaxRegistrations,
                    donation.RequiredBloodType,
                    donation.RequiredBloodVolume,
                    donation.SupportGift,
                    donation.Status,
                    CreatedAt = donation.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
                }
            });

        }

        [HttpPut("update/{id:guid}")]
        public async Task<IActionResult> UpdateDonation(Guid id, [FromBody] UpdateDonationDto dto)
        {
            var donation = await _context.DonationBloods.FindAsync(id);
            if (donation == null)
                return NotFound(new { message = "Không tìm thấy đợt hiến máu." });

            if (donation.RegisteredCount > 0)
                return BadRequest(new { message = "Đợt hiến máu đã có người đăng ký, không thể cập nhật." });

            if (!string.IsNullOrWhiteSpace(dto.CampaignName) &&
                string.IsNullOrWhiteSpace(dto.CampaignName))
                return BadRequest(new { message = "Tên chiến dịch không được để trống." });

            if (dto.Date.HasValue && dto.Date.Value.Date < DateTime.Today)
                return BadRequest(new { message = "Ngày tổ chức hiến máu không được là ngày cũ." });

            if (dto.RegistrationDeadline.HasValue && dto.RegistrationDeadline.Value.Date < DateTime.Today)
                return BadRequest(new { message = "Ngày đăng ký không được là ngày cũ." });

            if (dto.Date.HasValue && dto.RegistrationDeadline.HasValue &&
                (dto.Date.Value - dto.RegistrationDeadline.Value).TotalDays < 3)
                return BadRequest(new { message = "Ngày đăng ký phải cách ngày tổ chức ít nhất 3 ngày." });

            if (dto.RequiredBloodVolume.HasValue &&
                (dto.RequiredBloodVolume < 250 || dto.RequiredBloodVolume > 450))
                return BadRequest(new { message = "Lượng máu hiến phải từ 250ml đến 450ml." });

            if (dto.MaxRegistrations.HasValue && dto.MaxRegistrations <= 0)
                return BadRequest(new { message = "Số lượng người đăng ký phải lớn hơn 0." });

            if (!string.IsNullOrWhiteSpace(dto.TimeRange))
            {
                var parts = dto.TimeRange.Split("-");
                if (parts.Length != 2 ||
                    !TimeSpan.TryParse(parts[0].Trim(), out _) ||
                    !TimeSpan.TryParse(parts[1].Trim(), out _))
                {
                    return BadRequest(new { message = "TimeRange không đúng định dạng 'HH:mm - HH:mm'." });
                }
            }

            donation.CampaignName = dto.CampaignName ?? donation.CampaignName;
            donation.Date = dto.Date ?? donation.Date;
            donation.RegistrationDeadline = dto.RegistrationDeadline ?? donation.RegistrationDeadline;
            donation.RequiredBloodType = dto.RequiredBloodType ?? donation.RequiredBloodType;
            donation.Address = dto.Address ?? donation.Address;
            donation.City = dto.City ?? donation.City;
            donation.TimeRange = dto.TimeRange ?? donation.TimeRange;
            donation.MaxRegistrations = dto.MaxRegistrations ?? donation.MaxRegistrations;
            donation.RequiredBloodVolume = dto.RequiredBloodVolume ?? donation.RequiredBloodVolume;
            donation.SupportGift = dto.SupportGift ?? donation.SupportGift;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật đợt hiến máu thành công!",
                donation = new
                {
                    donation.CampaignName,
                    donation.Address,
                    donation.City,
                    RegistrationDeadline = donation.RegistrationDeadline.ToString("dd/MM/yyyy"),
                    Date = donation.Date.ToString("dd/MM/yyyy"),
                    donation.TimeRange,
                    donation.MaxRegistrations,
                    donation.RequiredBloodType,
                    donation.RequiredBloodVolume,
                    donation.SupportGift,
                    donation.Status,
                    CreatedAt = donation.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
                }
            });
        }


        [HttpDelete("delete/{id:guid}")]
        public async Task<IActionResult> DeleteDonation(Guid id)
        {
            var donation = await _context.DonationBloods.FindAsync(id);
            if (donation == null)
                return NotFound(new { message = "Không tìm thấy đợt hiến máu cần xóa." });

            if (!string.Equals(donation.Status, "Closed", StringComparison.OrdinalIgnoreCase)
                && donation.RegisteredCount > 0)
            {
                return BadRequest(new
                {
                    message = "Không thể xóa đợt hiến máu này vì đang có người đăng ký và đợt chưa đóng."
                });
            }

            _context.DonationBloods.Remove(donation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa đợt hiến máu thành công!" });
        }



        [HttpPost("filter")]
        public async Task<IActionResult> FilterDonations([FromBody] DonationFilterDto filter)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!user.IsApproved)
            {
                if (user.IsRejected)
                    return BadRequest(new { message = "Hồ sơ của bạn đã bị từ chối. Không thể xem danh sách." });

                return BadRequest(new { message = "Hồ sơ của bạn chưa được duyệt. Vui lòng chờ admin phê duyệt." });
            }

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



        [HttpPut("{donationId:guid}/end-donation")]
        public async Task<IActionResult> EndRegistration(Guid donationId)
        {
            var donation = await _context.DonationBloods.FindAsync(donationId);
            if (donation == null)
                return NotFound(new { message = "Không tìm thấy đợt hiến máu." });

            if (donation.Status == "Closed")
                return BadRequest(new { message = "Đợt hiến máu này đã kết thúc đăng ký trước đó." });

            donation.Status = "Closed";

            var registrations = await _context.DonationHistories
                .Where(h => h.DonationBloodId == donationId && h.Status == DonationStatus.Registered)
                .ToListAsync();

            foreach (var reg in registrations)
            {
                reg.Status = DonationStatus.Scheduled;
                reg.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đã kết thúc tuyển đăng ký cho '{donation.CampaignName}'."
            });
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
