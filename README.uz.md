<p align="center">
  <img src="BuranUI/Assets/Buran.png" alt="Buran" width="168">
</p>

<h1 align="center">Buran Music Library Manager</h1>

<p align="center">
  <strong>Musiqa kutubxonasini boshqarish, teglarni tuzatish va tinglash.</strong><br>
  Linux (Fedora / Bazzite) va Windows uchun ish stoli ilovasi.
</p>

<p align="center">
  <a href="README.md">Deutsch</a>
  ·
  <a href="README.en.md">English</a>
  ·
  <a href="README.ru.md">Русский</a>
  ·
  <strong>O'zbekcha</strong>
</p>

<p align="center">
  <a href="https://github.com/88Radium/BuranMusicLibraryManager/releases">Yuklab olish</a>
  ·
  <a href="#ornatish">O'rnatish</a>
  ·
  <a href="#funksiyalar">Funksiyalar</a>
  ·
  <a href="#ozingiz-yiging">O'zingiz yiging</a>
</p>

---

Buran katta MP3/FLAC to'plamini tartibda tutmoqchi bo'lganlar uchun: bir xil ijrochi nomlari, toza fayl nomlari, janrlar va kayfiyatlar, yonida esa ijro. **O'z katalogingiz** — haqiqat manbai, MusicBrainz yoki iTunes emas. Bir marta belgilagan narsa (afzal yozuv, muqobil, blok ro'yxati) keyingi jild ochilishida o'zi qo'llaniladi.

Teg o'zgarishlari **darhol faylga yoziladi**. Alohida Saqlash tugmasi yo'q.

| | |
|---|---|
| Fayllar | MP3 va FLAC |
| Platformalar | Linux (RPM, jumladan Fedora / Bazzite) va Windows |
| Pleyer | libVLC paket ichida |
| Tillar | nemis, ingliz, rus, o'zbek (lotin), tizim tili |
| Ma'lumot | Katalog va sozlamalar foydalanuvchi jildida, musiqa kutubxonasida emas |

Ilovaning o'zida shu qo'llanma **Sozlamalar → Qo'llanma** ostida.

## Mundarija

