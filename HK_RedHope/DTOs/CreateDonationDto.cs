using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HK_RedHope.DTOs
{
    public class CreateDonationDto
    {
        public string? CampaignName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }

        [DataType(DataType.Date)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime RegistrationDeadline { get; set; }

        [DataType(DataType.Date)]
        [JsonConverter(typeof(DateOnlyJsonConverter))] 
        public DateTime Date { get; set; }
        public string? TimeRange { get; set; }
        public int MaxRegistrations { get; set; }

        public string? RequiredBloodType { get; set; } 

        public int RequiredBloodVolume { get; set; } 
        public string? SupportGift { get; set; }

        public string Status { get; set; } = "Open";
    }

    public class DonationFilterDto
    {
        public Guid? Id { get; set; }

        [DataType(DataType.Date)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? ToDate { get; set; } 
        
        public string? City { get; set; }

        public List<string>? RequiredBloodTypes { get; set; }
    }

    public class DateOnlyJsonConverter : JsonConverter<DateTime>
    {
        private const string Format = "dd/MM/yyyy";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (DateTime.TryParseExact(str, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;

            throw new JsonException($"Giá trị '{str}' không đúng định dạng {Format}.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(Format));
        }
    }
}
