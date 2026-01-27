namespace Monolith.Domain.Enums;

/// <summary>
/// Durak gecikme sebepleri kategorileri
/// </summary>
public enum DelayReasonCategory
{
    /// <summary>
    /// Trafik yoğunluğu, köprü trafiği vb.
    /// </summary>
    Traffic = 1,

    /// <summary>
    /// Müşteri hazır değil, kapıda yok vb.
    /// </summary>
    CustomerNotReady = 2,

    /// <summary>
    /// Araç arızası, lastik patlaması vb.
    /// </summary>
    VehicleIssue = 3,

    /// <summary>
    /// Hava koşulları (yağmur, kar, sis vb.)
    /// </summary>
    Weather = 4,

    /// <summary>
    /// Yükleme/boşaltma işleminde gecikme
    /// </summary>
    UnloadingDelay = 5,

    /// <summary>
    /// Rota değişikliği, yol kapatma vb.
    /// </summary>
    RouteChange = 6,

    /// <summary>
    /// Kaza bölgesi, yol kapalı vb.
    /// </summary>
    AccidentArea = 7,

    /// <summary>
    /// Yemek molası, dinlenme vb.
    /// </summary>
    BreakTime = 8,

    /// <summary>
    /// Diğer sebepler
    /// </summary>
    Other = 99
}
