// src/utils/timeFormat.ts

// Saat inputu için otomatik format (09:00 formatında)
export const formatTimeInput = (text: string): string => {
  // Sadece rakamları al
  const numbers = text.replace(/[^\d]/g, '');
  
  if (numbers.length <= 2) {
    return numbers;
  } else if (numbers.length <= 4) {
    return `${numbers.slice(0, 2)}:${numbers.slice(2)}`;
  }
  return `${numbers.slice(0, 2)}:${numbers.slice(2, 4)}`;
};

// Time window için backend formatına çevir
export const formatTimeForBackend = (time: string): string | null => {
  if (!time) return null;
  
  // "09:00" formatını "09:00:00" formatına çevir
  const parts = time.split(':');
  
  if (parts.length === 2) {
    const hours = parts[0].padStart(2, '0');
    const minutes = parts[1].padStart(2, '0');
    return `${hours}:${minutes}:00`;
  } else if (parts.length === 3) {
    return time;
  }
  
  return null;
};

// Backend'den gelen time'ı display formatına çevir
export const formatTimeForDisplay = (time: string | null): string => {
  if (!time) return '';
  
  // "09:00:00" veya "09:00" formatını "09:00" formatına çevir
  const parts = time.split(':');
  if (parts.length >= 2) {
    return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`;
  }
  
  return time;
};

// Saat validasyonu
export const isValidTime = (time: string): boolean => {
  if (!time) return false;
  
  const parts = time.split(':');
  if (parts.length < 2) return false;
  
  const hours = parseInt(parts[0], 10);
  const minutes = parseInt(parts[1], 10);
  
  return hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59;
};

// İki saat arasındaki farkı dakika olarak hesapla
export const getTimeDifferenceInMinutes = (startTime: string, endTime: string): number => {
  const [startHours, startMinutes] = startTime.split(':').map(Number);
  const [endHours, endMinutes] = endTime.split(':').map(Number);
  
  const startTotalMinutes = startHours * 60 + startMinutes;
  const endTotalMinutes = endHours * 60 + endMinutes;
  
  return endTotalMinutes - startTotalMinutes;
};

// Dakikayı saat:dakika formatına çevir
export const minutesToTimeString = (minutes: number): string => {
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  
  if (hours > 0) {
    return `${hours} saat ${mins > 0 ? mins + ' dk' : ''}`.trim();
  }
  return `${mins} dk`;
};

// TimeSpan formatı için (.NET backend)
export const toTimeSpan = (time: string): string => {
  if (!time) return '00:00:00';
  
  const parts = time.split(':');
  if (parts.length === 2) {
    return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}:00`;
  }
  
  return time;
};