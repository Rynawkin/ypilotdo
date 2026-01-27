// src/Monolith.WebAPI/Applications/Commands/Drivers/DriverDtos.cs

using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Applications.Commands.Drivers;

public class CreateDriverDto
{
    [Required(ErrorMessage = "Sürücü adı zorunludur")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Telefon numarası zorunludur")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email adresi zorunludur")] // ✅ ARTIK ZORUNLU
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi girin")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Şifre zorunludur")] // ✅ YENİ ALAN
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Ehliyet numarası zorunludur")]
    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;
    
    public int? VehicleId { get; set; }
    
    [RegularExpression("^(available|busy|offline)$", ErrorMessage = "Geçersiz durum")]
    public string Status { get; set; } = "available";
    
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    
    public string? Avatar { get; set; }
    
    [Range(0, 5, ErrorMessage = "Puan 0-5 arasında olmalıdır")]
    public double? Rating { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Teslimat sayısı negatif olamaz")]
    public int TotalDeliveries { get; set; } = 0;
    
    // ✅ YENİ: Otomatik şifre oluşturma ve bilgilendirme seçenekleri
    public bool GenerateRandomPassword { get; set; } = false;
    public bool SendCredentialsBySms { get; set; } = false;
    public bool SendCredentialsByEmail { get; set; } = true;
}

public class UpdateDriverDto
{
    [Required(ErrorMessage = "Sürücü adı zorunludur")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Telefon numarası zorunludur")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    // ✅ Email güncelleme sırasında opsiyonel kalabilir (mevcut email korunur)
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi girin")]
    [MaxLength(100)]
    public string? Email { get; set; }
    
    // ✅ Şifre güncelleme opsiyonel
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    [MaxLength(100)]
    public string? NewPassword { get; set; }
    
    [Required(ErrorMessage = "Ehliyet numarası zorunludur")]
    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;
    
    public int? VehicleId { get; set; }
    
    [RegularExpression("^(available|busy|offline)$", ErrorMessage = "Geçersiz durum")]
    public string Status { get; set; } = "available";
    
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    
    public string? Avatar { get; set; }
    
    [Range(0, 5, ErrorMessage = "Puan 0-5 arasında olmalıdır")]
    public double? Rating { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Teslimat sayısı negatif olamaz")]
    public int TotalDeliveries { get; set; } = 0;
}

public class UpdateDriverStatusDto
{
    [Required(ErrorMessage = "Durum zorunludur")]
    [RegularExpression("^(available|busy|offline)$", ErrorMessage = "Geçersiz durum")]
    public string Status { get; set; } = string.Empty;
}

public class BulkImportDriverDto
{
    [Required(ErrorMessage = "Sürücü adı zorunludur")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Telefon numarası zorunludur")]
    public string Phone { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email zorunludur")] // ✅ Bulk import'ta da email zorunlu
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Ehliyet numarası zorunludur")]
    public string LicenseNumber { get; set; } = string.Empty;
    
    public string Status { get; set; } = "available";
    public double? Rating { get; set; }
    public int TotalDeliveries { get; set; } = 0;
    
    // ✅ Bulk import için varsayılan şifre kullanılacak
    public string? Password { get; set; }
}