- [O'rnatish](#ornatish)
- [Birinchi ishga tushirish](#birinchi-ishga-tushirish)
- [Odatiy muammolar](#odatiy-muammolar)
- [Funksiyalar](#funksiyalar)
  - [Sozlamalar](#sozlamalar)
  - [Kutubxona](#kutubxona)
  - [ID3 tahrirchi](#id3-tahrirchi)
  - [Ma'lumotlar bazasi uchun yangi qiymatlar](#malumotlar-bazasi-uchun-yangi-qiymatlar)
  - [Pleyer](#pleyer)
  - [MB tahrirchi](#mb-tahrirchi)
  - [Katalog modeli](#katalog-modeli)
- [Buran ma'lumotni qayerda saqlaydi](#buran-malumotni-qayerda-saqlaydi)
- [GitHub relizlari](#github-relizlari)
- [O'zingiz yiging](#ozingiz-yiging)

## O'rnatish

Tayyor o'rnatuvchilar **[Releases](https://github.com/88Radium/BuranMusicLibraryManager/releases)** sahifasida.

### Linux (Fedora, RHEL, Bazzite)

1. Kerakli relizning `.rpm` faylini yuklab oling (masalan `buran-0.1.6-4.x86_64.rpm`).
2. **Bazzite / rpm-ostree** da:

```bash
sudo rpm-ostree uninstall buran --install /yol/buran-*.x86_64.rpm
sudo systemctl reboot
```

Oldingi paket bo'lmasa, birinchi o'rnatish:

```bash
sudo rpm-ostree install /yol/buran-*.x86_64.rpm
sudo systemctl reboot
```

3. **Buran Music Library Manager** yorlig'i yoki `buran` buyrug'i bilan ishga tushiring.

`/usr/lib/buran/BuranUI` ni to'g'ridan-to'g'ri chaqirmang. O'rama `LD_LIBRARY_PATH` va `VLC_PLUGIN_PATH` ni o'rnatadi, shunda pleyer paketdagi VLC kutubxonalarini topadi.

Debian/Ubuntu: relizga `.deb` ham qo'shilgan. Bu hamroh paket; ichidagi Linux kutubxonalari Fedora dan.

### Windows

1. `Buran-<Versiya>-win-x64-setup.exe` ni yuklab oling va ishga tushiring.
2. O'rnatish tilini tanlang: nemis, ingliz, rus yoki o'zbek (lotin).
3. Ilova `%LOCALAPPDATA%\Programs\Buran` ga va Boshlash menyusiga tushadi.
4. Yoki portativ `.zip` ni ochib `BuranUI.exe` ni ishga tushiring.

Windows birinchi ishga tushirishda SmartScreen ko'rsatishi mumkin (fayl raqamli imzolanmagan). Unda **Batafsil → Baribir bajarish**.

libVLC va `ffmpeg.exe` (spektrogramma uchun) Windows paketida bor. Alohida VLC yoki ffmpeg o'rnatish shart emas.

## Birinchi ishga tushirish

1. Buranni oching.
2. Chapda **Jild qo'shish** va musiqa kutubxonasi ildizini tanlang (masalan `Musiqa`).
3. Daraxtida ko'k nuqtali ichki jildni bosing — u yerda MP3/FLAC bor.
4. Fayllar **ID3 tahrirchi** tabida chiqadi.
5. Yuqorida **Teglarni tahrirlash** — tekshiruvchi va ommaviy amallar ochiladi.

Bir nechta ildiz mumkin (ichki disk va NAS ulushi). Ro'yxatni Buran eslab qoladi.

## Odatiy muammolar

Qaysi funksiya qaysi chalkashlikka yordam beradi:

| Muammo | Funksiya |
|---|---|
| Bir ijrochi besh xil yozuvda (`Eminem`, `EMINEM`, `M&M`) | Katalog: **afzal nom** plus **muqobillar**. Keyingi jild ochilishida Buran variantlarni avtomatik almashtiradi. |
| `feat. Dido` izohga tushib, ijrochi bo'lmaydi | **Fayl nomidan ID3**; ajratgichlar **Kalit so'zlar → Hamkorlik** (MB tahrirchi). |
| Teglar bo'sh yoki noto'g'ri, fayl nomlari toza | **Fayl nomidan ID3** (bitta fayl yoki ommaviy). |
| Fayl nomlari chalkash, teglar to'g'ri | **ID3 dan fayl nomi**. Sxema: `Ijrochi - Nom`. |
| Ikki fayl bir xil nom olardi | **Fayllarni solishtirish** (hajm, davomiylik, bitreyt, …) va saqlash yoki o'chirish. |
| ID3 izohlari pleyer axlatiga to'la | Ommaviy **Izohlar** — bir xil matn yoki tozalash. |
| Janr faqat ba'zi fayllarda | Ommaviy **Janrlar**: × faqat uchragan joyda o'chiradi; qo'shish **har bir** belgilangan faylga yozadi. |
| Albomda `CD1`/`CD2` ichki jildlarda | **Ichki jildlarni qo'shish**, keyin albom jildini bosing. |
| To'plam bo'ylab ijrochining barcha treklari | **Kutubxonani indekslash**, MB tahrirchida o'ng tugma → **Treklarni ko'rsatish**. |
| «192 kHz» fayl haqiqatan Hi-Resmi? | **Spektrogramma**: 16 kHz dan yuqorida energiya bormi. |
| Tasodifan qayta nomlangan yoki teglar buzilgan | Tahrir rejimida **Tiklash** — teglar **va** fayl nomi shu seans boshiga. |
| Axlat nomlarni boshqa taklif qilmaslik | Import oynasi yoki MB tahrirchi: **Bloklash** (faqat tanlangan turga: ijrochi, janr yoki kayfiyat). |

## Funksiyalar

### Sozlamalar

Yuqori o'ngda, **Sozlamalar** tugmasi.

| Sozlama | Nima qiladi | Nima uchun | Qanday |
|---|---|---|---|
| **Til** | Interfeys va ichki qo'llanmani almashtiradi. | Nemis, ingliz, rus, o'zbek (lotin) yoki tizim tili. | Ro'yxatni oching, tilni tanlang. Darhol qo'llaniladi. |
| **Shrift o'lchami** | Kichik / o'rtacha / katta. | Uzun teg ro'yxatlari va 4K monitorlar. | Ro'yxatni oching, o'lchamni tanlang. |
| **Shaffoflik** | Oyna shaffofligi (40 % dan). | Ish stolini ko'rsatish. | Slayder. |
| **Qo'llanma** | Ichki qo'llanmani ochadi. | Shu funksiya ko'rinishi, GitHub siz. | **Qo'llanma** tugmasi. |
| **Versiya** | O'rnatilgan versiya raqamini ko'rsatadi. | Paket yangimi, tekshirish. | Faqat ko'rsatish, sozlamalar menyusi pastda va start ekranida. |

### Kutubxona

Chap ustun — to'plamingiz jildlar daraxti.

#### Jild qo'shish

**Nima:** Jildni kutubxona ildizi qiladi.  
**Nima uchun:** Har safar tanlamasdan, fayl boshqaruvchidagidek tuzilmani ko'rasiz.  
**Qanday:**

1. **Jild qo'shish**.
2. Eng yuqori musiqa jildini tanlang.
3. Daraxtni oching va yoping.

#### O'chirish

**Nima:** Tanlangan ildizni ro'yxatdan oladi. Diskdagi musiqa fayllari qoladi.  
**Nima uchun:** Eski USB disk yoki qayta nomlangan yo'l boshqa chiqmasin.  
**Qanday:** Ildizni belgilang → **O'chirish**, yoki daraxtida o'ng tugma.

#### Yangilash

**Nima:** Daraxtni qayta o'qiydi.  
**Nima uchun:** Burandan tashqarida jild yaratilgan yoki qayta nomlangandan keyin.  
**Qanday:** **Yangilash**.

#### Ichki jildlarni qo'shish

**Nima:** Jild ochilganda ichki jildlardagi fayllar ham yuklanadi.  
**Nima uchun:** `CD1` / `CD2` li albom, yoki «Metal ostidagi hammasi birdan».  
**Qanday:** Belgini qo'ying, keyin daraxtidagi jildni bosing.

#### Kutubxonani indekslash

**Nima:** Nom, ijrochi, albom, yo'l va hokazoni mahalliy katalog indeksiga yozadi.  
**Nima uchun:** MB tahrirchida **Treklarni ko'rsatish** — ijrochi, janr yoki kayfiyatning barcha indekslangan treklari, jild yo'li bilan.  
**Qanday:**

1. Kamida bitta ildiz qo'shing.
2. **Kutubxonani indekslash**.
3. Keyin MB tahrirchida o'ng tugma → **Treklarni ko'rsatish**.

#### Ko'k nuqta

Nuqtali ichki jildda audio bor. Bosish uni ID3 tahrirchiga yuklaydi.

### ID3 tahrirchi

Asosiy ish maydoni. Teglar va fayl nomlari mos kelishi kerak bo'lgan joy.

#### Jadval

**Nima:** Tanlangan jilddagi barcha fayllarning ixcham ro'yxati: nom, ijrochilar, jild yo'li, albom, yil, davomiylik, bitreyt, diskretlash chastotasi, bit chuqurligi.  
**Nima uchun:** Umumiy ko'rinish, saralash, har bir faylni ochmasdan keraklisini topish.  
**Qanday:**

1. Chapda audio bor jildni tanlang.
2. Qatorni bosing — bu **fokusdagi trek** (tekshiruvchi, Tiklash, ikki marta bosish ijro etadi).
3. Birinchi ustundagi **belgi** — ommaviy amallar uchun **ko'p tanlov**. Fokus va belgilar mustaqil: A trekni ko'rib, B va C ni belgilashingiz mumkin.

**Ustunlar**

- Yuqoridagi **Ustunlar**: belgi qo'yish yoki olib tashlash ustunlarni ko'rsatadi yoki yashiradi.
- Sarlavhalarni tortish tartibni, chetini tortish kenglikni o'zgartiradi. Ajratgichni ikki marta bosish chapdagi ustunni mazmuniga moslaydi.
- Sarlavhani bosish qatorlarni saralaydi, yana bosish yo'nalishni almashtiradi. Xira strelkalar ustun saralanishini ko'rsatadi.
- **Fayl nomi**, **Izoh**, **Janr** va **Kayfiyat** sukut bo'yicha o'chirilgan, aks holda jadval juda keng bo'ladi.

**O'ng tugma → Jildni ochish**

Chapda shu fayl jildini tanlaydi va undagi barcha treklarni ko'rsatadi. MB tahrirchidagi **Treklarni ko'rsatish** dan keyin, topilmalar ko'p jildda bo'lsa, qulay.

**Filtrni tozalash**

Ro'yxat filtrlangan bo'lsa (masalan «shu ijrochi treklari»), tugma joriy kutubxona jildini qayta yuklaydi.

**Ikki marta bosish** fokusdagi trekni ijro etadi (pleyer moduli yuklangan bo'lsa).

Yuqori panelda **doim** (nafaqat tahrir rejimida): **Hammasi / Hech qaysi**, **Ijro etish** va **Pleylistga** (pleyer yuklangan bo'lishi kerak). Teg va nom ommaviy amallari faqat **Teglarni tahrirlash** dan keyin chiqadi.

#### Teglarni tahrirlash (tahrir rejimi)

**Nima:** O'ngda tekshiruvchini, yuqorida ommaviy panelni ochadi.  
**Nima uchun:** Rejimsiz jadval ko'rib chiqish uchun ixcham qoladi. Rejim bilan teglarni o'zgartirasiz, qayta nomlaysiz, tiklaysiz.  
**Qanday:**

1. **Teglarni tahrirlash** — tugma keyin **Tahrirlanmoqda** deb yoziladi.
2. Teglar uchun joy kerak bo'lsa, ro'yxat va tekshiruvchi orasidagi ajratgichni torting.
3. Rejimdan chiqish uchun yana bosing.

Tahrir **boshlanishida** Buran har bir trekning holatini (teglar **va** fayl nomi) eslab qoladi. **Chiqilganda** saqlangan holat yangi asos bo'ladi.

#### Tekshiruvchi — bitta trek

Tekshiruvchi doim **fokusdagi** trekka (bosilgan qator) tegishli, barcha belgilarga emas.

| Maydon / tugma | Nima qiladi | Nima uchun | Qanday |
|---|---|---|---|
| **Nom, albom, yil, izoh** | Qiymatni darhol faylga yozadi. | Xato yozuv, yil yo'q, izohlardagi axlat. | Maydonni bosing, matnni o'zgartiring, fokusni qoldiring — tayyor. |
| **Ijrochilar / janrlar / kayfiyatlar** | × bilan o'chirish va + / Enter bilan qo'shish ro'yxatlari. Takliflar katalogdan. | Bir nechta ijrochi (`feat.`), bir nechta janr, kayfiyat janrdan alohida. | Nom yozing, ro'yxatdan tanlang yoki yarating, + yoki Enter. |
| **Nom →** (ID3 → fayl nomi) | Teglardan `Ijrochi - Nom.mp3` yasaydi va faylni qayta nomlaydi. Afzal katalog nomlari ishlatiladi. Ikki ijrochi: `A feat. B`, ko'proq: `A feat. B, C & D`. | Fayl nomi va teglar mos kelishi kerak. | Trekni fokuslang, **Nom →**. Qator tanlangan qoladi. |
| **← Nom** (fayl nomi → ID3) | Fayl nomini andozalar bilan ajratadi (ijrochi, nom, albom, yil, Live/Remix …) va teglarni yozadi. | Nomlar toza, teglar bo'sh yoki noto'g'ri. | Trekni fokuslang, **← Nom**. |
| **Tiklash** | Teglarni **va** fayl nomini **shu tahrir seansi boshiga** qaytaradi. | Tasodifan qayta nomlangan yoki teglar buzilgan, boshqa trekka o'tib qaytgach ham. | Shu tahrir rejimida qoling, trekni yana fokuslang, **Tiklash**. |

Bloklanmagan noma'lum nomlar afzal katalog yozuvi bo'ladi. Bloklangan qiymatlar faylga tushadi, bazaga emas.

**Jild ochilganda** Buran fayllardagi muqobil yozuvlarni afzal katalog nomiga almashtiradi va saqlaydi.

#### Ommaviy amallar (belgilangan fayllar)

Faqat tahrir rejimida, faqat belgilangan qatorlar.

| Tugma | Nima qiladi | Nima uchun | Qanday |
|---|---|---|---|
| **Ijrochilar / janrlar / kayfiyatlar** | Ommaviy oynani ochadi. Ro'yxat — barcha tanlangan fayllarning **birlashmasi**. | «Pop» ni faqat uchragan joyda o'chirish; «Freestyle» ni **har bir** faylga qo'yish. | Belgilang → tugma → × yoki qo'shish → **Qo'llash**. |
| **Izohlar** | Bir xil matn barcha tanlangan fayllarga, yoki izohlarni tozalash. | ID3 izohlari pleyer axlatiga to'la. | Matn yozing va **Qo'llash**, yoki maydonni bo'sh qoldiring / **Tozalash**. |
| **ID3 dan fayl nomi** | **Nom →** kabi, lekin barcha belgilar uchun. | Jildni teg sxemasi bo'yicha qayta nomlash. | Belgilang → tugma. |
| **Fayl nomidan ID3** | **← Nom** kabi, lekin barcha belgilar uchun. | Toza fayl nomlaridan butun jild teglari. | Belgilang → tugma. |

**Ijrochilar / janrlar / kayfiyatlar ommaviy oynasi batafsil**

1. Fayllarni belgilang.
2. **Ijrochilar**, **Janrlar** yoki **Kayfiyatlar**.
3. × yozuvni **faqat uchragan joyda** o'chiradi. Boshqa fayl uni saqlaydi.
4. Nom qo'shish (yoki **hammasi** eslatmasi) uni **har bir** tanlangan faylga qo'yadi — hatto birlashma ro'yxatida bo'lsa ham. Shunday qilib janrni avval ro'yxatdan olib, keyin ongli ravishda hammaga qaytarishingiz mumkin.
5. **Tanlovdan yuklash** orada belgilarni o'zgartirgan bo'lsangiz, ro'yxatni qayta quradi.
6. **Qo'llash** teglarni yozadi.

#### ID3 dan fayl nomi — nom ziddiyati

Maqsad fayli allaqachon bo'lsa, **Fayllarni solishtirish** ochiladi: hajm, o'zgarish, davomiylik, bitreyt, diskretlash chastotasi, kanallar, bit chuqurligi, format.

| Tugma | Ta'sir |
|---|---|
| **Ikkalasini ham saqlash** | Qayta nomlashni bekor qilish. Ikkala fayl qoladi. |
| **Bu faylni saqlash** | Mavjud fayl o'chiriladi, joriysi qayta nomlanadi. |
| **Mavjudini saqlash** | Joriy fayl o'chiriladi, mavjudi qoladi. |

#### Fayl nomidan ID3 — tahlilchi qanday o'ylaydi

- Andozalar va kalit so'zlar nomni ijrochi, nom, albom, yil va versiya eslatmalariga ajratadi.
- Vergul va nuqta-vergul doim ajratadi.
- `feat.` / `ft.` / `featuring` — qavslar ichida ham, masalan `Eminem - Stan (feat. Dido).mp3` — **qo'shimcha ijrochilar**, izoh emas.
- `(Live)` yoki `[Remix]` izoh bo'lib qoladi.
- Ma'lum katalog nomlari va `D & F` kabi bir harfli guruhlar `D` va `F` ga kesilmaydi.
- Qo'shimcha ajratgichlarni MB tahrirchidagi **Kalit so'zlar** da yuritasiz.

### Ma'lumotlar bazasi uchun yangi qiymatlar

**Nima:** Jild yuklangandan keyingi oyna, teglar yoki fayl nomlarida katalogda yo'q va bloklanmagan nomlar bo'lsa.  
**Nima uchun:** «Gwen Stefani» yangi ijrochimi, ma'lum narsaning muqobil yozuvimi, yoki boshqa ko'rmoqchi bo'lmagan axlatmi — bir marta qaror qilasiz.  
**Qanday:**

1. Jildni oching. Oyna faqat noma'lum bo'lsa chiqadi.
2. Har bir yozuvda belgi: qabul qilish yoki o'tkazib yuborish.
3. **Afzal nom** — yangi katalog yozuvi (fayllarga tushishi kerak bo'lgan yozuv).
4. **Muqobil nom** — mavjud yozuvning xato yoki boshqa yozilishi. Ijrochida kerak bo'lsa afzal nomni ham shu yerda yaratishingiz mumkin.
5. **Bloklash** — boshqa taklif qilmaslik. `Happy` janri `Happy` kayfiyatini **bloklamaydi**.
6. Pastda: allaqachon bloklangan qiymatlarni **Blokdan chiqarish** — ular yana yuqorida import uchun chiqadi.
7. **Qo'llash** faqat belgilangan yozuvlarni yozadi. **O'tkazib yuborish** katalog o'zgarishisiz yopadi.

### Pleyer

Alohida modul. MP3 va FLAC ni **libVLC** (paketda) orqali ijro etadi. Tahrirchi ro'yxati ostida dock (ajratgich balandlikni o'zgartiradi) va qo'shimcha **Pleyer** tabi.

| Funksiya | Nima qiladi | Nima uchun | Qanday |
|---|---|---|---|
| **Ijro / Pauza / To'xtatish / Oldingi / Keyingi** | Transport. | Oddiy tinglash. | Ro'yxat ostidagi panel yoki Pleyer tabida. |
| **Pozitsiya / ovoz** | Qidirish va daraja. | Joyga sakrash. | Slayderlar. |
| **Spektrogramma** | Vaqt bo'yicha chastota, bir marta hisoblanadi (Spek kabi). O'qlar: Hz, vaqt, dB fayl Nyquist chastotasigacha. | «192 kHz» faylda 16 kHz dan yuqorida energiya bormi; shovqin va kesishlarni ko'rish. | Bosish yoki tortish faylda qidiradi. Ajratgich balandlikni o'zgartiradi. |
| **Jild navbati** | Pleyer tabidagi chap ro'yxat: kutubxona jildi fayllari. | Jildni tinglab chiqish. | Ikki marta bosish ijro etadi, **Pleylistga** nusxa oladi. |
| **Pleylistlar** | O'ng ro'yxat: jildlar bo'ylab o'tishi mumkin bo'lgan nomlangan ro'yxatlar. | Mikslar, «teglash kerak», sevimlilar. | **Yangi**, nom qo'ying, treklar qo'shing. Nom maydonida qayta nomlang. **O'chirish**. |
| **M3U import / eksport** | Mutlaq yo'llar. Yo'q fayllar belgilangan holda qoladi. | Boshqa pleyerlar bilan almashish. | Pleyer tabida import/eksport. |
| **Takrorlash** | O'chirilgan / hammasi / bittasi / bir marta. | Aylanish yoki shu trekidan keyin to'xtash. | Paneldagi takrorlash tugmasi. |
| **Pleyer va tablarni almashtirish** | Dock yuqorida yoki pastda. | Ro'yxat yoki spektrogramma uchun ko'proq joy. | Almashtirish tugmasi. |

**Pleylist**dan ijro jild o'zgarganda davom etadi. **Jild navbati** joriy trek yangi jildda bo'lmasa to'xtaydi.

Pleyer modulisiz ID3 tahrirchi to'liq ishlaydi.

### MB tahrirchi

Ikkinchi tab. Bu yerda **butun katalog**: yaratish, qayta nomlash, muqobil qilish, bloklash, blokdan chiqarish. Import oynasi qila oladigan hamma narsani shu yerda doimiy yuritasiz.

#### Qidiruv va yangilash

- Qidiruv maydoni ijrochilar, muqobil nomlar va kalit so'zlarni filtrlaydi.
- **Qidiruvni tozalash** filtrni tiklaydi.
- **Yangilash** katalogni qayta yuklaydi.

#### Ijrochilar

**Nima:** Afzal nom va ixtiyoriy fuqarolik ismi ro'yxati (`Eminem` uchun `Marshall Mathers`).  
**Nima uchun:** Barcha variantlar yig'iladigan bitta kanonik yozuv.  
**Qanday:**

- Pastda **afzal nom** yozing, ixtiyoriy fuqarolik ismi, **Qo'shish**.
- **Muqobil nom:** yozilgan nomni mavjud ijrochiga bog'lash. Maydonga yozish filtrlaydi (masalan `E` → Eminem).
- **Bloklash:** yozilgan nomni yashirish. Katalogda bo'lsa, o'chiriladi. Afzal nomni bloklash uning muqobillarini ham bloklaydi.
- O'ng tugma: **O'chirish**, **Bloklash**, **Tanlovni bekor qilish**, **Treklarni ko'rsatish** (ID3 tahrirchidagi indekslangan treklar).

#### Muqobil nomlar

Barcha muqobillar yoki faqat tanlangan ijrochiniki. Qo'shish uchun ijrochi tanlangan bo'lishi kerak. O'chirish va bloklash o'ng tugma yoki tugmalar orqali.

#### A'zolik

Faqat ijrochi tanlanganda. Shu guruh a'zolari yoki ijrochi tegishli guruhlar (`Eminem` `D12` da). Nomlar allaqachon ijrochi sifatida mavjud bo'lishi kerak. Guruh o'zining a'zosi bo'la olmaydi.

#### Janrlar va kayfiyatlar

Bir xil boshqaruv, alohida ro'yxatlar. Kayfiyatlar — his-tuyg'u (`Happy`, `Dark`), janr o'rnini bosmaydi.

- Belgi: janr yoki kayfiyatni **tanlangan ijrochiga** biriktirish (shu act repertuari).
- O'ng tugma: O'chirish, Bloklash, Tanlovni bekor qilish, **Tanlov bo'yicha filtrlash** (ijrochilar ro'yxati), filtrni olib tashlash, **Treklarni ko'rsatish**.
- Muqobil yozuvlar: tanlangan yozuv variantlari (`Danc` → `Dance`).
- Pastda: afzal nom yaratish, muqobil bog'lash, bloklash.

#### Kalit so'zlar

Fayl nomlari va teglar uchun ajratgichlar va tanish so'zlar.

| Tur | Maqsad | Misollar |
|---|---|---|
| **Hamkorlik** | Ijrochilarni ajratadi. | `feat`, `ft`, `vs`, `with`, `and`, `&`, `+`, `/` |
| **Versiya** | Izoh bo'lib qoladigan eslatmalarni topadi. | `Live`, `Remix`, `Remaster` |

Vergul va nuqta-vergul doim ajratgich. `D & F` ijrochi nomi kesilmasin desangiz, `&` ni shu yerdan olib tashlashingiz mumkin.

#### Bloklangan qiymatlar

Pastki panel, ijrochi, janr va kayfiyat alohida. **Blokdan chiqarish** nomni yana taklif qilishga ruxsat beradi. Katalog yozuvi o'zi qaytmaydi — uni keyin ongli ravishda qayta qo'shasiz.

Keyinroq shu yozuvni katalogga qo'shish blokni faqat shu yozuv uchun olib tashlaydi.

### Katalog modeli

Uch alohida tur: **ijrochi**, **janr**, **kayfiyat**.

| Tur | Ma'nosi |
|---|---|
| **Afzal nom** | Fayllarga tushishi kerak bo'lgan yozuv. |
| **Muqobil** | Shu yozuvning boshqa yozilishi. Jild yuklanganda avtomatik almashtiriladi. |
| **Bloklangan** | Import oynasida chiqmaydi va katalogga jim qo'shilmaydi. |

Bloklash faqat tanlangan turga tegishli. Bir xil qator janr sifatida bloklanib, kayfiyat sifatida ruxsat etilishi mumkin.

## Buran ma'lumotni qayerda saqlaydi

Musiqa fayllariga Buran faqat teg yozganda yoki qayta nomlaganda tegadi. Katalog va sozlamalar alohida:

| Tizim | Joy |
|---|---|
| Linux | `~/.local/share/Buran` |
| Windows | `%LOCALAPPDATA%\Buran` |

SQLite fayli tarixan `CerberusMusicManager.db` deb ataladi. Ilovani o'chirish bu jildni teginmaydi, shunda katalog saqlanib qoladi.

## GitHub relizlari

`Directory.Build.props` dagi yangi versiya raqami bilan har bir reliz `master` ga push dan keyin avtomatik yaratadi:

- Linux `.rpm` (va `.deb`)
- Windows `setup.exe` va portativ `.zip`

Versiya oshirilmagan oddiy kod pushlari faqat kompilyatsiya qiladi (yashil **Build** belgisi). O'rnatuvchilar versiya yangi bo'lsa, `packaging/` yoki paket workflow o'zgarsa, yoki **Actions → Package → Run workflow** ni qo'lda ishga tushirsangiz paydo bo'ladi.

## O'zingiz yiging

Talab: [.NET 10 SDK](https://dotnet.microsoft.com/download) (`global.json` ga qarang).

```bash
dotnet build BuranMusicLibraryManager.sln -c Release
```

JetBrains Rider da solution ni oching va odatiy ishga tushiring / kompilyatsiya qiling.

Fedora/Bazzite mashinasida Linux paketlar (VLC kutubxonalari qo'shiladi):

```bash
bash packaging/pack.sh linux
```

Natija `dist/` ostida (gitignore), masalan `buran-<Versiya>-4.x86_64.rpm`.

Rider da shu qadamlar **Run → Run…** konfiguratsiyalari:

| Konfiguratsiya | Amal |
|---|---|
| **Pack Linux** | `packaging/pack.sh linux` |
| **Install Linux RPM** | `dist/buran-<Versiya>-*.rpm` ni rpm-ostree orqali qo'yadi; mavjud buran almashtiriladi. Parol Rider terminalida (qo'shimcha oyna yo'q). |
| **Pack + Install Linux RPM** | avval paket, keyin o'rnatish (almashtirish bilan) |

`packaging/rider-install-linux.sh` va `packaging/rider-pack-and-install-linux.sh` — ikki yangi konfiguratsiya skriptlari.

Windows o'rnatuvchisi GitHub Actions da (Inno Setup) yig'iladi. Windows da mahalliy, `iscc` PATH da bo'lsa:

```bash
bash packaging/pack.sh windows
```

---

Buran teglarni darhol yozadi, katalogingizni eslab qoladi va paketdagi libVLC orqali ijro etadi. Biror narsa noaniq qolsa: ilovada **Sozlamalar → Qo'llanma**.
