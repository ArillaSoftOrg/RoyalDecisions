# SIĞINAK: SALTANAT GÜNLÜKLERİ
### v11 — TEMİZ SÜRÜM: Sadece Gerçek Kart Numaralarıyla Dallanma

---

## 0. KADRO — 21 KİŞİ (9 VASIFLI)
**Zeynep** (Doktor) · **Sabiha** (Erzakçı) · **Ömer** (Gözcü) · **Kemal** (Mühendis) · **Atilla** (Sığınak Görevlisi) · **Aziz** (Tarımcı) · **İsmet** (Telsizci) · **Mustafa** (Asker) · **Mete** (Asker)
**Diğer 12:** Emine Teyze · Rıza · Ali & Veli · Semra · Necati · Gül · Tarık · Sibel · Yusuf · Fatma · Cem

## 1. SİSTEM
🥫 Erzak · 🏠 Barınak · 🩺 Toplum Sağlığı · ☺ Toplum Morali *(0-10, başlangıç 5)* + 👑 Lider Sağlığı *(0-10, başlangıç 10, ayrı ölçü)*.

**Saltanat Döngüsü:** Herhangi bir ölçü 0'a düşerse lider değişir. Hikaye **bir sonraki karttan** devam eder (asla geriye/baştan başlanmaz), sıfırlanan madde 3'e resetlenir, 👑 yeni lider için 10 olur, diğer her şey korunur. **Önemli:** kartlardaki her "→K(sayı)" oku, SALTANAT SONU tetiklense de tetiklenmese de geçerlidir — lider ölse de ölmese de hikaye aynı sonraki karta gider, sadece kim liderlik ettiği değişir.

## 2. DALLANMA KURALI — BASİT VE NET
**Hiçbir ara-etiket, hiçbir "K1a/K1b" yok.** Her kartta A ve B, doğrudan **gerçek kart numaralarına** gider. Nötr olmayan (madde etkisi olan) neredeyse her kartta A ve B **farklı numaralara** gider — biri hikayenin bir sonraki kartına, diğeri bir kartı atlayıp ilerisine. Atlanan kart, o an sadece diğer yolu seçenlerin gördüğü bir kart olur. Örnek: K1'de A seçilirse K2'ye, B seçilirse K3'e gidilir — iki farklı kart, iki farklı devam. Ölüm kuralı ve gecikmeli zincirler önceki sürümlerle aynıdır (bkz. Bölüm 3).

## 3. TEKNİK NOTLAR
- **Ölüm kuralı (kesin):** Hikaye hiçbir zaman kendiliğinden, anlatı gereği lideri öldürmez. Lider yalnızca **oyuncunun seçimlerinin doğrudan sonucu** olarak ölür: (a) bir seçenek bilerek bir maddeyi sıfırlarsa (örn. K8'de tüm depoyu dağıtmak), ya da (b) "Lider Riski" kartlarında, önceki kararların birikimiyle 👑 zaten kritik düşükken (`<5`) yine de riskli seçenek seçilirse. Madde/👑 sağlıklıysa aynı seçenek asla ölümle sonuçlanmaz, sadece küçük bir bedeli olur. Şans, zar ya da "bazen ölürsün" gibi rastgelelik hiçbir yerde yoktur.
- **Değişken gecikmeli zincirler:** bir zincirin kararı ile sonucu arasındaki mesafe oyuncunun izlediği yola göre biraz değişebilir — bu kasıtlıdır, öngörülemezliği artırır.
- **Çok kartlı olaylar:** bazı olaylar 2-4 kart boyunca sürer, her adımında yeni bir karar vardır; bu kartlar kendi aralarında sıralı ilerler.
- **Ölüm kuralı:** asla şansa bağlı değil — madde zaten kritik düşükse (≤3) riskli seçim ölüme götürür, sağlıklıysa küçük bedelle geçilir.

---

## 4. KART KATALOĞU

### BÖLÜM I (K1-K25)

**K1 — İlk Gün**
Ömer kapıya koşar. Dışarıda kapıyı çalanlar var, der. Kapatalım mı, son şans mı verelim?
A) Kapat, kapı gürültüyle kapanır. `🏠+1 ☺-1`→K2
B) İçeri çek, son anda birkaç kişi daha girer. `☺+1 🥫-1 🏠-1`→K3

**K2 — Vicdan**
Atilla yanına gelir. Dışarıda kalanlar hâlâ akıllarda, der. Konuşalım mı, geçelim mi?
A) Geç, suskunluk ağırlaşır. `☺-1`→K4
B) Konuş, açık konuşma rahatlatır. `☺+1`→K5

**K3 — Kalabalık**
Sabiha elinde defterle gelir. Depo hızla azalıyor, der. Payları nasıl bölelim?
A) Kısıtlı pay ver. `🥫-1 ☺+1` *(k3_yolu=evet)*→K6
B) Sınırsız paylaş. `🥫-2 ☺+2`→K7

**K4 — Gerginlik**
Mustafa gelir, yüzü asık. İki grup birbirine giriyor, der. Otorite mi, uzlaşma mı?
A) Otorite kur. `🏠+1 ☺-1`→K7
B) Uzlaşma dene. `☺+1 🏠-1`→K8💀

**K5 — Karar Anı**
Sabiha çantasını hazırlar. Keşfe mi çıkalım, önce mi güçlenelim?
A) Keşfe çık. `☺0`→K9⚑
B) Önce güçlen. `🏠+1`→K6

**K6 — Yüzleşme**
Kemal kroki elinde gelir. Kalabalık tek bölmede tehlikeli, der. Ayıralım mı, bir arada mı tutalım?
A) Ayır, duvarlar örülür. `🏠-1`→K7
B) Bir arada tut, sıkışık ama birlik. `☺+1 🩺-1`→K13

**K7 — Bölme/Otorite Sonrası**
*(K3-B veya K6-A'dan gelinir)* Kemal ya da Mustafa devam eder — ya yeni bölme kararı ya da otorite sonrası gerginlik. Yeni bölme mi/kararlı mı kal, geçici çözüm mü/yumuşa mı?
A) Sağlam çöz. `🏠+1 🥫-1`→K10
B) Hafif geç. `☺-1`→K10

**K8 — YIKICI 💀🥫**
Rıza bağırır: "Hepsini yiyelim!" Sabiha itiraz eder: "Delilik bu!"
A) Kısıtlamaya devam et. `🥫-1 ☺-1`→K11
B) Dağıt, bir gecelik ziyafet. `🥫=0`→**SALTANAT SONU**→K11

**K9 — Nöbet Kararı ⚑**
Ömer yorgun gelir. Nöbetçi az, der. Geniş-seyrek mi, dar-sıkı mı?
A) Geniş-seyrek. `☺+1` *(nobet=gevşek)*→K11
B) Dar-sıkı. `🏠+1` *(nobet=sıkı)*→K12

**K10 — Gece Yarısı**
Zeynep fenerle gelir. Kaç gündür uyumadın, der. Dinlenecek misin, nöbete mi katılacaksın?
A) Dinlen. `👑+1`→K14
B) Nöbete katıl. `👑-1 ☺+1`→K11

**K11 — Sızıntı (Gevşek Nöbet Sonucu) ⚑**
Ömer koşarak gelir. Biri sızmış, der. Sessizce mi hallederiz, herkesi mi uyandırırız?
A) Sessizce hallet. `🩺-1 👑-1`→K14
B) Herkesi uyandır. `☺-1 🩺-1`→K15

**K12 — Yaralı (Sıkı Nöbet Sonucu) ⚑**
Ömer gelir, gururlu ama endişeli. Saldırı püskürtüldü ama biri yaralı, der. Zeynep'i çağıralım mı?
A) Çağır. `🩺+1`→K14
B) Bekler. `🩺-1`→K15

**K13 — Hastalık Belirtisi ⚑**
Zeynep telaşla gelir. Biri döküntülerle uyandı, der. Karantina mı, görmezden mi gelelim?
A) Karantina ilan et. `🩺+1 ☺-1` *(karantina=evet)*→K17
B) Görmezden gel. `☺+1 🩺-1` *(karantina=hayır)*→K18

**K14 — LİDER RİSKİ 💀👑**
Mete gelir. Dışarıda bir gölge var, der. Bizzat mı gidiyorsun, nöbetçi mi gönderiyorsun?
A) Bizzat git. *(👑<5 ise ANİ ÖLÜM; değilse 👑-3)*→K18
B) Nöbetçi gönder. `👑0 ☺-1`→K15

**K15 — Yaralı Yabancı**
Zeynep yaralının yanına koşar. İçeri mi alalım, gözlemleyelim mi?
A) Al. `🩺-1 ☺+1`→K19
B) Gözlemle. *(etki yok)*→K16

**K16 — Nötr**
Ali ve Veli kart oyunu oynuyor. Atilla gülümser. Otur musun, işine mi dönersin?
A) Otur. *(etki yok)*→K19
B) İşe dön. *(etki yok)*→K17

**K17 — Sonuç (Karantina Evet) ⚑**
Zeynep üzgün gelir. Hastalık durdu ama karantinadaki öldü, der. Tören mi, sessiz mi?
A) Tören düzenle. `☺+1`→K19
B) Sessizce göm. *(etki yok)*→K20

