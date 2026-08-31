using coop.Dtos.AdminController;
using coop.Enums;
using coop.Model;
using coop.Services;
using coop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace coop.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public AdminController(CoopDbContext dbcontext, IAuditService auditService,INotificationService notificationService)
        {
            _dbcontext = dbcontext;
            _auditService = auditService;
            _notificationService = notificationService;

        }

      

        [HttpGet("offers/pending")]
        public async Task<IActionResult> GetPendingOffers()
        {
            var offers = await _dbcontext.Offers
                .Where(o => o.Status == OfferStatus.PendingApproval)
                .OrderBy(o => o.UpdatedAt)
                .Select(o => new PendingOfferResponseDto
                {
                    Id = o.Id,
                    Title = o.Title,
                    MerchantName = o.Merchant.Name,
                    DiscountPercentage = o.DiscountPercentage,
                    SubmittedAt = o.UpdatedAt
                })
                .ToListAsync();

            return Ok(offers);
        }

        [HttpPost("offers/{id}/approve")]
        public async Task<IActionResult> ApproveOffer(Guid id)
        {
            var adminId = GetCurrentUserId();

            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == id);
            if (offer == null)
                return NotFound("العرض غير موجود");

            if (offer.Status != OfferStatus.PendingApproval)
                return BadRequest("العرض ليس قيد المراجعة");

            var now = DateTime.UtcNow;

            offer.Status = offer.StartAt <= now && offer.EndAt >= now
                ? OfferStatus.Active
                : OfferStatus.Scheduled;

            offer.ApprovedAt = now;
            offer.ApprovedByUserId = adminId;
            offer.AdminReviewNote = null;
            offer.UpdatedAt = now;

            await _dbcontext.SaveChangesAsync();
            await _auditService.LogAsync(adminId, "ApproveOffer", "Offer", offer.Id,
                $"تمت الموافقة على العرض: {offer.Title} — الحالة: {offer.Status}");

            var merchantOwnerUserId = await _dbcontext.Merchants
                .Where(m => m.Id == offer.MerchantId)
                .Select(m => m.OwnerUserId)
                .FirstAsync();

            await _notificationService.NotifyAsync(
                merchantOwnerUserId,
                "تمت الموافقة على عرضك",
                $"تمت الموافقة على العرض \"{offer.Title}\"",
                "OfferApproved",
                "Offer",
                offer.Id);

            return Ok(offer);
        }

        [HttpPost("offers/{id}/reject")]
        public async Task<IActionResult> RejectOffer(Guid id, RejectRequestDto dto)
        {
            var adminId = GetCurrentUserId();
            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == id);
            if (offer == null)
                return NotFound("العرض غير موجود");

            offer.Status = OfferStatus.Rejected;
            offer.AdminReviewNote = dto.Reason;
            offer.ApprovedAt = null;
            offer.ApprovedByUserId = null;
            offer.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();
            await _auditService.LogAsync(adminId, "RejectOffer", "Offer", offer.Id,
                $"تم رفض العرض: {offer.Title} — السبب: {dto.Reason}");
            var merchantOwnerUserId = await _dbcontext.Merchants
                .Where(m => m.Id == offer.MerchantId)
                .Select(m => m.OwnerUserId)
                .FirstAsync();

            await _notificationService.NotifyAsync(
                merchantOwnerUserId,
                "تم رفض عرضك",
                $"تم رفض العرض \"{offer.Title}\". السبب: {dto.Reason}",
                "OfferRejected",
                "Offer",
                offer.Id);

            return Ok(offer);
        }

        [HttpGet("complaints")]
        public async Task<IActionResult> GetAllComplaints([FromQuery] ComplaintStatus? status)
        {
            var query = _dbcontext.Complaints.AsQueryable();

            if (status != null)
                query = query.Where(c => c.Status == status);

            var complaints = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    CreatedByName = c.CreatedByUser.FullName,
                    OrderNumber = c.Order != null ? c.Order.OrderNumber : null,
                    TargetName = c.Merchant != null ? c.Merchant.Name
                               : c.DriverProfile != null ? c.DriverProfile.User.FullName
                               : c.Offer != null ? c.Offer.Title
                               : null,
                    c.Category,
                    c.Description,
                    c.EvidenceUrl,
                    c.Status,
                    c.AdminResponse,
                    c.CreatedAt,
                    c.ResolvedAt
                })
                .ToListAsync();

            return Ok(complaints);
        }
        [HttpPut("complaints/{id}/resolve")]
        public async Task<IActionResult> ResolveComplaint(Guid id, ResolveComplaintRequestDto dto)
        {
            var adminId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(dto.AdminResponse))
                return BadRequest("الرد على الشكوى مطلوب");

            if (dto.Status != ComplaintStatus.Resolved
                && dto.Status != ComplaintStatus.Rejected
                && dto.Status != ComplaintStatus.UnderReview)
                return BadRequest("حالة غير صالحة");

            var complaint = await _dbcontext.Complaints.FirstOrDefaultAsync(c => c.Id == id);
            if (complaint == null)
                return NotFound("الشكوى غير موجودة");

            if (complaint.Status == ComplaintStatus.Resolved || complaint.Status == ComplaintStatus.Rejected)
                return BadRequest("الشكوى مغلقة بالفعل");

            complaint.Status = dto.Status;
            complaint.AdminResponse = dto.AdminResponse.Trim();

            if (dto.Status == ComplaintStatus.Resolved || dto.Status == ComplaintStatus.Rejected)
            {
                complaint.ResolvedByUserId = adminId;
                complaint.ResolvedAt = now;
            }

            await _dbcontext.SaveChangesAsync();
            await _auditService.LogAsync(adminId, "ResolveComplaint", "Complaint", complaint.Id,
                $"تم حل الشكوى — الرد: {dto.AdminResponse}");
            var title = dto.Status switch
            {
                ComplaintStatus.Resolved => "تم حل شكواك",
                ComplaintStatus.Rejected => "تم إغلاق شكواك",
                _ => "شكواك قيد المراجعة"
            };

            await _notificationService.NotifyAsync(
                complaint.CreatedByUserId,
                title,
                dto.AdminResponse,
                "ComplaintUpdated",
                "Complaint",
                complaint.Id);

            return Ok(new
            {
                complaint.Id,
                complaint.Status,
                complaint.AdminResponse,
                complaint.ResolvedAt
            });
        }
        [HttpPost("users/merchant")]
        public async Task<IActionResult> CreateMerchantUser([FromBody] CreateMerchantByAdminRequestDto dto)
        {
            var adminId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email)
                || string.IsNullOrWhiteSpace(dto.PhoneNumber) || string.IsNullOrWhiteSpace(dto.Password)
                || string.IsNullOrWhiteSpace(dto.MerchantName) || string.IsNullOrWhiteSpace(dto.ContactEmail)
                || string.IsNullOrWhiteSpace(dto.ContactPhone))
            {
                return BadRequest("جميع الحقول المطلوبة يجب تعبئتها");
            }

            if (dto.Password.Length < 6)
            {
                return BadRequest("كلمة المرور يجب أن تكون 6 أحرف على الأقل");
            }

            var email = dto.Email.Trim().ToLower();
            var phoneNumber = dto.PhoneNumber.Trim();

            var emailExists = await _dbcontext.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                return Conflict("البريد الإلكتروني مستخدم مسبقاً");
            }

            var phoneExists = await _dbcontext.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
            if (phoneExists)
            {
                return Conflict("رقم الهاتف مستخدم مسبقاً");
            }

            var now = DateTime.UtcNow;

            using var transaction = await _dbcontext.Database.BeginTransactionAsync();

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                Email = email,
                PhoneNumber = phoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.Merchant,
                Status = UserStatus.Active,
                ProfileImageUrl = null,
                LastLoginAt = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbcontext.Users.Add(user);

            var merchant = new Merchant
            {
                Id = Guid.NewGuid(),
                OwnerUserId = user.Id,
                Name = dto.MerchantName.Trim(),
                Description = dto.Description,
                RegistrationNumber = dto.RegistrationNumber,
                ContactEmail = dto.ContactEmail.Trim().ToLower(),
                ContactPhone = dto.ContactPhone.Trim(),
                LogoUrl = dto.LogoUrl,
                CoverImageUrl = dto.CoverImageUrl,
                VerificationStatus = VerificationStatus.Approved,
                RejectionReason = null,
                IsActive = true,
                AverageRating = null,
                CreatedAt = now,
                VerifiedAt = now,
                VerifiedByUserId = adminId
            };

            _dbcontext.Merchants.Add(merchant);

            await _dbcontext.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.LogAsync(adminId, "CreateMerchant", "Merchant", merchant.Id,
                $"أنشأ الأدمن حساب تاجر جديد: {merchant.Name} — {user.Email}");
            await _notificationService.NotifyAsync(
    user.Id,
    "تم إنشاء حسابك",
    $"تم إنشاء حساب متجر {merchant.Name} على COOP. يرجى تغيير كلمة المرور من إعدادات الحساب.",
    "AccountCreated",
    "Merchant",
    merchant.Id);
            var response = new CreateMerchantByAdminResponseDto
            {
                MerchantId = merchant.Id,
                OwnerUserId = user.Id,
                MerchantName = merchant.Name,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                VerificationStatus = merchant.VerificationStatus,
                CreatedAt = merchant.CreatedAt
            };

            return StatusCode(201, response);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] UserRole? role, [FromQuery] UserStatus? status,
    [FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1 || pageSize > 100)
            {
                pageSize = 20;
            }

            var query = _dbcontext.Users.AsQueryable();

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(u => u.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(term)
                                      || u.Email.Contains(term)
                                      || u.PhoneNumber.Contains(term));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserResponseDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();

            return Ok(new
            {
                items = users,
                totalCount,
                pageNumber,
                pageSize
            });
        }
        [HttpPut("users/{id}/suspend")]
        public async Task<IActionResult> SuspendUser(Guid id, [FromBody] SuspendUserRequestDto dto)
        {
            var adminId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return BadRequest("سبب التعليق مطلوب");
            }

            if (id == adminId)
            {
                return BadRequest("لا يمكنك تعليق حسابك الخاص");
            }

            var user = await _dbcontext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود");
            }

            if (user.Role == UserRole.Admin)
            {
                return BadRequest("لا يمكن تعليق حساب مشرف");
            }

            if (user.Status == UserStatus.Suspended)
            {
                return BadRequest("الحساب معلّق مسبقاً");
            }

            user.Status = UserStatus.Suspended;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();

            await _auditService.LogAsync(adminId, "SuspendUser", "User", user.Id,
                $"تم تعليق حساب: {user.Email} — السبب: {dto.Reason}");

            return NoContent();
        }

        [HttpPut("users/{id}/activate")]
        public async Task<IActionResult> ActivateUser(Guid id)
        {
            var adminId = GetCurrentUserId();

            var user = await _dbcontext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود");
            }

            if (user.Status == UserStatus.Active)
            {
                return BadRequest("الحساب مفعّل مسبقاً");
            }

            if (user.Status == UserStatus.Deleted)
            {
                return BadRequest("لا يمكن إعادة تفعيل حساب محذوف");
            }

            user.Status = UserStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();

            await _auditService.LogAsync(adminId, "ActivateUser", "User", user.Id,
                $"تمت إعادة تفعيل حساب: {user.Email}");

            return NoContent();
        }
        [HttpPost("users/driver")]
        public async Task<IActionResult> CreateDriverUser([FromBody] CreateDriverByAdminRequestDto dto)
        {
            var adminId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email)
                || string.IsNullOrWhiteSpace(dto.PhoneNumber) || string.IsNullOrWhiteSpace(dto.Password)
                || string.IsNullOrWhiteSpace(dto.VehicleType) || string.IsNullOrWhiteSpace(dto.VehiclePlateNumber))
            {
                return BadRequest("جميع الحقول المطلوبة يجب تعبئتها");
            }

            if (dto.Password.Length < 6)
            {
                return BadRequest("كلمة المرور يجب أن تكون 6 أحرف على الأقل");
            }

            if (dto.MaximumCapacity < 1)
            {
                return BadRequest("السعة القصوى يجب أن تكون 1 أو أكثر");
            }

            var email = dto.Email.Trim().ToLower();
            var phoneNumber = dto.PhoneNumber.Trim();

            var emailExists = await _dbcontext.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                return Conflict("البريد الإلكتروني مستخدم مسبقاً");
            }

            var phoneExists = await _dbcontext.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
            if (phoneExists)
            {
                return Conflict("رقم الهاتف مستخدم مسبقاً");
            }

            var now = DateTime.UtcNow;

            using var transaction = await _dbcontext.Database.BeginTransactionAsync();

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                Email = email,
                PhoneNumber = phoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.Driver,
                Status = UserStatus.Active,
                ProfileImageUrl = null,
                LastLoginAt = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbcontext.Users.Add(user);

            var driverProfile = new DriverProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                VehicleType = dto.VehicleType.Trim(),
                VehiclePlateNumber = dto.VehiclePlateNumber.Trim(),
                MaximumCapacity = dto.MaximumCapacity,
                VerificationStatus = VerificationStatus.Approved,
                RejectionReason = null,
                IsAvailable = false,
                CompletedDeliveries = 0,
                CreatedAt = now
            };

            _dbcontext.DriverProfiles.Add(driverProfile);

            await _dbcontext.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.LogAsync(adminId, "CreateDriver", "DriverProfile", driverProfile.Id,
                $"أنشأ الأدمن حساب سائق جديد: {user.FullName} — {user.Email}");

            await _notificationService.NotifyAsync(
                user.Id,
                "تم إنشاء حسابك",
                "تم إنشاء حسابك كسائق على COOP. يرجى تغيير كلمة المرور من إعدادات الحساب.",
                "AccountCreated",
                "DriverProfile",
                driverProfile.Id);

            var response = new CreateDriverByAdminResponseDto
            {
                DriverProfileId = driverProfile.Id,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                VehicleType = driverProfile.VehicleType,
                VehiclePlateNumber = driverProfile.VehiclePlateNumber,
                MaximumCapacity = driverProfile.MaximumCapacity,
                VerificationStatus = driverProfile.VerificationStatus,
                CreatedAt = driverProfile.CreatedAt
            };

            return StatusCode(201, response);
        }



        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}