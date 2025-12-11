namespace HK_RedHope.DTOs
{
    public class ExaminationUpdateDto
    {
        public string? BloodType { get; set; }
        public string? MedicalHistory { get; set; }
        public string? RiskBehavior { get; set; }

        public string? CurrentHealthStatus { get; set; }
        public bool? IsPregnant { get; set; }
        public bool IsEligible { get; set; } 
    }
}