**K18 — Sonuç (Karantina Hayır) ⚑**
Zeynep telaşla gelir. Hastalık yayıldı, der. Şimdi karantina mı, doğaçlama tedavi mi?
A) Karantina uygula. `🩺-2 ☺-1`→K20
B) Doğaçlama tedavi et. *(değişken 🩺+1/-1)*→K19

**K19 — Vertak İpucu**
İsmet bir kimlik kartı bulur. Vertak yazıyor üstünde, der. Sorgulayalım mı, saklayalım mı?
A) Sorgula. `☺-1` *(vertak_ipucu+1)*→K21
B) Sakla. *(etki yok)*→K20

**K20 — Sürü Yaklaşıyor ⚑**
Ömer koşarak gelir. Sürü sesi geliyor, der. Barikat mı, saklanma mı?
A) Barikat kur. `🏠+1 🥫-1` *(savunma=barikat)*→K23
B) Saklan. `☺-1` *(savunma=saklanma)*→K21

**K21 — Nötr**
Aziz son kahveyi demler. İçer misin, başkasına mı vereyim?
A) İç. *(etki yok)*→K24
B) Ver. *(etki yok)*→K22

**K22 — Ara Kart**
Mustafa silahını hazırlar. Herkesi içeri mi toplayalım, nöbetçileri mi konumlandırayım?
A) Topla. `☺+1`→K24
B) Konumlandır. `👑-1 🏠+1`→K23

**K23 — Sonuç (Barikat) ⚑**
Kemal gelir. Barikat tuttu ama hasar gördü, der. Hemen mi onaralım, sonra mı?
A) Hemen onar. `🏠-1 🥫-1`→K26
B) Sonra onar. `🏠-2`→K24

**K24 — Sonuç (Saklanma) ⚑**
Ömer alçak sesle gelir. Sürü fark etmeden geçti, der. Az kalsın biri ses çıkarıyordu. Uyar mı, şansa mı bırak?
A) Uyar. `☺-1`→K25
B) Şansa bırak. *(değişken 🩺-2/etkisiz)*→K25

**K25 — Eşik**
İsmet kulaklığını çıkarır. Vertak'a sinyal mi, yalnız mı devam?
A) Sinyal gönder. →K26
B) Yalnız devam et. →K29

---

### BÖLÜM II (K26-K60)

**K26 — Nötr**
Semra tozlu bir gitar bulur. Tamir edeyim mi, bırakayım mı?
A) Tamir et. *(etki yok)*→K29
B) Bırak. *(etki yok)*→K27

**K27 — Çitteki Ses (1/2) ⚑🧟**
Ömer nöbette gelir. Çitin ötesinde bir şey konuşuyor gibi, der. Yaklaşalım mı, uzaktan mı izleyelim?
A) Yaklaş. `☺-1` *(cit_yaklastik=evet)*→K31
B) Uzaktan izle. `☺0` *(cit_yaklastik=hayır)*→K28

**K28 — Çitteki Ses (2/2) Sonuç**
*Yaklaşıldıysa:* "Yardım" diyor ama gözleri insan gözü değil. Ateş mi, dinle mi?
A) Ateş et. `☺-1`→K31
B) Dinle. `☺-2` *(zombi_konustu=evet)*→K31
*Uzaktan izlendiyse:* Ses kesildi. Devriyeyi artır mı, gerek yok mu?
A) Artır. `🏠+1`→K30
B) Gerek yok. →K31

**K29 — Zeynep'in Yorumu**
Zeynep gelir. Bu ses Vertak'ın notlarındaki bir şeye benziyor, der. Araştıralım mı, unutalım mı?
A) Araştır. `☺-1` *(pharma_arastirma=1)*→K32
B) Unut. *(etki yok)*→K30

**K30 — Nötr**
Emine Teyze eski bir anısını anlatır. Dinler misin, vaktin yok mu?
A) Dinle. *(etki yok)*→K33
B) Yok. *(etki yok)*→K33

**K31 — Sabiha'nın Seferi (1/2) ⚑**
Sabiha harita açar. 3 kişi mi göndereyim, 5 kişi mi?
A) 3 kişi. `🥫0` *(sefer_ekip=kucuk)*→K34
B) 5 kişi. `🥫0 🏠-1` *(sefer_ekip=buyuk)*→K35

**K32 — Sabiha'nın Seferi (2/2) Sonuç**
İsmet telsizden bağırır — sürüyle karşılaşmışlar. Geri mi, riske mi?
A) Geri çekil. `☺+1 🥫+1`→K35
B) Riske gir. *(kucuk: `🥫+1 🩺-1`; buyuk: `🥫+3 ☺-2`)*→K35

**K33 — Nötr**
Aziz topladığı sebzelerden bir yemek çıkarır. Ye mi, sakla mı?
A) Ye. *(etki yok)*→K36
B) Sakla. *(etki yok)*→K36

**K34 — Kemal'in Şüphesi ⚑** *(3 kart sonra sonuçlanır)*
Kemal duvara vurur. Temelde çatlak var, der. Şimdi mi, bekle mi?
A) Şimdi onar. `🏠0 🥫-1` *(catlak=onarildi)*→K37
B) Bekle. *(catlak=bekletildi)*→K36

**K35 — Nötr**
Ali ve Veli gitarla "konser" verir. Alkışla mı, izle mi?
A) Alkışla. *(etki yok)*→K38
B) İzle. *(etki yok)*→K38

**K36 — Salgının Kökeni**
İsmet eski bir rapor bulur — Vertak'ın Suş-7 deneyi kontrolden çıkmış. Herkese mi, kadroya mı?
A) Herkese açıkla. `☺-2`→K39
B) Kadroya söyle. →K39

**K37 — Kemal'in Şüphesi Sonucu ⚑**
*Onarıldıysa:* Duvar sağlam. Dinlen mi, kontrol mü et?
A) Dinlen. `☺+1`→K40
B) Kontrol et. `🏠+1 👑-1`→K40
*Bekletildiyse:* Çatlak büyüdü, su alıyor! Onar mı, boşalt mı?
A) Onar. `🏠-1 🥫-1`→K40
B) Boşalt. `🏠-2 ☺-1`→K39

**K38 — Nötr**
Rıza ve Tarık tartışıyor. Atilla araya girer. Sen mi, o mu?
A) Ben hallederim. *(etki yok)*→K41
B) Atilla'ya bırak. *(etki yok)*→K40

**K39 — YIKICI 💀☺**
Necati bağırır: "Lider bizi kandırıyor!" Açıkla mı, sustur mu?
A) Açıkla. `☺+1`→K42
B) Sustur. *(☺≤3 ise `☺=0`→SALTANAT SONU; ☺>3 ise `☺-1`)*→K43

**K40 — İsmet'in Sinyali (1/3) ⚑**
İsmet tuhaf bir sinyal yakalar. Cevap ver mi, tuzak mı?
A) Cevap ver. `☺+1` *(sinyal=cevaplandi)*→K43
B) Verme. *(sinyal=sessiz)*→K42

**K41 — İsmet'in Sinyali (2/3)**
*Cevaplandıysa:* Koordinat istiyorlar. Ver mi, verme mi?
A) Ver. *(konum_paylasildi=evet)*→K44
B) Verme. →K44
*Sessizse:* Sinyal sıklaşıyor. Kapat mı, açık mı bırak?
A) Kapat. `🥫-1`→K44
B) Açık bırak. →K44

**K42 — Nötr**
Gül'ün bebeği ilk kez güler. Gülümse mi, işine mi dön?
A) Gülümse. *(etki yok)*→K45
B) İşe dön. *(etki yok)*→K43

**K43 — İsmet'in Sinyali (3/3) Sonuç**
Ömer gelir — dışarıda bir araç var. Karşıla mı, kilitle mi?
A) Karşıla. →K46
B) Kilitle. →K44

**K44 — Vertak Konuşması** *(Konum paylaşıldıysa Vertak gelir, aksi halde belirsiz bir grup ya da kimse.)*
Konuş mu, mesafeli mi?
A) Konuş. `☺+1 veya ☺-2`→K47
B) Mesafeli kal. →K45

**K45 — Bebeğin Ateşi ⚑** *(2 kart sonra sonuçlanır)*
Zeynep endişeli gelir. Bebek ateşleniyor, der. Son ilacı mı, bekle mi?
A) Kullan. `🥫-1` *(ates_ilac=evet)*→K48
B) Bekle. *(ates_ilac=hayir)*→K46

**K46 — Nötr**
Sibel sessizce ayakkabıları onarıyor. Teşekkür et mi, sessiz mi kal?
A) Teşekkür et. *(etki yok)*→K49
B) Sessiz kal. *(etki yok)*→K47

**K47 — Bebeğin Ateşi Sonucu ⚑**
*İlaç kullanıldıysa:* Ateş düştü. →`☺+1`→K50
*Beklenildiyse:* Ateş yükseldi, şimdi vermek zorunda. →`🥫-1 🩺-1`→K48

**K48 — LİDER RİSKİ 💀👑**
Mustafa gelir. Birkaç enfekteli çok yaklaştı, der. Sen mi liderlik et, o mu alsın komutayı?
A) Ben ederim. *(👑<5 ise ANİ ÖLÜM; değilse `👑-2 🏠+1`)*→K51
B) Mustafa alsın. `👑0 🏠+1 ☺-1`→K49

**K49 — Nötr**
Cem ve Yusuf zar oynuyor. Katıl mı, gülümse mi?
A) Katıl. *(etki yok)*→K52
B) Gülümse. *(etki yok)*→K50

