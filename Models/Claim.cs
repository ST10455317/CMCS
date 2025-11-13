using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CMCS.Models
{
 
        public enum ClaimStatus { Pending, Approved, Rejected, Settled }

        public class Claim
        {
            [Key]
            public int Id { get; set; }

            [Required] public string LecturerName { get; set; }
            [Required] public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

            [Required]
            [Range(0.1, 1000)]
            public double HoursWorked { get; set; }

            [Required]
            [Range(0, 10000)]
            public decimal HourlyRate { get; set; }

            [NotMapped]
            public decimal Total => (decimal)HoursWorked * HourlyRate;

            public string Notes { get; set; }

            public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

            
            public string UploadedFileName { get; set; }
        }
    }
