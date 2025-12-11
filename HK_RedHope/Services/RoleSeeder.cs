using Microsoft.AspNetCore.Identity;
using HK_RedHope.Models;

namespace HK_RedHope.Services
{
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public RoleSeeder(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration config)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _config = config;
        }

        public async Task SeedRolesAndAdminAsync()
        {
            Console.WriteLine("Bắt đầu seeding Roles, Admin và Doctor...");

            var roles = new[] { "User", "Admin", "Doctor" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                    Console.WriteLine($"Đã tạo role: {role}");
                }
            }

            string adminEmail = _config["AdminUser:Email"] ?? "admin@blood.local";
            string adminPwd = _config["AdminUser:Password"] ?? "Admin@123";

            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Admin",
                    EmailConfirmed = true,
                    Gender = "Unknown",
                    IdentificationNumber = "000000000000",
                    IsApproved = true,
                    IsRejected = false
                };

                var result = await _userManager.CreateAsync(admin, adminPwd);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(admin, "Admin");
                    Console.WriteLine($"Đã tạo tài khoản Admin: {adminEmail}");
                }
                else
                {
                    Console.WriteLine("Lỗi khi tạo Admin:");
                    foreach (var e in result.Errors)
                        Console.WriteLine($" - {e.Description}");
                }
            }
            else
            {
                Console.WriteLine("Admin đã tồn tại, bỏ qua seeding.");
            }

            string doctorEmail = _config["DoctorUser:Email"] ?? "doctor@blood.local";
            string doctorPwd = _config["DoctorUser:Password"] ?? "Doctor@123";

            var existingDoctor = await _userManager.FindByEmailAsync(doctorEmail);
            if (existingDoctor == null)
            {
                var doctor = new ApplicationUser
                {
                    UserName = doctorEmail,
                    Email = doctorEmail,
                    FullName = "Doctor",
                    EmailConfirmed = true,
                    Gender = "Unknown",
                    IdentificationNumber = "111111111111",
                    IsApproved = true,      
                    IsRejected = false
                };

                var result = await _userManager.CreateAsync(doctor, doctorPwd);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(doctor, "Doctor");
                    Console.WriteLine($"Đã tạo tài khoản Doctor: {doctorEmail}");
                }
                else
                {
                    Console.WriteLine("Lỗi khi tạo Doctor:");
                    foreach (var e in result.Errors)
                        Console.WriteLine($" - {e.Description}");
                }
            }
            else
            {
                Console.WriteLine("Doctor đã tồn tại, bỏ qua seeding.");
            }
        }
    }
}
