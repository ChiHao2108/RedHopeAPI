using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HK_RedHope.Models;

namespace HK_RedHope.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DonationBlood> DonationBloods { get; set; }
        public DbSet<DonationHistory> DonationHistories { get; set; }

        public DbSet<DonationBloodResult> DonationBloodResults { get; set; }

    }
}
