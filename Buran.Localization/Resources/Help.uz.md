# Buran — Qo'llanma

Buran musiqa kutubxonasini boshqaradi: MP3 va FLAC fayllaridagi ID3 teglarni tahrirlaydi, ijrochilar, janrlar va kayfiyatlar katalogini yuritadi va yonida ijro etadi. Teg o'zgarishlari darhol faylga yoziladi.

O'z katalogingiz — haqiqat manbai, MusicBrainz yoki iTunes emas. Afzal yozuv, muqobillar va blok ro'yxati keyingi jild ochilishida o'zi qo'llaniladi.

## Odatiy muammolar

- Bir ijrochi besh xil yozuvda (`Eminem`, `EMINEM`, `M&M`): katalogda afzal nom va muqobillar. Jild ochilganda variantlar avtomatik almashtiriladi.
- `feat. Dido` izohga tushib, ijrochi bo'lmaydi: fayl nomidan ID3; ajratgichlar Kalit so'zlar → Hamkorlik (MB tahrirchi).
- Teglar bo'sh yoki noto'g'ri, fayl nomlari toza: fayl nomidan ID3 (bitta fayl yoki ommaviy).
- Fayl nomlari chalkash, teglar to'g'ri: ID3 dan fayl nomi. Sxema `Ijrochi - Nom`.
- Ikki fayl bir xil nom olardi: Fayllarni solishtirish (hajm, davomiylik, bitreyt, …) va saqlash yoki o'chirish.
- ID3 izohlari pleyer axlatiga to'la: ommaviy Izohlar — bir xil matn yoki tozalash.
- Janr faqat ba'zi fayllarda: ommaviy Janrlar. × faqat uchragan joyda o'chiradi; qo'shish har bir belgilangan faylga yozadi.
- Albomda CD1/CD2 ichki jildlarda: Ichki jildlarni qo'shish, keyin albom jildini bosing.
- To'plam bo'ylab ijrochining barcha treklari: Kutubxonani indekslash, MB tahrirchida o'ng tugma → Treklarni ko'rsatish.
- «192 kHz» fayl haqiqatan Hi-Resmi? Spektrogramma: 16 kHz dan yuqorida energiya bormi.
- Tasodifan qayta nomlangan yoki teglar buzilgan: tahrir rejimida Tiklash — teglar va fayl nomi shu seans boshiga.
- Axlat nomlarni boshqa taklif qilmaslik: import oynasi yoki MB tahrirchi → Bloklash (faqat tanlangan turga).

## Sozlamalar

Yuqori o'ngdagi «Sozlamalar» menyuni ochadi.

- Til: nemis, ingliz, rus, o'zbek (lotin) yoki tizim tili. Interfeys va ushbu qo'llanma tanlovga amal qiladi.
- Shrift o'lchami: kichik, o'rtacha yoki katta.
- Shaffoflik: asosiy oyna shaffofligi (40 % dan).
- Qo'llanma: shu qo'llanma.
- Versiya: o'rnatilgan versiya raqami (start ekranida ham).

## Kutubxona

Chap ustun — jildlar daraxti.

- Jild qo'shish: ildiz jildni kutubxonaga olish. Bir nechta ildiz mumkin; ro'yxat saqlanadi.
- O'chirish: tanlangan ildizni ro'yxatdan olish. Diskdagi fayllar qoladi.
- Yangilash: daraxtni qayta o'qish, masalan Burandan tashqarida jild o'zgarganda.
- Ichki jildlarni qo'shish: jild ochilganda ichki jildlardagi fayllar ham yuklanadi (CD1/CD2 albom).
- Kutubxonani indekslash: nom, ijrochi, albom, yo'lni mahalliy indeksga yozish. Keyin MB tahrirchida o'ng tugma → Treklarni ko'rsatish.
- Ko'k nuqtali ichki jildda audio bor. Bosish uni ID3 tahrirchiga yuklaydi.
- Qo'llab-quvvatlanadigan fayllar: MP3 va FLAC.

## ID3 tahrirchi

