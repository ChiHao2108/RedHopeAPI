using HK_RedHope.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public class DonationBlood
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string CampaignName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateTime Date { get; set; }

    public DateTime RegistrationDeadline { get; set; }

    [Required]
    public string RequiredBloodType { get; set; } = string.Empty;

    public int RequiredBloodVolume { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    public string TimeRange { get; set; } = string.Empty;

    public int RegisteredCount { get; set; } = 0;

    public int MaxRegistrations { get; set; } = 0; 

    public string SupportGift { get; set; } = string.Empty;

    public string Status { get; set; } = "Open"; 

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DonationStatus
{
    Registered,
    Scheduled,
    Approved,        
    Rejected,       
    NotArrived,     
    Completed,
    Cancelled
}

public class DonationHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    public Guid DonationBloodId { get; set; }

    [ForeignKey("DonationBloodId")]
    public DonationBlood? Donation { get; set; }

    [Required]
    public DonationStatus Status { get; set; } = DonationStatus.Registered;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public int QueueNumber { get; set; }

    public DonationBloodDetail? Detail { get; set; }

}

public class UpdateDonationStatusDto
{
    public List<Guid> HistoryIds { get; set; } = new();
    public DonationStatus Status { get; set; }
}
