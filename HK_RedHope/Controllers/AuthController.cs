using HK_RedHope.DTOs;
using HK_RedHope.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HK_RedHope.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;

        public AuthController(UserManager<ApplicationUser> userManager,
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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingEmail != null)
                return BadRequest(new { message = "Email đã tồn tại." });

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Gender = dto.Gender,
                PhoneNumber = dto.PhoneNumber 
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "User");

            return Ok(new
            {
                message = $"Đăng ký thành công! Chào mừng {dto.FullName} đến với hệ thống.",
                role = "User"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Sai thông tin đăng nhập." });

            var check = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!check.Succeeded)
                return Unauthorized(new { message = "Sai mật khẩu." });

            var roles = await _userManager.GetRolesAsync(user);
            var mainRole = roles.FirstOrDefault() ?? "User";

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? throw new ArgumentNullException(nameof(user.Id))),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""), 
                new Claim("fullname", user.FullName ?? ""),
                new Claim("role", mainRole ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var r in roles ?? Enumerable.Empty<string>()) 
                claims.Add(new Claim(ClaimTypes.Role, r));

            var keyString = _config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

            var expireHoursString = _config["Jwt:ExpireHours"] ?? "6";
            var expires = DateTime.UtcNow.AddHours(double.Parse(expireHoursString));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "", 
                audience: _config["Jwt:Audience"] ?? "",
                claims: claims,
                expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );


            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            string message = mainRole switch
            {
                "Admin" => $"Đăng nhập thành công! Xin chào Quản trị viên {user.FullName ?? user.Email}.",
                _ => $"Đăng nhập thành công! Xin chào {user.FullName ?? user.Email} (Người hiến máu)."
            };

            return Ok(new
            {
                message,
                role = mainRole,
                token = tokenString,
                expires = expires.ToString("o")
            });
        }
    }
}
