-- Mini Muhasebe Uygulaması - Temiz Başlangıç Verileri
-- Özkan Ferhat - 2026-05-14
-- Bu dosya yalnızca zorunlu sistem verilerini içerir (test verisi yok)

-- Rolleri Ekle
INSERT OR IGNORE INTO Roles (RoleId, RoleName, Description) VALUES
(1, 'Admin', 'Yönetici - Tüm izinlere sahip'),
(2, 'User', 'Standart Kullanıcı - Okuma ve temel yazma izni');

-- Varsayılan Admin Kullanıcısı
-- Username: admin  |  Password: Admin123!
-- Not: İlk girişten sonra şifrenizi değiştirin!
INSERT OR IGNORE INTO Users (UserId, Username, Email, PasswordHash, PasswordSalt, RoleId, IsActive) VALUES
(1, 'admin', 'admin@minimuhasebe.local',
    'PLACEHOLDER_HASH',   -- DatabaseInitializer tarafından PBKDF2 ile üretilir
    'PLACEHOLDER_SALT',
    1, 1);

-- Uygulama Ayarları
INSERT OR IGNORE INTO AppSettings (SettingKey, SettingValue, SettingType, Description) VALUES
('DefaultCurrency',    'TRY',          'String',  'Varsayılan Para Birimi'),
('DateFormat',         'dd.MM.yyyy',   'String',  'Tarih Formatı'),
('DecimalSeparator',   ',',            'String',  'Ondalık Ayraçı'),
('ThousandsSeparator', '.',            'String',  'Binler Ayraçı'),
('VatRates',           '[0,1,8,18]',   'String',  'KDV Oranları (%)'),
('AmountTolerance',    '0.01',         'Decimal', 'Eşleştirme Tutar Toleransı'),
('DateTolerance',      '3',            'Integer', 'Eşleştirme Tarih Toleransı (Gün)'),
('AutoBackupEnabled',  '1',            'Boolean', 'Otomatik Yedekleme Aktif'),
('AutoBackupTime',     '23:00',        'String',  'Otomatik Yedekleme Saati'),
('MaxBackupCount',     '10',           'Integer', 'Maksimum Yedek Dosyası Sayısı'),
('LogLevel',           'Info',         'String',  'Log Seviyesi (Debug/Info/Warning/Error)'),
('AppVersion',         '1.0.0',        'String',  'Uygulama Versiyonu'),
('CompanyName',        '',             'String',  'Aktif Firma Adı (Önbellek)'),
('LastBackupDate',     '',             'String',  'Son Yedekleme Tarihi');
