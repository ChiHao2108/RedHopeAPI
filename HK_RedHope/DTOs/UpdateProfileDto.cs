using HK_RedHope.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HK_RedHope.DTOs
{
    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }

        [DataType(DataType.Date)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? DateOfBirth { get; set; }

        public string? BloodType { get; set; }

        public int? BloodVolumeToDonate { get; set; }

        public float? Weight { get; set; }

        public bool? HasDonatedBefore { get; set; }

        [DataType(DataType.Date)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? LastDonationDate { get; set; }

        public string? MedicalHistory { get; set; }
        public string? RiskBehavior { get; set; }
        public string? CurrentHealthStatus { get; set; }
        public bool? IsPregnant { get; set; }
    }

}
