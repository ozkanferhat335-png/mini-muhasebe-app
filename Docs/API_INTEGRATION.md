# Banka API Entegrasyonu Kılavuzu

## Genel Bilgi

Mini Muhasebe Uygulaması, banka API'lerinden otomatik olarak hesap hareketlerini çekerek muhasebe kayıtlarıyla eşleştirme yapabilir.

## Desteklenen Bankalar

### 1. Türkiye'de Yaygın Kullanılan API'ler

- **Banka Verilerini Açma Standardı (ODP)**
- **Finansal API Standartları (OpenBanking)**
- **Bireysel Banka API'leri**

## API Entegrasyonu Ayarları

### Adım 1: Banka API Kimlik Bilgilerini Alın

1. Bankanızın geliştirici portalına gidin (örneğin developer.example-bank.com)
2. Uygulamayı kaydedin ve aşağıdaki bilgileri alın:
   - **Client ID**
   - **Client Secret**
   - **API Key** (varsa)
   - **API Base URL** (örneğin: https://api.example-bank.com/v1)
   - **Redirect URI** (eğer OAuth kullanılıyorsa)

### Adım 2: Uygulamada API'yi Yapılandırın

1. **Ayarlar → Banka API Ayarları** menüsüne gidin
2. **Yeni API Ayarı** butonuna tıklayın
3. Aşağıdaki bilgileri girin:

```
Banka Adı: [Bankanın Adı]
API Türü: [REST/SOAP/OpenBanking]
API Base URL: [Bankanın sağladığı URL]
Client ID: [Alınan Client ID]
Client Secret: [Alınan Client Secret]
API Key: [API Key - opsiyonel]
Username: [Banka portalı kullanıcı adı - opsiyonel]
Password: [Banka portalı şifre - şifreli saklanacaktır]
```

4. **Test Bağlantısı** butonuna tıklayın
5. "Bağlantı başarılı" mesajı aldıysanız, **Kaydet** butonuna tıklayın

### Adım 3: Banka Hesabı ile Bağlayın

1. **Banka Hesapları** menüsüne gidin
2. Banka hesabını açın veya yeni oluşturun
3. **API Entegrasyonu:** "Evet" seçin
4. **API Sağlayıcı:** Yapılandırdığınız API'yi seçin
5. **Hesap ID** (eğer bankanın talep ettiği şekilde):
   - Bankanın API'den dönen hesap kimliğini girin
   - Veya **Bağlı Hesapları Listele** butonuna tıklayıp listeden seçin
6. **Kaydet** butonuna tıklayın

## API'den Hareketleri Çekme

### Manuel Çekme

1. **Banka Hareketleri → Hareketleri Çek** menüsüne gidin
2. Parametreleri seçin:
   - **Banka Hesabı:** (Zorunlu)
   - **Başlangıç Tarihi:** (Zorunlu)
   - **Bitiş Tarihi:** (Zorunlu)
   - **Sayfa Boyutu:** (Opsiyonel - varsayılan 100)
3. **Çek** butonuna tıklayın

Uygulama:
- API'ye istek gönderir
- Hareketleri indirir
- Veritabanında kontrol eder (mükerrer kayıt olup olmadığını)
- Yeni hareketleri ekler
- "Başarıyla X hareket eklendi" mesajını gösterir

### Otomatik Çekme (Zamanlanmış)

1. **Ayarlar → Zamanlanmış İşlemler** menüsüne gidin
2. **Banka Hareketleri Otomatik Çekme** seçeneğini açın
3. Ayarları yapılandırın:
   - **Çekme Sıklığı:** Günlük, Saatlik, vb.
   - **Çekme Saati:** (örneğin her gün 09:00'da)
   - **Geçmiş Gün:** (son kaç günü çekmek - varsayılan 7 gün)
4. **Kaydet** butonuna tıklayın

Uygulama arka planda otomatik olarak hareketleri çekmeye başlayacaktır.

## Hareketleri Eşleştirme

### Mükerrer Kayıt Kontrolü

Sistem her hareket için **Unique Transaction ID** (banka tarafından verilen) kontrolü yapar:
- Aynı ID ile kayıt varsa, hareket eklenmez (mükerrer kayıt işlemi engellenir)
- ID değişirse, ayrı kayıt olarak eklenir

### Otomatik Eşleştirme Kuralları

Sistem aşağıdaki kurallara göre otomatik eşleştirmeye çalışır:

1. **Tutar Eşleştirmesi:**
   - Banka hareketi tutarı = Gelir/Gider kaydı tutarı
   - Tolerans: ±0.01 TRY (ayarlanabilir)

2. **Tarih Eşleştirmesi:**
   - Banka hareket tarihi ≈ Kayıt tarihi
   - Tolerans: ±3 gün (ayarlanabilir)

3. **Açıklama Eşleştirmesi:**
   - Hareket açıklamasında cari hesap adı, fatura no, vb. aranır

### Manuel Eşleştirme

1. **Banka Hareketleri** ekranında eşleşmeyen hareketleri görürsünüz
2. Bir hareket seçin
3. **Eşleştir** butonuna tıklayın
4. **Eşleştirilecek Kayıt** listesinden seçin veya ara
5. **Eşleştir** butonuna tıklayın

Sistem:
- Hareketler arasında bağlantı kurar
- İşlem detaylarını güncelleştir (banka referans no, vb.)
- Tüm audit log'a kaydeder

## Sorun Giderme

### API Bağlantı Hatası

**Hata:** "Cannot connect to API"

**Nedenleri:**
1. İnternet bağlantısı kopuk
2. Yanlış API URL
3. API anahtarları/token'ı geçersiz veya süresi dolmuş
4. Firewall/proxy engeli

**Çözüm:**
1. İnternet bağlantısını kontrol edin
2. API URL'sini doğrulayın (https://, port numarası, vb.)
3. Bankanın geliştirici portalında token'ı yenileyin
4. Firewall ayarlarını kontrol edin veya sistem yöneticisine başvurun

### Kimlik Doğrulama Hatası

**Hata:** "Unauthorized" veya "403 Forbidden"

**Nedenleri:**
1. Client ID/Secret yanlış
2. API Key geçersiz
3. Hesap yetkileri yetersiz

**Çözüm:**
1. Client ID/Secret'i tekrar kontrol edin
2. Bankanın geliştirici portalında yetkileri kontrol edin
3. Hesaptan hareket okuma izni açık olup olmadığını kontrol edin

### Hareket Çekme Başarısız

**Hata:** "Failed to fetch transactions"

**Nedenleri:**
1. Tarih aralığı geçersiz
2. Hesap ID yanlış
3. API'de veri yok

**Çözüm:**
1. Tarih aralığını kontrol edin (başlangıç < bitiş)
2. Hesap ID'sini doğrulayın
3. Banka portalında işlemler var mı kontrol edin
4. Log dosyasını kontrol edin (`Logs/` klasöründe detaylı hatayı görürsünüz)

### Eşleştirme Başarısız

**Sorun:** Hareketler eşleşmiyor

**Nedenleri:**
1. Tutar farkı
2. Tarih farkı
3. Kayıt silinmiş veya düzeltilmiş

**Çözüm:**
1. Manuel eşleştirmeyi deneyin
2. Eşleştirme toleransını artırın (Ayarlar → Eşleştirme Kuralları)
3. Kaydı düzenleyip tutarı/tarihi doğrulayın

## Güvenlik Notları

✅ **API Anahtarları Şifreli Saklanır**
- Client Secret, API Key, şifre vb. AES-256 ile şifreli saklanır
- Veritabanında açık metin olarak saklanmaz

✅ **HTTPS Zorunlu**
- API bağlantıları her zaman HTTPS üzerinden yapılır
- Sertifika doğrulaması etkin

✅ **Rate Limiting**
- Çok fazla API isteğinin gönderilmesi engellenir
- Banka API'nin rate limit'ine uyulur

✅ **Audit Log**
- Tüm API işlemleri log'a kaydedilir
- Başarılı ve başarısız işlemler izlenir

## API Referans

### BankApiClient Sınıfı

```csharp
public class BankApiClient
{
    // Banka API'ye bağlan
    public bool Connect(BankApiSettings settings);
    
    // Hareketleri çek
    public List<BankTransaction> FetchTransactions(
        string accountId, 
        DateTime startDate, 
        DateTime endDate
    );
    
    // Test bağlantısı
    public bool TestConnection(BankApiSettings settings);
    
    // Hesapları listele
    public List<BankAccount> GetAccounts();
}
```

## Kontrol Lisesi

- [ ] Banka API kimlik bilgileri alındı
- [ ] Uygulamada API ayarları yapılandırıldı
- [ ] Test bağlantısı başarılı
- [ ] İlk hareket çekme yapıldı
- [ ] Eşleştirme kuralları kontrol edildi
- [ ] Otomatik çekme ayarlandı (opsiyonel)

---

**Son Güncelleme:** 2026-05-14
