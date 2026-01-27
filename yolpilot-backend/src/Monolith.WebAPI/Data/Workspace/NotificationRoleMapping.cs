using Monolith.WebAPI.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monolith.WebAPI.Data.Workspace
{
    [Table("NotificationRoleMapping")]
    public class NotificationRoleMapping : BaseEntity
    {
        public int WorkspaceId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string NotificationType { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ContactRole { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        // Navigation properties
        public Workspace? Workspace { get; set; }

        // Bildirim tipleri sabitleri
        public static class NotificationTypes
        {
            public const string JourneyStart = "JourneyStart";
            public const string JourneyCheckIn = "JourneyCheckIn";
            public const string DeliveryCompleted = "DeliveryCompleted";
            public const string DeliveryFailed = "DeliveryFailed";

            public static readonly Dictionary<string, string> DisplayNames = new()
            {
                { JourneyStart, "Araç Yola Çıktı" },
                { JourneyCheckIn, "Sürücü Yaklaştı" },
                { DeliveryCompleted, "Teslimat Tamamlandı" },
                { DeliveryFailed, "Teslimat Başarısız" }
            };

            public static List<string> GetAllTypes() => DisplayNames.Keys.ToList();
            
            public static List<KeyValuePair<string, string>> GetTypeOptions() 
                => DisplayNames.ToList();
        }

        public string GetNotificationTypeDisplayName()
        {
            return NotificationTypes.DisplayNames.ContainsKey(NotificationType) 
                ? NotificationTypes.DisplayNames[NotificationType] 
                : NotificationType;
        }

        public string GetContactRoleDisplayName()
        {
            return CustomerContact.Roles.DisplayNames.ContainsKey(ContactRole)
                ? CustomerContact.Roles.DisplayNames[ContactRole]
                : ContactRole;
        }
    }
}