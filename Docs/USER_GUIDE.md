# Mini Muhasebe Uygulaması - Kullanıcı Kılavuzu

## İçindekiler

1. [Giriş ve Ana Ekran](#giriş-ve-ana-ekran)
2. [Firma Yönetimi](#firma-yönetimi)
3. [Gelir-Gider Kayıtları](#gelir-gider-kayıtları)
4. [Cari Hesap Yönetimi](#cari-hesap-yönetimi)
5. [Banka İşlemleri](#banka-işlemleri)
6. [Raporlama](#raporlama)
7. [Ayarlar ve Yedekleme](#ayarlar-ve-yedekleme)

---

## Giriş ve Ana Ekran

### Uygulamaya Giriş

1. Uygulamayı başlatın
2. Giriş ekranında kullanıcı adı ve şifre girin
3. **Giriş Yap** butonuna tıklayın

**Not:** İlk defa giriş yapıyorsanız varsayılan kimlik bilgilerini kullanın (Bkz: INSTALLATION.md)

### Ana Dashboard

Giriş yapıldıktan sonra ana dashboard'a yönlendirileceksiniz. Burada:

- **Aktif Firma:** Şu anda çalıştığınız firma
- **Aktif Dönem:** Seçili mali dönem
- **Hızlı İstatistikler:**
  - Aylık toplam gelir
  - Aylık toplam gider
  - Güncelke bakiye
  - Cari borç/alacak özeti

---

## Firma Yönetimi

### Yeni Firma Oluşturma

1. **Ayarlar → Firma Yönetimi** menüsüne gidin
2. **Yeni Firma** butonuna tıklayın
3. Aşağıdaki bilgileri girin:
   - **Firma Adı:** (Zorunlu)
   - **Vergi Müdürlüğü:** (Opsiyonel)
   - **Vergi No:** (Opsiyonel)
   - **Telefon:** (Opsiyonel)
   - **E-posta:** (Opsiyonel)
   - **Adres:** (Opsiyonel)
4. **Kaydet** butonuna tıklayın

### Mali Dönem Tanımlama

1. **Ayarlar → Mali Dönemler** menüsüne gidin
2. **Yeni Dönem** butonuna tıklayın
3. Aşağıdaki bilgileri girin:
   - **Başlangıç Tarihi:** (Zorunlu)
   - **Bitiş Tarihi:** (Zorunlu)
   - **Dönem Adı:** (Otomatik doldurulabilir - örneğin "2026 - Ocak")
4. **Kaydet** butonuna tıklayın

### Aktif Firma/Dönem Seçimi

- Ana ekranın üst sağında **Firma Seç** ve **Dönem Seç** dropdown'ları vardır
- Çalışmak istediğiniz firmayı ve dönemi seçin
- Seçim otomatik olarak kaydedilir

---

## Gelir-Gider Kayıtları

### Yeni Kayıt Ekleme

1. Ana menüden **Gelir-Gider Kayıtları** seçin
2. **Yeni Kayıt** butonuna tıklayın
3. Fiş detaylarını doldurun:
   - **Tarih:** İşlem tarihi (Zorunlu)
   - **Belge No:** Fatura/makbuz numarası (Opsiyonel)
   - **Açıklama:** İşlem açıklaması (Zorunlu)
   - **Tutar:** Brüt tutar (Zorunlu)
   - **KDV Oranı:** %0, %8, %18 vb. (Opsiyonel)
   - **KDV Tutarı:** Otomatik hesaplanır
   - **Net Tutar:** Otomatik hesaplanır
   - **Ödeme Tipi:**
     - **Nakit** - Kasa ile ödeme
     - **Banka** - Banka hesabından ödeme (hangi hesaptan seçin)
     - **Cari** - Cari hesaba borç/alacak
   - **Kategori:** Gelir, Gider, vb.
   - **İlgili Cari:** Cari ödeme seçildiyse cari hesap seçin

4. **Kaydet** butonuna tıklayın

### Kayıt Düzenleme

1. Listede düzenlemek istediğiniz kayıtı seçin
2. **Düzenle** butonuna tıklayın
3. Gerekli değişiklikleri yapın
4. **Güncelle** butonuna tıklayın

### Kayıt Silme

1. Listede silmek istediğiniz kayıtı seçin
2. **Sil** butonuna tıklayın
3. Onay ekranında **Evet** seçin

**Not:** Silme işlemi audit log'a kaydedilir ve yalnızca yöneticiler tarafından yapılabilir.

### Filtreleme ve Arama

- **Tarih Aralığı:** Başlangıç ve bitiş tarihini seçin
- **Kategori:** Belirli bir kategoriye göre filtrele
- **Açıklama:** Açıklama metinine göre ara
- **Ödeme Tipi:** Nakit, Banka, Cari vb. göre filtrele

---

## Cari Hesap Yönetimi

### Yeni Cari Kart Oluşturma

1. Ana menüden **Cari Hesaplar** seçin
2. **Yeni Cari Kart** butonuna tıklayın
3. Aşağıdaki bilgileri girin:
   - **Unvan:** Müşteri/tedarikçi adı (Zorunlu)
   - **Cari Tipi:** Müşteri / Tedarikçi (Zorunlu)
   - **Vergi No:** Kurumsal müşteri (Opsiyonel)
   - **TCKN:** Bireysel müşteri (Opsiyonel)
   - **Telefon:** (Opsiyonel)
   - **E-posta:** (Opsiyonel)
   - **Adres:** (Opsiyonel)
4. **Kaydet** butonuna tıklayın

### Cari Hareket Kaydı

1. Cari listeden bir cariyi seçin
2. **Hareketleri Gör** butonuna tıklayın
3. **Yeni Hareket** butonuna tıklayın
4. Hareket detaylarını girin:
   - **Tarih:** (Zorunlu)
   - **Tutar:** (Zorunlu)
   - **Türü:** Borç / Alacak
   - **Açıklama:** (Opsiyonel)
5. **Kaydet** butonuna tıklayın

### Cari Ekstre Görüntüleme

1. Cari listeden bir cariyi seçin
2. **Ekstre** butonuna tıklayın
3. Aşağıdakiler görüntülenecektir:
   - Tüm hareketler (tarih, tutar, tür)
   - Toplam borç
   - Toplam alacak
   - Bakiye (Borç - Alacak)

---

## Banka İşlemleri

### Banka Hesabı Tanımlama

1. Ana menüden **Banka Hesapları** seçin
2. **Yeni Hesap** butonuna tıklayın
3. Aşağıdaki bilgileri girin:
   - **Banka Adı:** (Zorunlu)
   - **Hesap Adı:** (Opsiyonel - örneğin "Maaş Hesabı")
   - **IBAN:** (Zorunlu)
   - **Para Birimi:** TRY, USD, EUR vb.
   - **API Entegrasyonu:** Evet/Hayır
   - **API Sağlayıcı:** (Entegrasyonu seçtiyseniz)
4. **Kaydet** butonuna tıklayın

### Banka API'den Hareketleri Çekme

1. Ana menüden **Banka Hareketleri** seçin
2. **Hareketleri Çek** butonuna tıklayın
3. Aşağıdaki parametreleri seçin:
   - **Banka Hesabı:** (Zorunlu)
   - **Başlangıç Tarihi:** (Zorunlu)
   - **Bitiş Tarihi:** (Zorunlu)
4. **Çek** butonuna tıklayın

Sistem otomatik olarak:
- Banka API'ye bağlanır
- Hareketleri indirir
- Mükerrer kayıtları kontrol eder
- Yeni hareketleri ekler
- İşlem tamamlandığında bildirim gösterir

### Banka Hareketlerini Eşleştirme

#### Manuel Eşleştirme

1. **Banka Hareketleri** ekranında eşleşmeyen hareketleri görürsünüz
2. Bir hareket seçin ve **Eşleştir** butonuna tıklayın
3. İlgili gelir/gider kaydını listeden seçin
4. **Eşleştir** butonuna tıklayın

#### Otomatik Eşleştirme

1. **Banka Hareketleri** ekranında **Otomatik Eşleştir** butonuna tıklayın
2. Sistem aşağıdaki kriterlere göre eşleştirmeye çalışır:
   - Tutar uyuşması (tam veya ±0.01 TRY tolerans)
   - Tarih yakınlığı (±3 gün)
   - Açıklama anahtar kelimeleri

### Banka Mutabakatı

1. **Raporlar → Banka Mutabakatı** menüsüne gidin
2. **Banka Hesabı** ve **Dönem** seçin
3. Aşağıdakiler görüntülenecektir:
   - Sistem bakiyesi (muhasebe kayıtlarından)
   - Banka bakiyesi (son API çekişinden)
   - Fark (varsa)
   - Farktaki hareketlerin listesi

---

## Raporlama

### Aylık Gelir-Gider Raporu

1. **Raporlar** menüsüne gidin
2. **Gelir-Gider Özeti** seçin
3. **Dönem** seçin
4. Rapor görüntülenecektir:
   - Kategori bazlı gelirler
   - Kategori bazlı giderler
   - Toplam gelir
   - Toplam gider
   - Net sonuç (Gelir - Gider)

### Nakit Akış Raporu

1. **Raporlar → Nakit Akış** menüsüne gidin
2. **Başlangıç Tarihi** ve **Bitiş Tarihi** seçin
3. Rapor görüntülenecektir:
   - Günlük nakit girişleri
   - Günlük nakit çıkışları
   - Kümülatif nakit akışı
   - Dönem başı bakiye
   - Dönem sonu bakiye

### Cari Ekstre Raporu

1. **Raporlar → Cari Ekstre** menüsüne gidin
2. **Cari Hesap** seçin
3. **Dönem** seçin
4. Rapor görüntülenecektir:
   - Tüm hareketler
   - Dönem başı bakiye
   - Dönem sonu bakiye
   - Debit/Kredi toplamları

### Raporları Dışa Aktarma

1. Herhangi bir raporda **Dışa Aktar** butonunu arayın
2. **Format seçin:**
   - **Excel (.xlsx)** - Excel'de açılabilir, filtrelenebilir
   - **CSV** - Tüm uygulamalarla uyumlu
   - **PDF** - Yazdırmaya uygun
3. **Dışa Aktar** butonuna tıklayın
4. Dosya otomatik olarak indirilecektir

---

## Ayarlar ve Yedekleme

### Profil Ayarları

1. **Ayarlar** menüsüne gidin
2. **Profil** kısmında:
   - Şifre değiştirebilirsiniz
   - E-posta adresinizi güncelleyebilirsiniz

### Yedek Alma

1. **Ayarlar → Yedekleme** menüsüne gidin
2. **Yedek Al** butonuna tıklayın
3. Yedek konumunu seçin (varsayılan: `DatabaseBackups/`)
4. İşlem tamamlandığında onay mesajı görüntülenecektir

### Yedekten Geri Yükleme

1. **Ayarlar → Yedekleme** menüsüne gidin
2. **Geri Yükle** butonuna tıklayın
3. Yedek dosyasını seçin
4. Onay verin
5. Uygulama otomatik olarak yeniden başlatılacaktır

**Uyarı:** Geri yükleme sonrasındaki veriler silinecektir. Lütfen işlemden emin olun.

---

**Son Güncelleme:** 2026-05-14
