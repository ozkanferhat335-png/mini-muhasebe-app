-- Sadece rolleri ve admin kullanıcısını ekle (minimal setup)

INSERT OR IGNORE INTO Roles (RoleId, RoleName, Description) VALUES
(1, 'Admin', 'Yönetici - Tüm izinlere sahip'),
(2, 'User', 'Standart Kullanıcı - Okuma ve temel yazma izni');

-- Admin: admin / Admin123!
INSERT OR IGNORE INTO Users (UserId, Username, Email, PasswordHash, PasswordSalt, RoleId, IsActive) VALUES
(1, 'admin', 'admin@minimuhasebe.local', 
    '2A9E9C7F9D8B1E5C5F5E5D5C5B5A59585756555453525150',
    '1A2B3C4D5E6F7A8B9C0D1E2F3A4B5C6D',
    1, 1);

INSERT OR IGNORE INTO AppSettings (SettingKey, SettingValue, SettingType, Description) VALUES
('DefaultCurrency', 'TRY', 'String', 'Varsayılan Para Birimi'),
('DateFormat', 'dd.MM.yyyy', 'String', 'Tarih Formatı'),
('DecimalSeparator', ',', 'String', 'Ondalık Ayraçı'),
('ThousandsSeparator', '.', 'String', 'Binler Ayraçı'),
('VatRates', '[0, 1, 8, 18]', 'String', 'KDV Oranları'),
('AmountTolerance', '0.01', 'Decimal', 'Eşleştirme Toleransı'),
('DateTolerance', '3', 'Integer', 'Gün Cinsinden Tarih Toleransı'),
('AutoBackupEnabled', '1', 'Boolean', 'Otomatik Yedekleme Aktif'),
('AutoBackupTime', '23:00', 'String', 'Otomatik Yedekleme Saati'),
('MaxBackupCount', '10', 'Integer', 'En Fazla Yedek Sayısı'),
('LogLevel', 'Info', 'String', 'Log Seviyesi');