**K50 — YIKICI 💀🏠**
Kemal ciddi gelir. Kapının menteşeleri güvenilir değil, der. Tüm kaynağı mı, idare mi et?
A) Tüm kaynak ver. `🥫-2 🏠+2`→K53
B) İdare et. *(🏠≤3 ise `🏠=0`→SALTANAT SONU; 🏠>3 ise `🏠-1`)*→K51

**K51 — Vertak Teması**
İsmet gelir. Konuş mu, reddet mi?
A) Konuş. →K54
B) Reddet. →K52

**K52 — Halkın Öfkesi**
Tarık liderliğini sorguluyor. Sakin kal mı, sert mi?
A) Sakin kal. `☺+1`→K55
B) Sert karşılık ver. `☺-1`→K53

**K53 — Nötr**
Emine Teyze garip bir tarif dener. Tadına bak mı, reddet mi?
A) Tadına bak. *(etki yok)*→K56
B) Reddet. *(etki yok)*→K54

**K54 — Duman Kararı ⚑** *(6 kart sonra sonuçlanır)*
Sabiha uzakta bir duman görür. Araştır mı, girmeyelim mi?
A) Araştır. *(duman_arastir=evet)*→K57
B) Girmeyelim. *(duman_arastir=hayır)*→K55

**K55 — Nötr**
Ali'nin doğum günü. Kutla mı, sade mi?
A) Kutla. *(etki yok)*→K58
B) Sade geç. *(etki yok)*→K56

**K56 — Yabancı Grup (1/2)**
Ömer yaklaşan bir grup görür. Temas mı, izle mi?
A) Temas. *(yabanci_temas=evet)*→K59
B) İzle. *(yabanci_temas=hayır)*→K57

**K57 — Yabancı Grup (2/2) Sonuç**
*Temas:* Ticaret öneriyorlar. Ticaret mi, ret mi?
A) Ticaret. `🥫+1 ☺+1`→K60
B) Ret. `☺-1`→K58
*İzle:* Yakında konaklıyorlar. Nöbet artır mı, sessiz mi?
A) Artır. `🏠0`→K60
B) Sessiz kal. *(değişken 🏠-1/etkisiz)*→K58

**K58 — Nötr**
Fatma duvara gökkuşağı çiziyor. İzle mi, geç mi?
A) İzle. *(etki yok)*→K61
B) Geç. *(etki yok)*→K59

**K59 — Duman Kararı Sonucu (6 kart sonra) ⚑**
*Araştırıldıysa:* Küçük bir grup bulunur, katılmak istiyor. Al mı, ret mi?
A) Al. `🥫-1 ☺+1`→K62
B) Ret. `☺-1`→K60
*Girilmediyse:* İyi ki gitmediniz — orası tuzakmış. →`☺+1`→K62

**K60 — YIKICI 💀🩺**
Zeynep suyun kirli olabileceğini söylüyor. Test et mi, hemen iç mi?
A) Test et. `🥫-1`→K63
B) Hemen iç. *(🩺≤3 ise `🩺=0`→SALTANAT SONU; 🩺>3 ise `🩺0`)*→K61

---

### BÖLÜM III (K61-K100)

**K61 — Nötr**
Necati kumarda kaybediyor, herkes gülüyor. Gül mü, ciddi mi kal?
A) Gül. *(etki yok)*→K64
B) Ciddi kal. *(etki yok)*→K62

**K62 — LİDER RİSKİ 💀👑**
Mete, kritik malzeme için tehlikeli bir keşif gerektiğini söylüyor. Bizzat mı, gönder mi?
A) Bizzat git. *(👑<5 ise ANİ ÖLÜM; değilse `👑-2`)*→K65
B) Gönder. `👑0 🥫-1`→K63

**K63 — Lore: Faz 4**
İsmet eski bir Vertak dosyası buluyor *(pharma_arastirma+1)*. Paylaş mı, sakla mı?
A) Paylaş. `☺-1`→K66
B) Sakla. →K64

**K64 — Nötr**
Yusuf derede balık tutmaya çalışıyor. Yardım et mi, izle mi?
A) Yardım et. *(etki yok)*→K67
B) İzle. *(etki yok)*→K65

**K65 — Güneş Paneli Projesi (1/3)**
Kemal büyük bir proje öneriyor. Başla mı, ertele mi?
A) Başla. *(proje=baslatildi)*→K68
B) Ertele. *(proje=ertelendi)*→K66 ⚡

**K66 — Güneş Paneli (2/3)**
Malzeme eksik. Başka sığınaktan mı, kendi kaynağımızla mı?
A) Başka sığınaktan iste. *(ittifak_baslangic=evet)* `🥫-1`→K69
B) Kendi kaynağımız. `🏠-1`→K67

**K67 — Güneş Paneli (3/3) Sonuç**
*(proje=baslatildi ise)* Panel tamamlanır. →`🏠+2 🥫-1`→K70

**K68 — Nötr**
Sibel'in eskiden piyanist olduğu ortaya çıkıyor. İste mi, bırak mı?
A) İste. *(etki yok)*→K71
B) Bırak. *(etki yok)*→K69

**K69 — İttifak Teklifi ⚑** *(6 kart sonra sonuçlanır)*
Komşu bir sığınaktan ittifak teklifi geliyor. Kabul mü, ret mi?
A) Kabul. *(ittifak=evet)*→K72
B) Ret. *(ittifak=hayır)*→K70

**K70 — Nötr**
Tarık ve Rıza beklenmedik şekilde barışıyor. Kutla mı, fark etme mi?
A) Kutla. *(etki yok)*→K73
B) Fark etme. *(etki yok)*→K71

**K71 — YIKICI 💀🥫**
Sabiha, riskli bir toptan takas fırsatı buluyor. Güvenli mi, büyük riskli mi?
A) Güvenli takas. `🥫-1 🩺+1`→K74
B) Büyük riskli takas. *(🥫≤3 ise `🥫=0`→SALTANAT SONU; 🥫>3 ise `🥫+3`)*→K72

**K72 — Nötr**
Gül bebeğine isim koyuyor. Katıl mı, kısa tebrik mi?
A) Katıl. *(etki yok)*→K75
B) Kısa tebrik. *(etki yok)*→K73

**K73 — Konuşan Zombi**
Ömer çitte yine bir ses duyar: "Biz de... insandık." Dinle mi, uzaklaş mı?
A) Dinle. `☺-1` *(zombi_ikinci_temas=evet)*→K76
B) Uzaklaş. →K74

**K74 — Nötr**
Aziz'in tohum defteri kayboluyor. Yardım et mi, boşver mi?
A) Yardım et. *(etki yok)*→K77
B) Boşver. *(etki yok)*→K75

**K75 — İttifak Sonucu (6 kart sonra) ⚑**
*Kabul edildiyse:* İttifak sizi sömürmek istiyormuş. Karşı çık mı, boyun eğ mi?
A) Karşı çık. `☺-1`→K78
B) Boyun eğ. `🥫-2 ☺-1`→K76
*Reddedildiyse:* İyi ki reddettik. →`☺+1`→K77

**K76 — LİDER RİSKİ 💀👑**
Ömer bir suikast girişimi fark ediyor. Soruştur mu, görmezden mi?
A) Soruştur, şüpheliyle yüzleş. *(👑<5 ise ANİ ÖLÜM; değilse `👑-1 ☺-1`)*→K79
B) Görmezden gel. `👑0`→K77

**K77 — Nötr**
Ali ilk kez nöbete katılmak istiyor. İzin ver mi, erken mi bul?
A) İzin ver. *(etki yok)*→K80
B) Erken bul. *(etki yok)*→K78

**K78 — Kış Hazırlığı (1/2)**
Kemal, ısınma sorunu için iki çözüm sunuyor. Odun mu, elektrik mi?
A) Odun. *(kis_hazirlik=odun)* `🥫-1`→K81
B) Elektrik. *(kis_hazirlik=elektrik)* `🏠-1`→K79

**K79 — Kış Hazırlığı (2/2) Sonuç**
Kışın ilk haftası geçiyor. →`🏠 ve 🩺 üzerinde küçük swing`→K80

**K80 — Nötr**
Semra'nın konseri artık gelenek oldu. Katıl mı, kaçır mı?
A) Katıl. *(etki yok)*→K84
B) Kaçır. *(etki yok)*→K81

**K81 — Lore Doruğu** *(pharma_arastirma≥2 ise özel metin)*
İsmet, Vertak'ın asıl planını çözüyor: sığınakları toplamak. Yay mı, sessiz mi?
A) Yay. `☺-1`→K83
B) Sessiz kal. →K82

**K82 — YIKICI 💀☺**
"Vertak'a katılalım" tartışması büyüyor. İzin ver mi, zorla tut mu?
A) İzin ver. `☺+1`→K84
B) Zorla tut. *(☺≤3 ise `☺=0`→SALTANAT SONU; ☺>3 ise `☺-1`)*→K83

**K83 — Nötr**
Emine Teyze'nin son güzel günü — doğal seyrinde. Otur mu, yalnız mı bırak?
A) Otur. *(etki yok)*→K87
B) Yalnız bırak. *(etki yok)*→K84
*(Not: nüfus 20'ye düşer.)*

**K84 — Zeynep'in Tükenmişliği ⚑** *(5 kart sonra sonuçlanır)*
Zeynep bitkin görünüyor. Dinlenmesini emret mi, kendi bilsin mi?
A) Emret. *(zeynep_zorla_dinlendirildi=evet)*→K86
B) Kendi bilsin. →K85

