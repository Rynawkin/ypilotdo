using Monolith.WebAPI.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monolith.WebAPI.Data.Workspace
{
    [Table("CustomerContact")]
    public class CustomerContact : BaseEntity
    {
        public int WorkspaceId { get; set; }
        public Workspace? Workspace { get; set; }
        
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty; // DepoSorumlusu, SatinalmasorumluSu, MuhasebeSorumlusu, Diger

        public bool IsActive { get; set; } = true;
        public bool IsPrimary { get; set; } = false;

        // Bildirim tercihleri
        public bool ReceiveJourneyStart { get; set; } = true;
        public bool ReceiveJourneyCheckIn { get; set; } = true;
        public bool ReceiveDeliveryCompleted { get; set; } = true;
        public bool ReceiveDeliveryFailed { get; set; } = true;
        public bool ReceiveJourneyAssigned { get; set; } = true;
        public bool ReceiveJourneyCancelled { get; set; } = true;

        // Computed property for full name
        public string FullName => $"{FirstName} {LastName}";

        // Computed property for display text
        public string DisplayText => $"{FullName} ({GetRoleDisplayName()}) - {Email}";

        public string GetRoleDisplayName()
        {
            return Role switch
            {
                "DepoSorumlusu" => "Depo Sorumlusu",
                "SatinalmasorumluSu" => "Satınalma Sorumlusu",
                "MuhasebeSorumlusu" => "Muhasebe Sorumlusu",
                "Diger" => "Diğer",
                _ => Role
            };
        }

        // Rol sabitleri
        public static class Roles
        {
            public const string DepoSorumlusu = "DepoSorumlusu";
            public const string SatinalmasorumluSu = "SatinalmasorumluSu";
            public const string MuhasebeSorumlusu = "MuhasebeSorumlusu";
            public const string Diger = "Diger";

            public static readonly Dictionary<string, string> DisplayNames = new()
            {
                { DepoSorumlusu, "Depo Sorumlusu" },
                { SatinalmasorumluSu, "Satınalma Sorumlusu" },
                { MuhasebeSorumlusu, "Muhasebe Sorumlusu" },
                { Diger, "Diğer" }
            };

            public static List<string> GetAllRoles() => DisplayNames.Keys.ToList();
            
            public static List<KeyValuePair<string, string>> GetRoleOptions() 
                => DisplayNames.ToList();
        }
    }
}