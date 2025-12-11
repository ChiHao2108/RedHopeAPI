using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class DonationBloodResult
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DonationHistoryId { get; set; }

    [ForeignKey(nameof(DonationHistoryId))]
    public DonationHistory? DonationHistory { get; set; }

    [Required]
    public bool IsEligible { get; set; }

    public string? BloodType { get; set; }
    public string? MedicalHistory { get; set; }
    public string? CurrentHealthStatus { get; set; }
    public string? RiskBehavior { get; set; }
    public bool? IsPregnant { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
