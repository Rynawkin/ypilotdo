using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monolith.WebAPI.Data.Journeys
{
    public class JourneyStopPhoto : BaseEntity
    {
        public int JourneyId { get; set; }
        public int StopId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string PhotoUrl { get; set; }
        
        [MaxLength(500)]
        public string ThumbnailUrl { get; set; }
        
        public int DisplayOrder { get; set; }
        
        [MaxLength(200)]
        public string Caption { get; set; }
        
        [MaxLength(200)]
        public string? ReceiverName { get; set; } // YENİ ALAN
        
        // Navigation properties
        [ForeignKey("JourneyId")]
        public Journey Journey { get; set; }
        
        [ForeignKey("StopId")]
        public JourneyStop Stop { get; set; }
    }
}