**K85 — Nötr**
Cem ve Yusuf yeni bir oyun icat ediyor. Katıl mı, izle mi?
A) Katıl. *(etki yok)*→K87
B) İzle. *(etki yok)*→K86

**K86 — Mülteci Grubu (1/2)**
Sınırdan bir grup mülteci geliyor. Kabul mü, ret mi?
A) Kabul. *(multeci=kabul)*→K89
B) Ret. *(multeci=ret)*→K87

**K87 — Mülteci Grubu (2/2) Sonuç**
*Kabul:* Biri hasta. Karantina mı, risk mi?
A) Karantina. `🩺+1 ☺-1`→K90
B) Risk al. `🩺-1`→K88
*Ret:* Grup çevrede kalmış. Dağıt mı, görmezden mi?
A) Dağıt. `☺-1`→K90
B) Görmezden gel. *(değişken 🏠-1/etkisiz)*→K88

**K88 — Zeynep'in Tükenmişliği Sonucu (5 kart sonra) ⚑**
*Zorla dinlendirildiyse:* Zeynep toparlanmış döner. →`☺+1`→K90
*Kendi bilsin dendiyse:* Zeynep hastalanır. →`🩺-2`→K89

**K89 — Nötr**
Sığınağın yıl dönümü kutlanıyor. Kutla mı, sade mi?
A) Kutla. *(etki yok)*→K91
B) Sade geç. *(etki yok)*→K90

**K90 — LİDER RİSKİ 💀👑**
Mustafa, büyük bir sürü saldırısı geldiğini haber veriyor. Cepheye çık mı, ona mı bırak?
A) Cepheye çık. *(👑<5 ise ANİ ÖLÜM; değilse `👑-3 🏠+2`)*→K93
B) Mustafa'ya bırak. `👑0 🏠+1 ☺-1`→K91

**K91 — Nötr**
Ali büyümüş, ilk vasıflı görevini istiyor. Şans ver mi, bekle mi?
A) Şans ver. *(etki yok)*→K94
B) Bekle. *(etki yok)*→K92

**K92 — YIKICI 💀🏠**
Kemal, sığınağın taşınması gerekebileceğini söylüyor. Taşın mı, kal mı?
A) Taşın. `🥫-2 🏠+1`→K94
B) Kal. *(🏠≤3 ise `🏠=0`→SALTANAT SONU; 🏠>3 ise `🏠-1`)*→K93

**K93 — Vertak Final İpucu** *(pharma_arastirma≥3 ise özel yol)*
İsmet, Vertak'ın içeriden bölündüğünü öğreniyor. Temas mı, güvenme mi?
A) Temas. `☺+1`→K95
B) Güvenme. →K94

**K94 — Nötr**
İsmet eski bir kaset buluyor, hep beraber dinliyorlar. Dinle mi, kaçır mı?
A) Dinle. *(etki yok)*→K97
B) Kaçır. *(etki yok)*→K95

**K95 — Ateşkes ya da Son Saldırı (1/2)**
Ömer, konuşan zombilerle resmi bir temas fırsatı doğduğunu bildiriyor. Ateşkes mi, saldır mı?
A) Ateşkes dene. *(ateskes=evet)*→K97
B) Saldır. *(ateskes=hayır)*→K96

**K96 — Ateşkes Sonucu (2/2)**
*Ateşkes:* uzun vadeli barış kurulur. →`☺+2 🩺+1`→K100
*Saldırı:* çatışma büyür. →`☺-2 🩺-1`→K97

**K97 — Nötr**
Sessiz bir akşam, herkes hayatta kalmanın farkında. Yansıt mı, uyu mu?
A) Yansıt. *(etki yok)*→K100
B) Uyu. *(etki yok)*→K98

**K98 — Nötr**
Gül'ün çocuğu ilk adımlarını atıyor. Kutla mı, meşgul mü?
A) Kutla. *(etki yok)*→K102
B) Meşgul ol. *(etki yok)*→K99

**K99 — Nötr**
Fatma yeni çocuklara resim dersi veriyor. Katıl mı, izle mi?
A) Katıl. *(etki yok)*→K101
B) İzle. *(etki yok)*→K100

**K100 — Dönüm Noktası (Sezon 1 Kapanışı)**
Sığınağın kaderi o ana kadarki tüm bayrakların toplamına bağlı. Bu bir final değil — hikaye devam ediyor.

---

### BÖLÜM IV (K101-K150)

**K101 — Nötr**
Yeni sezon sakin bir sabahla açılıyor. Necati eski radyoyu tamir ediyor. Yardım et mi, izle mi?
A) Yardım et. *(etki yok)*→K104
B) İzle. *(etki yok)*→K102

**K102 — Vertak Baskısı ⚑** *(5 kart sonra sonuçlanır)*
İsmet: Vertak sinyalleri sıklaştı. Karart mı, açık mı bırak?
A) Karart. *(vertak_karartma=evet)*→K104
B) Açık bırak. *(vertak_karartma=hayır)*→K103

**K103 — Nötr**
Fatma duvara yeni resimler yapıyor. Katkı ver mi, izle mi?
A) Katkı ver. *(etki yok)*→K106
B) İzle. *(etki yok)*→K104

**K104 — Meydan Okuma (1/2)**
Tarık liderliğini açıkça sorguluyor: "Oy yapalım." İzin ver mi, bastır mı?
A) İzin ver. *(meydan_okuma=evet)*→K107
B) Bastır. *(gizli_gerginlik=evet)*→K105

**K105 — Meydan Okuma (2/2) Sonuç**
*İzin verildiyse:* Açık tartışma. Açık konuş mu, sessiz mi?
A) Açık konuş. `☺+2`→K108
B) Sessiz kal. `☺+1`→K106
*Bastırıldıysa:* Tarık gizliden destek topluyor. Ömer'e izlet mi, görmezden mi?
A) İzlet. `☺-1`→K108
B) Görmezden gel. *(ayaklanma_riski=evet)*→K106

**K106 — Vertak Baskısı Sonucu (5 kart sonra) ⚑**
*Karartıldıysa:* Sinyal kaybolur. →`☺+1`→K108
*Açık bırakıldıysa:* Vertak konumu bulur *(vertak_yolda=evet)*→K107

**K107 — Nötr**
Sibel'in piyano konserleri artık düzenli. Dinle mi, işe mi dön?
A) Dinle. *(etki yok)*→K110
B) İşe dön. *(etki yok)*→K108

**K108 — Nötr**
Ali artık genç bir yetişkin, "çırak nöbetçi" oldu. Gurur duy mu, sıradan mı davran?
A) Gurur duy. *(etki yok)*→K111
B) Sıradan davran. *(etki yok)*→K109

**K109 — Konuşan Zombi Escalation**
Ömer, birinin düzenli olarak çite yaklaşıp konuşmaya çalıştığını bildiriyor. İsim ver mi, mesafeli mi?
A) İsim ver. *(zombi_isimlendirildi=evet)*→K112
B) Mesafeli kal. →K110

**K110 — Vertak Keşif Ekibi (1/2)** *(vertak_yolda=evet ise)*
Bir araç yakında duruyor, kapıyı çalıyorlar. Aç mı, silahlan mı?
A) Aç. →K112
B) Silahlan. →K111
*(değilse)* Sabiha yeni bir bölge öneriyor. Git mi, kal mı?
A) Git. →K112
B) Kal. →K111

**K111 — Vertak Keşif Ekibi (2/2) Sonuç**
*Vertak:* Temsilci ayrılır ama "gözlemleneceksiniz" der. *(vertak_gozlem=evet)*→`☺-1`→K115
*Diğer:* Eski bir depo bulunur, kilitli — açılır, orta düzey erzak. →`🥫+2`→K112

**K112 — Nötr**
Aziz yeni bir hasat tarifi dener. Tadına bak mı, mütevazı mı kal?
A) Tadına bak. *(etki yok)*→K116
B) Mütevazı kal. *(etki yok)*→K113

**K113 — YIKICI 💀🩺**
Bir gıda zehirlenmesi vakası çıkıyor. Test et mi, görmezden mi?
A) Test et. `🥫-1 🩺+1`→K116
B) Görmezden gel. *(🩺≤3 ise `🩺=0`→SALTANAT SONU; 🩺>3 ise kendiliğinden atlatılır)*→K114

**K114 — Nötr**
Cem ve Yusuf'un oyunu artık gelenek. Oyna mı, izle mi?
A) Oyna. *(etki yok)*→K117
B) İzle. *(etki yok)*→K115

**K115 — Yeniden Yapılanma (1/2)**
Kemal büyük bir onarım projesi öneriyor. Tam mı, minimal mi?
A) Tam proje. *(onarim=tam)*→K118
B) Minimal. *(onarim=minimal, onarim_gecici=evet)*→K116

**K116 — Yeniden Yapılanma (2/2) Sonuç**
*(Tam: `🏠+3 👑-1`) (Minimal: `🏠+1`, ileride tekrar sorun çıkabilir)* →K118

**K117 — Nötr**
Küçük bir pazar kuruluyor, millet eşya takas ediyor. Katıl mı, gözlemle mi?
A) Katıl. *(etki yok)*→K120
B) Gözlemle. *(etki yok)*→K118

