using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Monolith.WebAPI.Applications.Commands.Journeys;
using Monolith.WebAPI.Data.Journeys.Events;

namespace Monolith.WebAPI.Data.Journeys;

public class JourneyStatus : BaseEntity
{
    // Do not remove this constructor
    public JourneyStatus()
    {
    }

    public JourneyStatus(int journeyId, int stopId, JourneyStatusType status)
    {
        JourneyId = journeyId;
        StopId = stopId;
        Status = status;
        Notes = null;
        Latitude = 0;
        Longitude = 0;
        AdditionalValues = null;
        FailureReason = null;
        SignatureBase64 = null;
        PhotoBase64 = null;
        SignatureUrl = null;
        PhotoUrl = null;
        ReceiverName = null; // YENİ ALAN
    }

    public JourneyStatus(AddJourneyStatusCommand command)
    {
        JourneyId = command.JourneyId;
        StopId = command.StopId;
        Status = command.Status;
        Notes = command.Notes;
        Latitude = command.Latitude;
        Longitude = command.Longitude;
        AdditionalValues = command.AdditionalValues;
        
        // ✅ YENİ FIELD'LAR
        FailureReason = command.FailureReason;
        SignatureBase64 = command.SignatureBase64;
        PhotoBase64 = command.PhotoBase64;
        ReceiverName = command.ReceiverName; // YENİ ALAN
        // URL'ler command'dan gelmez, upload sonrası set edilir
        SignatureUrl = null;
        PhotoUrl = null;

        //AddDomainEvent(new JourneyStatusAddedEvent(this));
    }

    public int JourneyId { get; set; }
    public int StopId { get; set; }

    public JourneyStatusType Status { get; set; }

    [MaxLength(1024)] 
    public string Notes { get; set; }

    // ✅ YENİ EKLENEN FIELD'LAR
    [MaxLength(500)]
    public string FailureReason { get; set; }
    
    // ✅ TESLİM ALAN KİŞİ - YENİ ALAN
    [MaxLength(200)]
    public string ReceiverName { get; set; }
    
    // Base64 fields (legacy support için tutulabilir)
    [MaxLength(int.MaxValue)] // Base64 string uzun olabilir
    public string SignatureBase64 { get; set; }
    
    [MaxLength(int.MaxValue)] // Base64 string uzun olabilir
    public string PhotoBase64 { get; set; }
    
    // ✅ V38 - YENİ URL FIELD'LARI
    [MaxLength(500)]
    public string SignatureUrl { get; set; }
    
    [MaxLength(500)]
    public string PhotoUrl { get; set; }

    public Dictionary<string, string> AdditionalValues { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [ForeignKey("JourneyId")] public Journey Journey { get; set; }

    [ForeignKey("StopId")] public JourneyStop Stop { get; set; }
}

public enum JourneyStatusType
{
    InTransit = 200, // Yolda
    Arrived = 300, // Varış yapıldı
    Processing = 400, // İşlem yapılıyor (yükleme, boşaltma, vb.)
    Completed = 500, // İşlem tamamlandı
    Delayed = 600, // Gecikme var
    Cancelled = 700, // İptal edildi (Başarısız durak için kullanılıyor)
    OnHold = 800 // Bekletiliyor
}