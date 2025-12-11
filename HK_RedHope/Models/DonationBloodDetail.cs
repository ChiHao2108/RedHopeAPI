using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class DonationBloodDetail
{
    [Key]
    [ForeignKey("DonationBlood")]
    public Guid DonationBloodId { get; set; }

    public DonationBlood? Donation { get; set; }

    public string CampaignName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string RequiredBloodType { get; set; } = string.Empty;
    public int RequiredBloodVolume { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string TimeRange { get; set; } = string.Empty;
    public int RegisteredCount { get; set; }
    public int MaxRegistrations { get; set; }
    public string SupportGift { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