**K118 — LİDER RİSKİ 💀👑**
*(ayaklanma_riski=evet ise)* Gizli gerginlik patlıyor. Yüzleş mi, kaç mı?
A) Yüzleş. *(👑<5 ise ANİ ÖLÜM; değilse `👑-3 ☺+2`)*→K120
B) Kaç/saklan. `👑0 ☺-2`→K119

**K119 — Nötr**
Olaylardan sonra sakin bir akşam. Paylaş mı, yalnız mı kal?
A) Paylaş. *(etki yok)*→K122
B) Yalnız kal. *(etki yok)*→K120

**K120 — Yeni Yüz**
Yaralı bir kadın kapıya geliyor, eski bir Vertak çalışanı. İçeri al mı, uzak tut mu?
A) İçeri al. *(eski_vertak_calisan=evet)*→K124
B) Uzak tut. →K121

**K121 — Sorgu Sonucu**
*Alındıysa:* İsmet sorguluyor. Güven mi, şüphe mi? *(güvenilirse pharma_arastirma+2, icerden_bilgi=evet)* `☺-1`→K124
*Uzak tutulduysa:* Kadın gider, bir not bırakır — kısmi bilgi. →K122

**K122 — Nötr**
Ali "tam nöbetçi"liğe terfi ediyor. Gurur duy mu, sade mi geç?
A) Gurur duy. *(etki yok)*→K124
B) Sade geç. *(etki yok)*→K123

**K123 — Zeynep'in Halefi ⚑** *(4 kart sonra sonuçlanır)*
Zeynep kendinden sonrasını eğitmek istiyor. Atilla mı, Sibel mi?
A) Atilla. *(halef=atilla)*→K125
B) Sibel. *(halef=sibel)*→K124

**K124 — Nötr**
Büyüyen bir "sığınak kütüphanesi" oluşuyor. Katkı ver mi, izle mi?
A) Katkı ver. *(etki yok)*→K127
B) İzle. *(etki yok)*→K125

**K125 — Konuşan Zombi Doruk (1/2)**
Ömer, zombinin düzenli ziyaret edip bir yön işaret ettiğini fark ediyor. Takip et mi, görmezden mi?
A) Takip et. *(zombi_takip=evet)*→K128
B) Görmezden gel. →K126

**K126 — Konuşan Zombi Doruk (2/2) Sonuç**
*Takip:* Eski bir Vertak tesisine yönlendiriyor. *(vertak_tesis_bulundu=evet, pharma_arastirma+2)* →`☺-1`→K129
*Görmezden:* Zombi kayboluyor, gizem çözülmeden kalır. →K127

**K127 — Zeynep'in Halefi Sonucu (4 kart sonra) ⚑**
Halef eğitimi tamamlıyor, ikinci bir sağlıkçı var. *(ikinci_saglikci=evet)* →`🩺+1`→K131

**K128 — Nötr**
Sibel'in konserine dışarıdan katılanlar da oluyor. Katıl mı, izle mi?
A) Katıl. *(etki yok)*→K132
B) İzle. *(etki yok)*→K129

**K129 — YIKICI 💀☺**
*(vertak_gozlem=evet ise)* "Gözlem"in aslında sürekli takip olduğu anlaşılıyor. Sakinleştir mi, gerçeği kabul et mi?
A) Sakinleştir. `☺+1`→K133
B) Gerçeği kabul et. *(☺≤3 ise `☺=0`→SALTANAT SONU; ☺>3 ise `☺-1`)*→K130

**K130 — Nötr**
Gül'ün çocuğu ilk kelimelerini söylüyor. Kutla mı, meşgul mü?
A) Kutla. *(etki yok)*→K133
B) Meşgul ol. *(etki yok)*→K131

**K131 — Büyük Sürü Krizi (1/3) ⚑**
Mustafa: en büyük sürü yaklaşıyor. Seferberlik mi, tahliye mi?
A) Seferberlik. *(kriz=seferberlik)*→K134
B) Tahliye. *(kriz=tahliye)*→K132

**K132 — Büyük Sürü Krizi (2/3)**
Sürü artık görünür mesafede. Mustafa ve Mete pozisyon alıyor. Cepheye lider mi, geride mi?
A) Cepheye çık. *(👑<5 ise ANİ ÖLÜM; değilse `👑-3`)*→K135
B) Geride kal. `👑0`→K133

**K133 — Büyük Sürü Krizi (3/3) Sonuç**
*(Seferberlik+cephe: `🏠-1 ☺+3`) (Seferberlik+geride: `🏠-2 ☺+1`) (Tahliye+devam: `🏠-2 🥫-1`) (Tahliye+iptal: `☺-1`)* →K136

**K134 — Nötr**
Kriz sonrası sakin bir gün. Vakit geçir mi, yalnız mı kal?
A) Vakit geçir. *(etki yok)*→K137
B) Yalnız kal. *(etki yok)*→K135

**K135 — Vertak Final Yüzleşmesi (1/2)**
*(pharma_arastirma yüksekse)* İsmet: Vertak'ın gerçek yüzü gizlenemiyor. Yüzleş mi, kaçın mı?
A) Yüzleş. →K139
B) Kaçın. →K136
*(Düşükse)* Vertak hâlâ gizemli. Devam mı, unut mu?
A) Devam et. →K139
B) Unut. →K136

**K136 — Vertak Final Yüzleşmesi (2/2) Sonuç**
*(Kabul: Vertak korumasına girer, güvenlik artar özgürlük azalır) (Ret: tam bağımsızlık, tehlike sürer) (Araştırma: belirsizlik uzar, pharma_arastirma+1)* →K139

**K137 — Nötr**
Sığınakta büyük bir toplantı yapılıyor. Söz al mı, dinle mi?
A) Söz al. *(etki yok)*→K140
B) Dinle. *(etki yok)*→K138

**K138 — Zombi Anlaşması**
*(ateskes=evet ise)* Ömer, zombilerle "sınır anlaşması" önerildiğini iletir. Kabul mü, mesafe mi?
A) Kabul et. `☺+1`→K141
B) Mesafe koy. →K139

**K139 — Nötr**
Ali artık sığınağın en genç vasıflı üyesi. Gurur duy mu, mütevazı mı kal?
A) Gurur duy. *(etki yok)*→K143
B) Mütevazı kal. *(etki yok)*→K140

**K140 — LİDER RİSKİ 💀👑 (Final Tehlike)**
Mustafa ve Mete en büyük tehdidin geldiğini haber veriyor. Öne çık mı, arkada dur mu?
A) Öne çık. *(👑<5 ise ANİ ÖLÜM; değilse `👑-4`)*→K142
B) Arkada dur. `👑0 ☺-1`→K141

**K141 — Nötr**
Fırtına dinmiş gibi, sığınak bir kez daha ayakta. Nefes al mı, işe mi dön?
A) Nefes al. *(etki yok)*→K143
B) İşe dön. *(etki yok)*→K142

**K142 — Tarih Yazımı**
İsmet eski günlükleri düzenliyor, sığınağın tarihini yazmaya karar veriyor. Anlat mı, ona mı bırak?
A) Anlat. *(etki yok)*→K144
B) Ona bırak. *(etki yok)*→K143

**K143 — Nötr**
Son bir sakin akşam, hayatta kalan kadronun hepsi bir arada. Teşekkür et mi, sessizce mi otur?
A) Teşekkür et. *(etki yok)*→K146
B) Sessizce otur. *(etki yok)*→K144

**K144 — Nötr**
Emine Teyze'nin bahçesi (Aziz'in eseri) çiçek açıyor. İzle mi, geç mi?
A) İzle. *(etki yok)*→K147
B) Geç. *(etki yok)*→K145

**K145 — Nötr**
Necati eski dostlarını anıyor, sessiz bir akşam. Dinle mi, boşver mi?
A) Dinle. *(etki yok)*→K149
B) Boşver. *(etki yok)*→K146

**K146 — Nötr**
Aziz yeni bir tarif üzerinde çalışıyor. Katkı ver mi, izle mi?
A) Katkı ver. *(etki yok)*→K148
B) İzle. *(etki yok)*→K147

**K147 — Nötr**
Sığınağın nüfusu artık istikrarlı. Değerlendir mi, sıradan mı gör?
A) Değerlendir. *(etki yok)*→K151
B) Sıradan gör. *(etki yok)*→K148

**K148 — Nötr**
Sabiha'nın ticaret ağı büyüyor. Destekle mi, sınırlı mı tut?
A) Destekle. *(etki yok)*→K152
B) Sınırlı tut. *(etki yok)*→K149

**K149 — Nötr**
İsmet arşivine yeni kayıtlar ekliyor. Katkı ver mi, izle mi?
A) Katkı ver. *(etki yok)*→K150
B) İzle. *(etki yok)*→K150

**K150 — SEZON 2 DÖNÜM NOKTASI**
Sığınağın kaderi K1'den beri birikmiş tüm bayrakların toplamına bağlı: kaçıncı liderdesiniz, hangi ittifaklar kuruldu, Vertak'la ilişki nasıl, zombilerle ateşkes mi savaş mı — hepsi burada birleşiyor. *Bu bir final değildir.* 3. sezon buradan başlar.

---

### BÖLÜM V (K151-K200)

**K151 — Nötr**
Sezon 3 sakin bir günle açılıyor. Ali kendi yolunu seçiyor. Tarım mı, savunma mı?
A) Tarım. *(ali_yol=tarim, etki yok)*→K155
B) Savunma. *(ali_yol=savunma, etki yok)*→K152

