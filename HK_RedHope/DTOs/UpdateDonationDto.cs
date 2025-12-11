using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HK_RedHope.DTOs
{
    public class UpdateDonationDto
    {
        public string? CampaignName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }

        [DataType(DataType.Date)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? RegistrationDeadline { get; set; }

        [DataType(DataType.Date)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? Date { get; set; }

        public string? TimeRange { get; set; }
        public int? MaxRegistrations { get; set; }
        public string? RequiredBloodType { get; set; }
        
        public int? RequiredBloodVolume { get; set; }
        public string? SupportGift { get; set; }
    }
}
