using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HK_RedHope.Models
{
    public class ApplicationUser : IdentityUser, IValidatableObject
    {
        public string? IdentificationNumber { get; set; }

        public string? FullName { get; set; }

        public string? Gender { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public new string? PhoneNumber { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }

        [DataType(DataType.Date)]
        [JsonConverter(typeof(DateOnlyJsonConverter))] 
        public DateTime? DateOfBirth { get; set; }

        public string? BloodType { get; set; }

        public bool? HasDonatedBefore { get; set; }

        [DataType(DataType.Date)]
        [JsonConverter(typeof(DateOnlyJsonConverter))] 
        public DateTime? LastDonationDate { get; set; } 

        [Range(250, 450, ErrorMessage = "Số lượng máu hiến phải từ 250ml tới 450ml.")]
        public int? BloodVolumeToDonate { get; set; } 

        [Range(30, 200)]
        public float? Weight { get; set; } 

        public string? MedicalHistory { get; set; } 

        public string? RiskBehavior { get; set; } 

        public string? CurrentHealthStatus { get; set; } 

        public bool? IsPregnant { get; set; } 

        [DataType(DataType.Date)]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false; 
        public bool IsRejected { get; set; } = false;

        [DataType(DataType.Date)]
        public DateTime? LastProfileUpdate { get; set; } = DateTime.Now;

        public UserProfile? Profile { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var today = DateTime.Today;

            if (DateOfBirth.HasValue && DateOfBirth.Value > today)
                yield return new ValidationResult(
                    "Ngày sinh không được lớn hơn ngày hiện tại.",
                    new[] { nameof(DateOfBirth) });

            if (HasDonatedBefore.HasValue && !HasDonatedBefore.Value && LastDonationDate.HasValue)
            {
                yield return new ValidationResult(
                    "Người chưa từng hiến máu không được nhập Ngày hiến gần nhất.",
                    new[] { nameof(LastDonationDate) });
            }

            if (HasDonatedBefore.HasValue && HasDonatedBefore.Value && !LastDonationDate.HasValue)
            {
                yield return new ValidationResult(
                    "Người đã từng hiến máu phải nhập Ngày hiến gần nhất.",
                    new[] { nameof(LastDonationDate) });
            }

            if (BloodVolumeToDonate.HasValue && (BloodVolumeToDonate < 250 || BloodVolumeToDonate > 450))
                yield return new ValidationResult(
                    "Số lượng máu hiến phải từ 250ml tới 450ml.",
                    new[] { nameof(BloodVolumeToDonate) });
        }
    }

    public class DateOnlyJsonConverter : JsonConverter<DateTime?>
    {
        private const string Format = "dd/MM/yyyy";

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (string.IsNullOrWhiteSpace(reader.GetString())) return null;
            return DateTime.ParseExact(reader.GetString()!, Format, null);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString(Format));
            else
                writer.WriteNullValue();
        }
    }
}