**K152 — Nötr**
Veli, ikizinin seçiminden kıskanıyor. Konuş mu, zaman mı tanı?
A) Konuş. *(etki yok)*→K155
B) Zaman tanı. *(etki yok)*→K153

**K153 — Yeni Faktör**
Kemal, "Karakol" diye anılan düzenli bir yerleşim olduğunu bildiriyor. Temas mı, uzak mı?
A) Temas. *(karakol_temas=evet)*→K155
B) Uzak dur. *(karakol_temas=hayır)*→K154

**K154 — Nötr**
Fatma çocuklara resim dersi veriyor. Katıl mı, izle mi?
A) Katıl. *(etki yok)*→K157
B) İzle. *(etki yok)*→K155

**K155 — Karakol İlişkisi (1/2)**
*Temas:* İsmet radyo bağlantısı kuruyor — otoriter bir yönetim. İşbirliği mi, mesafeli mi?
A) İşbirliği öner. →K158
B) Mesafeli kal. →K156
*Uzak durulduysa:* Mete, devriyeyle karşılaşıyor. Selamla mı, çekil mi?
A) Selamla. →K158
B) Çekil. →K156

**K156 — Karakol İlişkisi (2/2) Sonuç**
*(işbirliği+kabul: `🥫+2 ☺-1`) (işbirliği+pazarlık: `🥫+1 ☺+1`) (mesafeli+şikayet: `☺-1`) (mesafeli+görmezden: gerginlik kalır, karakol_gerginlik=evet) (selamla/çekil: `☺0`)* →K158

**K157 — Nötr**
Necati, Karakol hakkında bir şeyler duymuş. Dinle mi, boşver mi?
A) Dinle. *(etki yok)*→K160
B) Boşver. *(etki yok)*→K158

**K158 — Zombi Gelişimi ⚑** *(4 kart sonra sonuçlanır)*
Ömer, zombilerin "toplanma" davranışı sergilediğini fark ediyor. İzle mi, rapor mu?
A) Yakından izle. *(zombi_izle=evet)*→K160
B) Mesafeli rapor. *(zombi_izle=hayır)*→K159

**K159 — Nötr**
Sibel'in müzik dersleri artık çocuklara da veriliyor. Katıl mı, izle mi?
A) Katıl. *(etki yok)*→K163
B) İzle. *(etki yok)*→K160

**K160 — YIKICI 💀🏠**
Kemal, eski onarımların sorun çıkardığını bildiriyor. Büyük tamir mi, ertele mi?
A) Büyük tamir. `🥫-2 🏠+2`→K163
B) Ertele. *(🏠≤3 ise `🏠=0`→SALTANAT SONU; 🏠>3 ise `🏠-1`)*→K161

**K161 — Nötr**
Gül'ün çocuğu artık yürüyor. Kutla mı, meşgul mü?
A) Kutla. *(etki yok)*→K165
B) Meşgul ol. *(etki yok)*→K162

**K162 — Zombi Gelişimi Sonucu (4 kart sonra) ⚑**
*İzlendiyse:* Örgütlendikleri doğrulanıyor. Zeynep'e ilet mi, sakla mı?
A) İlet. `☺-1` *(bilimsel_gozlem=evet)*→K164
B) Sakla. →K163
*Mesafeli rapor edildiyse:* Belirsizlik sürüyor. →K165

**K163 — Yeni Nesil (1/2)**
Ali'nin ilk büyük görevi geliyor. Bağımsız mı, yanında mı dur?
A) Bağımsız bırak. *(ali_bagimsiz=evet)*→K166
B) Yanında dur. →K164

**K164 — Yeni Nesil (2/2) Sonuç**
Ali beklenmedik bir tehlikeyle karşılaşıyor. Yardım gönder mi, izin ver mi?
A) Yardım gönder. `🥫-1 ☺+1 🩺0`→K166
B) İzin ver. *(ali_sinandi=evet)* `☺+1`→K165

**K165 — Nötr**
Yusuf ve Cem'in oyunu gençler arasında yayılıyor. Katıl mı, izle mi?
A) Katıl. *(etki yok)*→K168
B) İzle. *(etki yok)*→K166

**K166 — Karakol Gerginliği ⚑** *(6 kart sonra sonuçlanır)*
*(karakol_gerginlik=evet ise)* Kemal, Karakol'un sınırı yaklaştırdığını fark ediyor. Uyar mı, izle mi?
A) Uyar. *(karakol_uyari=evet)*→K168
B) İzle. *(karakol_uyari=hayır)*→K167
*(değilse)* Sabiha yeni bir ticaret rotası öneriyor. Riskli mi, güvenli mi?
A) Riskli. *(rota=riskli)*→K168
B) Güvenli. *(rota=guvenli)*→K167

**K167 — Nötr**
Emine Teyze'nin bahçesi çiçek açıyor. İzle mi, geç mi?
A) İzle. *(etki yok)*→K170
B) Geç. *(etki yok)*→K168

**K168 — Konuşan Zombi Derinleşme**
"Lider" zombi düzenli olarak çite geliyor. Zeynep'i çağır mı, yalnız mı dinle?
A) Zeynep'i çağır. →K171
B) Yalnız dinle. →K169

**K169 — Nötr**
İsmet eski bir müzik istasyonu sinyali yakalıyor. Dinle mi, boşver mi?
A) Dinle. *(etki yok)*→K172
B) Boşver. *(etki yok)*→K170

**K170 — YIKICI 💀☺**
Karakol söylentisi sığınağı ikiye bölüyor. Açık forum mu, bastır mı?
A) Açık forum. `☺+1`→K173
B) Bastır. *(☺≤3 ise `☺=0`→SALTANAT SONU; ☺>3 ise `☺-1`)*→K171

**K171 — Nötr**
Ali'nin ilk başarısı kutlanıyor. Kutla mı, mütevazı mı kal?
A) Kutla. *(etki yok)*→K173
B) Mütevazı kal. *(etki yok)*→K172

**K172 — Karakol Gerginliği Sonucu (6 kart sonra) ⚑**
*(Uyarıldıysa: `☺-1`) (İzlendiyse: `🏠-1 🩺-1`) (Riskli rota: değişken `🥫+2/-1`) (Güvenli rota: `🥫+1`)* →K175

**K173 — Nötr**
Büyük bir "hasat/inşaat bayramı" kutlanıyor. Katıl mı, arka planda mı?
A) Katıl. *(etki yok)*→K177
B) Arka planda kal. *(etki yok)*→K174

**K174 — LİDER RİSKİ 💀👑**
Karakol'dan görüşme daveti geliyor. Bizzat mı, temsilci mi?
A) Bizzat git. *(👑<5 ise ANİ ÖLÜM; değilse 👑-3)*→K176
B) Temsilci gönder. `👑0`→K175

**K175 — Nötr**
Sonrasında sakin bir hafta. Vakit geçir mi, işe mi dön?
A) Vakit geçir. *(etki yok)*→K177
B) İşe dön. *(etki yok)*→K176

**K176 — Vertak Yankısı**
*(K135-136'daki karara göre)* Yeni bir talep ya da eski bir sinyal geliyor. İncele mi, yok say mı?
A) İncele. *(vertak_yanki=evet)*→K179
B) Yok say. →K177

**K177 — Nötr**
Necati doğal bir şekilde vefat ediyor. Anısını an mı, sessizce devam mı?
A) An. *(etki yok)*→K180
B) Sessizce devam. *(etki yok)*→K178
*(Not: nüfus bir azalır.)*

**K178 — Genişleme Projesi (1/2)**
Kemal, sığınağı genişletme fikri sunuyor. Büyük mü, kademeli mi?
A) Büyük yatırım. *(genisleme=buyuk)* `👑-1`→K181
B) Kademeli. *(genisleme=kademeli)*→K179

**K179 — Genişleme Projesi (2/2) Sonuç**
*(Büyük: `🏠+3`) (Kademeli: `🏠+2`, yavaş ama sağlam)* →K181

**K180 — Nötr**
Yeni bölgede ilk gece. Orada mı kal, eski bölgede mi?
A) Yeni bölgede kal. *(etki yok)*→K183
B) Eski bölgede kal. *(etki yok)*→K181

**K181 — Zombi Anlaşması Teklifi (1/2)**
"Lider" zombi bir bölgeyi paylaşmayı teklif ediyor. Kabul mü, ret mi?
A) Kabul. *(zombi_anlasma=evet)*→K183
B) Ret. *(zombi_anlasma=hayır)*→K182

**K182 — Zombi Anlaşması Sonucu (2/2)**
*Kabul:* garip ama işlevsel bir komşuluk kurulur. *(zombi_komsuluk=evet)* →`☺+1 🩺-1`
*Ret:* net bir sınır çizilir. →`🏠+1`
→K184

**K183 — Nötr**
Ali kendi çırağını eğitmeye başlıyor. Gurur duy mu, doğal mı karşıla?
A) Gurur duy. *(etki yok)*→K187
B) Doğal karşıla. *(etki yok)*→K184

**K184 — YIKICI 💀🩺**
Yeni bölgeden bir hastalık riski var. Sıkı karantina mı, devam mı?
A) Sıkı karantina. `🥫-1 🩺+1`→K186
B) Devam et. *(🩺≤3 ise `🩺=0`→SALTANAT SONU; 🩺>3 ise `🩺-1`)*→K185