Ish maydonining birinchi tabi. Ixcham jadval (nom, ijrochilar, jild yo'li, albom, yil, davomiylik, bitreyt, diskretlash chastotasi, bit chuqurligi). Ustunlar «Ustunlar» orqali. Fayl nomi, izoh, janr va kayfiyat sukut bo'yicha o'chirilgan. Sarlavhalar orasidagi ajratgichni ikki marta bosish chapdagi ustunni mazmuniga moslaydi, sarlavha matnidan tor emas. Sarlavhaning o'zini bosish qatorlarni saralaydi; yonidagi xira strelkalar shuni bildiradi.

Bosilgan qator — fokusdagi trek (tekshiruvchi, Tiklash, ikki marta bosish ijro etadi). Birinchi ustundagi belgi — ko'p tanlov. Fokus va belgilar mustaqil.

O'ng tugma → Jildni ochish chapda shu fayl jildini tanlaydi. Treklarni ko'rsatishdan keyin, topilmalar ko'p jildda bo'lsa, qulay.

«Tozalash» (Treklarni ko'rsatishdan keyin) tanlangan kutubxona jildini qayta yuklaydi.

Pleyer yuklangan bo'lsa, transport va spektrogramma ro'yxat ostida (ajratgich balandlikni o'zgartiradi). Pleyersiz tahrirchi to'liq ishlaydi.

Yuqori panelda doim: Hammasi / Hech qaysi, Ijro etish va Pleylistga (pleyer yuklangan bo'lishi kerak). Teg va nom ommaviy amallari faqat tahrir rejimida.

### Teglarni tahrirlash

O'ngda tekshiruvchini, yuqorida ommaviy panelni ochadi. Rejim boshida Buran har bir trekning teglari va fayl nomini eslab qoladi; chiqilganda shu holat yangi asos bo'ladi.

### Tekshiruvchi — bitta trek

Fokusdagi trekka tegishli, barcha belgilarga emas.

- Nom, albom, yil, izoh: darhol faylga.
- Ijrochilar, janrlar, kayfiyatlar: × va + / Enter ro'yxatlari. Takliflar katalogdan.
- Nom → teglardan `Ijrochi - Nom` yasaydi (afzal katalog nomlari). Ikki ijrochi: `A feat. B`, ko'proq: `A feat. B, C & D`.
- ← Nom fayl nomini ajratadi va teglarni yozadi.
- Tiklash teglar va fayl nomini shu tahrir seansi boshiga qaytaradi.

Bloklanmagan noma'lum nomlar afzal katalog yozuvi bo'ladi. Bloklangan qiymatlar faylga tushadi, bazaga emas.

Jild ochilganda fayllardagi muqobil yozuvlar afzal katalog nomiga almashtiriladi va saqlanadi.

### Ommaviy amallar (belgilangan fayllar)

Faqat tahrir rejimida, faqat belgilangan qatorlar: ijrochilar, janrlar, kayfiyatlar, izohlar, ID3 dan fayl nomi, fayl nomidan ID3.

Ommaviy oynadagi ro'yxat — barcha tanlangan fayllarning birlashmasi. × faqat uchragan joyda o'chiradi. Qo'shish yoki «hammasi» uni har bir tanlangan faylga yozadi.

Izohlar: bitta maydon va fayl bo'yicha ko'rib chiqish. Bo'sh maydon izohni o'chiradi.

### ID3 dan fayl nomi va ziddiyatlar

Maqsad nomi allaqachon bo'lsa, «Fayllarni solishtirish» ochiladi (hajm, o'zgarish, davomiylik, bitreyt, chastota, kanallar, bit chuqurligi, format).

- Ikkalasini ham saqlash: qayta nomlashni bekor qilish.
- Bu faylni saqlash: mavjudini o'chirish, buni qayta nomlash.
- Mavjudini saqlash: bu faylni o'chirish.

### Fayl nomidan ID3

Andozalar va kalit so'zlar nomni ajratadi (ijrochi, nom, albom, yil, Live/Remix). Vergul va nuqta-vergul doim ajratadi.

«feat.» / «ft.» / «featuring» — qavslar ichida ham, masalan `Eminem - Stan (feat. Dido).mp3` — qo'shimcha ijrochilar, izoh emas. `(Live)` yoki `[Remix]` izoh bo'lib qoladi.

Ma'lum katalog nomlari va «D & F» kabi bir harfli guruhlar kesilmaydi. Qo'shimcha ajratgichlar MB tahrirchidagi Kalit so'zlarda.

## Ma'lumotlar bazasi uchun yangi qiymatlar

Jild yuklangandan keyin, teglar yoki fayl nomlarida katalogda yo'q va bloklanmagan nomlar bo'lsa.

- Belgi: qabul qilish yoki o'tkazib yuborish.
- Afzal nom: yangi katalog yozuvi.
- Muqobil nom: mavjud yozuvning yozilishi.
- Bloklash: boshqa taklif qilmaslik. «Happy» janri «Happy» kayfiyatini bloklamaydi.
- Pastda: bloklangan qiymatlarni blokdan chiqarish — ular yana yuqorida chiqadi.
- Qo'llash faqat belgilangan yozuvlarni yozadi. O'tkazib yuborish o'zgarishsiz yopadi.

## Pleyer

Alohida modul. MP3 va FLAC ni libVLC orqali ijro etadi. ID3 tahrirchi bilan transport va spektrogramma ro'yxat ostida; Pleyer tabi jild navbati va pleylistlarni saqlaydi.

- Ijro / Pauza / To'xtatish / Oldingi / Keyingi, pozitsiya, ovoz.
- Takrorlash: o'chirilgan / hammasi / bittasi / bir marta.
- Pleyer va tablarni almashtirish: yuqorida yoki pastda.
- Spektrogramma (bir marta hisoblanadi, Spek uslubida). O'qlar: Hz, vaqt, dB Nyquist chastotasigacha. Bosish yoki tortish qidiradi. 192 kHz faylda 16 kHz dan yuqorida energiya bormi — shu yerda ko'rinadi.
- Chap ro'yxat: kutubxona jildi fayllari. Ikki marta bosish ijro etadi, Pleylistga nusxa oladi.
- O'ng ro'yxat: jildlar bo'ylab nomlangan pleylistlar. Yangi, qayta nomlash, o'chirish.
- M3U import va eksport (mutlaq yo'llar). Yo'q fayllar belgilangan holda qoladi.
- ID3 tahrirchidan: tanlov uchun Ijro etish va Pleylistga.

Pleylistdan ijro jild o'zgarganda davom etadi. Jild navbati joriy trek yangi jildda bo'lmasa to'xtaydi.

## MB tahrirchi

Ikkinchi tab. To'liq katalog: yaratish, qayta nomlash, muqobil qilish, bloklash, blokdan chiqarish.

### Qidiruv va yangilash

Qidiruv ijrochilar, muqobil nomlar va kalit so'zlarni filtrlaydi. Qidiruvni tozalash filtrni tiklaydi. Yangilash katalogni qayta yuklaydi.

### Ijrochilar

Afzal nom va fuqarolik ismi ro'yxati. O'ng tugma: O'chirish, Bloklash, Tanlovni bekor qilish, Treklarni ko'rsatish (indekslangan treklar, jild yo'li bilan).

Pastda qo'shish: afzal nom (ixtiyoriy fuqarolik ismi), muqobilni mavjud ijrochiga bog'lash yoki Bloklash. Maydonga yozish filtrlaydi (masalan «E»).

Afzal nomni bloklash uning muqobillarini ham bloklaydi.

### Muqobil nomlar

Barcha muqobillar yoki faqat tanlangan ijrochiniki. Qo'shish uchun ijrochi tanlangan bo'lishi kerak.

### A'zolik

Faqat ijrochi tanlanganda. Shu guruh a'zolari yoki ijrochi tegishli guruhlar. Nomlar allaqachon ijrochi sifatida mavjud bo'lishi kerak. Guruh o'zining a'zosi bo'la olmaydi.

### Janrlar va kayfiyatlar

Bir xil boshqaruv, alohida ro'yxatlar. Kayfiyatlar — his-tuyg'u (`Happy`, `Dark`), janr o'rnini bosmaydi.

- Belgi: tanlangan ijrochiga biriktirish.
- O'ng tugma: O'chirish, Bloklash, Tanlovni bekor qilish, Tanlov bo'yicha filtrlash, filtrni olib tashlash, Treklarni ko'rsatish.
- Tanlangan yozuvning muqobil yozuvlari.
- Pastda: afzal nom yaratish, muqobil bog'lash, bloklash.

### Kalit so'zlar

- Hamkorlik: ijrochilarni ajratadi (feat, ft, vs, with, and va & + / kabi belgilar).
- Versiya: Live, Remix, Remaster ni topadi.

Vergul va nuqta-vergul doim ajratgich. «D & F» kesilmasin desangiz, & ni shu yerdan olib tashlang.

### Bloklangan qiymatlar

Pastki panel, ijrochi, janr va kayfiyat alohida. Blokdan chiqarish nomni yana taklif qilishga ruxsat beradi. Katalog yozuvi o'zi qaytmaydi.

Keyinroq shu yozuvni katalogga qo'shish blokni faqat shu yozuv uchun olib tashlaydi.

## Katalog modeli

Uch tur, har biri alohida: ijrochi, janr, kayfiyat.

- Afzal nom: fayllarga tushishi kerak bo'lgan yozuv.
- Muqobil: shu yozuvning boshqa yozilishi. Jild yuklanganda afzal nomga almashtiriladi.
- Bloklangan: import oynasida chiqmaydi va katalogga jim qo'shilmaydi.

Bloklash faqat tanlangan turga tegishli. Bir xil qator janr sifatida bloklanib, kayfiyat sifatida ruxsat etilishi mumkin.

MB tahrirchi import oynasi qila oladigan hamma narsani qila oladi: afzal nom yaratish, muqobil bog'lash, bloklash va blokdan chiqarish.
