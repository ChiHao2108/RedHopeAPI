using HK_RedHope.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserProfile
{

    [Key]
    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? BloodType { get; set; }
    public bool? HasDonatedBefore { get; set; }
    public DateTime? LastDonationDate { get; set; }
    public int? BloodVolumeToDonate { get; set; }
    public float? Weight { get; set; }
    public string? MedicalHistory { get; set; }
    public string? RiskBehavior { get; set; }
    public string? CurrentHealthStatus { get; set; }
    public bool? IsPregnant { get; set; }
    public DateTime RegistrationDate { get; set; }
    public bool IsApproved { get; set; }
    public bool IsRejected { get; set; }
    public DateTime? LastProfileUpdate { get; set; }
}