**K185 — Nötr**
Sibel ve öğrencileri bir konser daha veriyor. Katıl mı, izle mi?
A) Katıl. *(etki yok)*→K188
B) İzle. *(etki yok)*→K186

**K186 — İsmet'in Keşfi ⚑** *(4 kart sonra sonuçlanır)*
İsmet eski bir askeri frekansta kodlanmış bir mesaj yakalıyor. Deşifre et mi, yok say mı?
A) Deşifre et. *(mesaj_cozuldu=evet)*→K188
B) Yok say. →K187

**K187 — Nötr**
Fatma'nın resimleri dışarıya da hediye ediliyor. Destekle mi, önemseme mi?
A) Destekle. *(etki yok)*→K190
B) Önemseme. *(etki yok)*→K188

**K188 — Nötr**
Gül'ün çocuğu ilk kez "anne" dışında bir kelime söylüyor. Gülümse mi, şaşır mı?
A) Gülümse. *(etki yok)*→K192
B) Şaşır. *(etki yok)*→K189

**K189 — İsmet'in Keşfi Sonucu (4 kart sonra) ⚑**
*Deşifre:* Uzak bir topluluktan SOS mesajı. Yardıma git mi, mesafeli mi?
A) Yardıma git. `🥫-1 ☺+1` *(uzak_topluluk=evet)*→K192
B) Mesafeli kal. →K190
*Yok sayıldıysa:* Sinyal zamanla söner. →K192

**K190 — Nötr**
Haftalık toplantı geleneği sürüyor. Katıl mı, dinle mi?
A) Katıl. *(etki yok)*→K193
B) Dinle. *(etki yok)*→K191

**K191 — LİDER RİSKİ 💀👑**
*(Yardıma gidildiyse)* Ulaşmak tehlikeli. Bizzat mı, ekip mi?
A) Bizzat git. *(👑<5 ise ANİ ÖLÜM; değilse 👑-3)*→K194
B) Ekip gönder. `👑0`→K192
*(Mesafeli kalındıysa)* Sıradan, düşük riskli bir gün. →K192

**K192 — Vertak Yankısı Sonucu**
*(İncelendiyse)* Önemli bulgu çıkar. `pharma_arastirma+1` *(Yok sayıldıysa)* Sinyal söner. →K195

**K193 — Nötr**
Herkes döner, sakin bir akşam. Dinlen mi, işe mi dön?
A) Dinlen. *(etki yok)*→K196
B) İşe dön. *(etki yok)*→K194

**K194 — Nötr**
İsmet'in tarih arşivi büyüyor. Katkı ver mi, izle mi?
A) Katkı ver. *(etki yok)*→K198
B) İzle. *(etki yok)*→K195

**K195 — Zombi Komşuluk Testi**
*(zombi_komsuluk=evet ise)* Anlaşma ilk kez ciddi sınanıyor. Sakin mi, sert mi?
A) Sakin kal. `☺+1`→K198
B) Sert tepki. `☺-1` *(zombi_komsuluk_gergin=evet)*→K196

**K196 — Nötr**
Yeni bir çocuk doğuyor, isim koyma günü. Katıl mı, kısa tebrik mi?
A) Katıl. *(etki yok)*→K200
B) Kısa tebrik. *(etki yok)*→K197

**K197 — Nötr**
Kemal, küçük bir elektrik şebekesi kurduğunu gösteriyor. Kutla mı, sıradan mı?
A) Kutla. *(etki yok)*→K201
B) Sıradan karşıla. *(etki yok)*→K198

**K198 — Nötr**
Ali'nin çırağı kendi çırağını almaya hazırlanıyor. Gurur duy mu, şaşır mı?
A) Gurur duy. *(etki yok)*→K200
B) Şaşır. *(etki yok)*→K199

**K199 — Mete'nin Şüphesi ⚑** *(3 kart sonra sonuçlanır)*
Mete, Karakol ilişkilerinin gizli bir ajandası olabileceğinden şüpheleniyor. Araştır mı, güven mi?
A) Araştır. *(son_kusku=evet)*→K201
B) Güven. →K200

**K200 — Dönüm Noktası (Sezon 3 Ara Kapanışı)**
Sığınak artık büyümüş, komşuları var. Bu bir final değil — hikaye derinleşerek sürüyor.

---

### BÖLÜM VI (K201-K250)

**K201 — Nötr**
Yeni bir mevsim başlıyor. Kutla mı, sıradan mı geç?
A) Kutla. *(etki yok)*→K203
B) Sıradan geç. *(etki yok)*→K202

**K202 — Mete'nin Şüphesi Sonucu (3 kart sonra) ⚑**
*Araştırıldıysa:* Şüphe doğrulanır ya da yanlış çıkar — değişken etki.
*Güvenildiyse:* Gereksiz bir kaygıydı. →`☺+1`
→K205

**K203 — Nötr**
Veli kendi yolunu buluyor — mühendislik mi, telsizcilik mi. Destekle mi, kendi haline mi bırak?
A) Destekle. *(etki yok)*→K205
B) Kendi haline bırak. *(etki yok)*→K204

**K204 — Büyük Kriz Habercisi ⚑** *(7 kart sonra sonuçlanır)*
Mustafa ve Mete ufukta hareketlilik fark ediyor. Erken uyarı mı, gözlem mi?
A) Erken uyarı kur. `🏠-1` *(erken_uyari=evet)*→K206
B) Gözlemeye devam. *(erken_uyari=hayır)*→K205

**K205 — Karakol Krizi (1/2)**
Karakol'da iç karışıklık çıktığı haberi geliyor. Değerlendir mi, karışma mı?
A) Değerlendir. *(karakol_yeni_yonetim=evet)*→K207
B) Karışma. →K206

**K206 — Karakol Krizi (2/2) Sonuç**
*(Değerlendirildi+yakınlaş: yeni ilişki) (Değerlendirildi+temkinli: mesafeli izleme) (Karışılmadı+hazırlan: `🏠+1`) (Karışılmadı+bekle: belirsizlik)* → karışık `🥫/🏠/☺` swing →K208

**K207 — Nötr**
Ali'nin çırağı ilk bağımsız görevini tamamlıyor. Gurur duy mu, doğal mı karşıla?
A) Gurur duy. *(etki yok)*→K210
B) Doğal karşıla. *(etki yok)*→K208

**K208 — YIKICI 💀🏠**
Genişleyen sığınağın yapısal karmaşıklığı bir soruna yol açıyor. Acil mi, göze al mı?
A) Acil müdahale. `🥫-2 🏠+1`→K211
B) Göze al. *(🏠≤3 ise `🏠=0`→SALTANAT SONU; 🏠>3 ise `🏠-1`)*→K209

**K209 — Büyük Kriz Sonucu (7 kart sonra) ⚑**
*(Erken uyarı: `☺+1`) (Kurulmadıysa: `🩺-1 ☺-1`)* →K211

**K210 — Nötr**
Kriz sonrası dayanışma güçleniyor. Kutla mı, sessizce hisset mi?
A) Kutla. *(etki yok)*→K212
B) Sessizce hisset. *(etki yok)*→K211

**K211 — Zombi Komşuluk Derinleşme**
*(zombi_komsuluk=evet ise)* "Lider" zombi karmaşık bir şey ifade etmeye çalışıyor. Zeynep'le mi, tek mi?
A) Zeynep'le dinle. →K213
B) Tek başına dinle. →K212
*(değilse)* Sıradan bir nöbet günü. →K212

**K212 — Nötr**
İsmet'in arşivi sığınağın gururu. Katkı ver mi, izle mi?
A) Katkı ver. *(etki yok)*→K215
B) İzle. *(etki yok)*→K213

**K213 — Yeni Nesil Liderlik**
Ali ya da Veli ilk kez resmi bir karar toplantısına katılıyor. Söz hakkı ver mi, izle mi?
A) Söz hakkı ver. `☺+1`→K215
B) İzle. →K214

**K214 — Nötr**
Sabiha'nın ticaret ağı birden fazla topluluğu kapsıyor. Genişlet mi, sınırlı mı?
A) Genişlet. `🥫+1` *(ticaret_agi=genis)*→K216
B) Sınırlı tut. *(ticaret_agi=sinirli)*→K215

**K215 — LİDER RİSKİ 💀👑**
Karakol krizi doğrudan sığınağa sıçrıyor. Bizzat mı, ekibe mi bırak?
A) Bizzat git. *(👑<5 ise ANİ ÖLÜM; değilse 👑-3)*→K218
B) Ekibe bırak. `👑0`→K216

**K216 — Nötr**
Sakinlik geri geliyor. Dinlen mi, işe mi dön?
A) Dinlen. *(etki yok)*→K218
B) İşe dön. *(etki yok)*→K217

**K217 — Aziz'in Büyük Hasadı (1/2)**
Tarım alanı genişletildiyse rekor bir hasat mümkün. Riske gir mi, güvenli mi?
A) Riske gir. *(hasat=riskli)*→K219
B) Güvenli ilerle. *(hasat=guvenli)*→K218

**K218 — Aziz'in Büyük Hasadı (2/2) Sonuç**
*(Riskli: değişken, çoğunlukla `🥫+4`, kötü giderse `🥫+1`) (Güvenli: istikrarlı `🥫+2`)* →K221

**K219 — Nötr**
Sığınakta ilk kez fazla erzak "ihraç" ediliyor. Kutla mı, tedbirli mi?
A) Kutla. *(etki yok)*→K223
B) Tedbirli ol. *(etki yok)*→K220

**K220 — Vertak'ın Sonu ya da Devamı**
*(pharma_arastirma ve K135-136'ya göre)* Vertak hikayesi netleşiyor. Kutla/rahatla mı, temkinli mi?
A) Kutla/rahatla. `☺+2`→K223
B) Temkinli kal. `🏠+1`→K221

**K221 — Nötr**
Sığınağın en yaşlısı geçmişi genç nesile anlatıyor. Dinle mi, işine mi dön?
A) Dinle. *(etki yok)*→K224
B) İşe dön. *(etki yok)*→K222

**K222 — YIKICI 💀☺**
Yeni nesille eski nesil arasında değerler çatışması. Ortak karar mı, otorite mi?
A) Ortak karar ara. `☺+1`→K226
B) Otorite kullan. *(☺≤3 ise `☺=0`→SALTANAT SONU; ☺>3 ise `☺-1`)*→K223

**K223 — Nötr**
Uzlaşma ya da gerginlik sonrası sakin bir hafta. Vakit geçir mi, yalnız mı?
A) Vakit geçir. *(etki yok)*→K226
B) Yalnız kal. *(etki yok)*→K224

**K224 — Dönüm Noktası (Ara)**
Sığınak artık kurulduğu günden çok farklı bir yer. Devam ediyor. →K225

**K225 — Nötr**
Sığınakta bir "gelenek günü" var, ilk günden beri hayatta kalanlar anılıyor. Katıl mı, izle mi?
A) Katıl. *(etki yok)*→K229
B) İzle. *(etki yok)*→K226

**K226 — Nötr**
Kemal'in mühendislik mirası artık kalıcı bir yapı taşı. Değerlendir mi, sıradan mı?
A) Değerlendir. *(etki yok)*→K230
B) Sıradan gör. *(etki yok)*→K227

**K227 — Konuşan Zombi Finali (1/2)**
"Lider" zombi son kez net bir şekilde konuşuyor — uyarı mı, veda mı, teklif mi. Dikkatle dinle mi, mesafede mi kal?
A) Dikkatle dinle. →K230
B) Mesafede kal. →K228

**K228 — Konuşan Zombi Finali (2/2) Sonuç**
*Dinlendiyse:* Önemli bir bilgi/uyarı alınır. *(zombi_son_mesaj=evet)*
*Mesafede kalındıysa:* Belirsizlik sürer.
*(Tüm zombi bayraklarının toplamına göre büyük dallanma: kalıcı bir ateşkese dönüşür ya da tamamen gizemli kalır.)*
→K230

**K229 — Nötr**
Sakin bir akşam, sığınağın artık bir "ev" olduğu hissediliyor. Yansıt mı, sessizce yaşa mı?
A) Yansıt. *(etki yok)*→K231
B) Sessizce yaşa. *(etki yok)*→K230

**K230 — LİDER RİSKİ 💀👑 (Son Büyük Tehlike)**
Yıllardır biriken tüm gerilimler bir araya gelip en büyük krizi yaratıyor. Öne çık mı, kadroya güven mi?
A) Öne çık. *(👑<5 ise ANİ ÖLÜM; değilse 👑-4)*→K234
B) Kadroya güven. `👑0 ☺+1`→K231

**K231 — Nötr**
Fırtına dinmiş, sığınak bir kez daha ayakta. Nefes al mı, işe mi dön?
A) Nefes al. *(etki yok)*→K234
B) İşe dön. *(etki yok)*→K232

**K232 — Nötr**
Sabiha, Aziz, Kemal, İsmet — hepsinin mirası sığınağın kimliğini oluşturuyor. Fark et mi, sıradan mı?
A) Fark et. *(etki yok)*→K236
B) Sıradan gün. *(etki yok)*→K233

**K233 — Nötr**
Ali, Veli ve yeni nesil geleceği kendi elleriyle şekillendiriyor. Güven mi, temkinli mi?
A) Güven. *(etki yok)*→K235
B) Temkinli ol. *(etki yok)*→K234

**K234 — Uzun Vadeli Sentez**
Tüm ilişkiler (Vertak, Karakol, zombiler) bir arada değerlendiriliyor — sığınak bölgede kendi başına bir güç mü, hâlâ kırılgan mı? *(Bayrakların toplamına göre metin değişir.)* →K238

**K235 — Nötr**
İsmet'in arşivinde kaçıncı lider olduğunuz, kaç gündür ayakta olduğunuz yazılı. Oku mu, ona mı bırak?
A) Oku. *(etki yok)*→K237
B) Ona bırak. *(etki yok)*→K236

**K236 — Nötr**
Büyük bir toplantı yapılıyor, artık gerçek bir "topluluk" gibi karar veriliyor. Söz al mı, dinle mi?
A) Söz al. *(etki yok)*→K238
B) Dinle. *(etki yok)*→K237

**K237 — Nötr**
Zeynep'in eğittiği halef artık kendi başına yeterli. Gurur duy mu, doğal mı?
A) Gurur duy. *(etki yok)*→K239
B) Doğal karşıla. *(etki yok)*→K238

**K238 — Nötr**
Ömer'in güvenliği, Mustafa ve Mete'nin savunması — kalıcı yapı taşları. Değerlendir mi, sıradan mı?
A) Değerlendir. *(etki yok)*→K242
B) Sıradan gör. *(etki yok)*→K239

**K239 — Nötr**
Son bir sakin akşam, herkes bir arada. Teşekkür et mi, sessizce mi otur?
A) Teşekkür et. *(etki yok)*→K242
B) Sessizce otur. *(etki yok)*→K240

**K240 — Nötr**
Yıllardır süren yolculuk, ilk günün korkusundan çok uzakta bir yere gelmiş. Geriye mi, ileriye mi?
A) Geriye bak. *(etki yok)*→K242
B) İleriye bak. *(etki yok)*→K241

**K241 — Nötr**
Sığınağın halk arasında oluşmuş bir ismi bile var. Resmi mi yap, doğal mı bırak?
A) Resmi yap. *(etki yok)*→K244
B) Doğal bırak. *(etki yok)*→K242

**K242 — Nötr**
Emine Teyze'nin bahçesi hâlâ çiçek açıyor, ilk günden beri süren bir sembol. İzle mi, geç mi?
A) İzle. *(etki yok)*→K245
B) Geç. *(etki yok)*→K243

**K243 — Nötr**
Gül'ün çocuğu artık okula benzer bir derse katılıyor, Atilla'nın mirası sürüyor. Katıl mı, izle mi?
A) Katıl. *(etki yok)*→K246
B) İzle. *(etki yok)*→K244

**K244 — Nötr**
Aziz'in tarım mirası artık sığınağın temel geçim kaynağı. Fark et mi, sıradan mı?
A) Fark et. *(etki yok)*→K248
B) Sıradan gör. *(etki yok)*→K245

**K245 — Nötr**
Son kart öncesi, herkes bir arada, sessiz bir gurur var havada. Hisset mi, geleceğe mi odaklan?
A) Hisset. *(etki yok)*→K248
B) Geleceğe odaklan. *(etki yok)*→K246

**K246 — Nötr**
Kadronun hepsi (Zeynep, Sabiha, Ömer, Kemal, Atilla, Aziz, İsmet, Mustafa, Mete) bir arada son bir toplantı yapıyor. Katıl mı, dinle mi?
A) Katıl. *(etki yok)*→K250
B) Dinle. *(etki yok)*→K247

**K247 — Nötr**
Sığınağın günlüğüne son bir kayıt düşülüyor. Sen mi yaz, İsmet mi?
A) Sen yaz. *(etki yok)*→K250
B) İsmet yazsın. *(etki yok)*→K248

**K248 — Nötr**
Gece çöküyor, sığınak sessizleşiyor — ama huzurlu bir sessizlik bu sefer. Dışarı bak mı, içeri dön mü?
A) Dışarı bak. *(etki yok)*→K250
B) İçeri dön. *(etki yok)*→K249

**K249 — Nötr**
Son an — kaç lider geldi geçti, kaç gün geçti, kimin hatırladığı önemli değil artık; sığınak ayakta. Düşün mü, sadece hisset mi?
A) Düşün. *(etki yok)*→K250
B) Sadece hisset. *(etki yok)*→K250

**K250 — SEZON 3 DÖNÜM NOKTASI (Büyük Kapanış)**
Sığınağın kaderi artık K1'den beri birikmiş 250 kartlık tüm kararların toplamına bağlı: kaç lider geldi geçti, hangi ittifaklar (Karakol, Vertak) kuruldu ya da yıkıldı, konuşan zombilerle ilişki nasıl şekillendi — hepsi burada birleşiyor. *Bu hâlâ bir final değildir.* Sistem aynı kurallarla sonsuza dek üretilebilir; 4. sezon buradan başlar.

---

## 5. ÖZET
250 kart, tek dosya, tek kadro. **Hiçbir ara-etiket (K1a/K1b tarzı) yok** — her kart doğrudan gerçek kart numaralarına gider. Nötr olmayan kartların büyük çoğunluğunda A ve B **farklı numaralara** gider; atlanan kart yalnızca diğer yolu seçenlerin gördüğü bir kart olur. Değişken gecikmeli zincirler, çok kartlı olaylar, deterministik ölüm kuralı, doğru saltanat geçişi (asla baştan başlanmaz) korunmuştur.
