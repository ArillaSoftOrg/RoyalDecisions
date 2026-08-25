# SIĞINAK: SALTANAT GÜNLÜKLERİ
### v12 — ONARILMIŞ SÜRÜM: Graf Bütünlüğü + Anlatı Kalitesi Geçişi

**v12 değişiklik özeti** *(AŞAMA 1 denetiminin onaylanmış bulgularına dayanır; StoryChapter*.cs referans/karşılaştırma amaçlı kullanıldı, otorite olarak alınmadı)*:
- 10 P0 graf hatası onarıldı (K20-24 barikat/saklanma çapraz bulaşması, K31-32/K40-44/K34-37/K45-47/K65-68 kopuk zincirler, K100/150/200 sezon geçiş kenarları, K199-202 ölü uç, K102-104-106 gizli kilitlenme, ve yazım sırasında bulunan onuncusu: K205-207 Karakol Krizi kopuk zinciri). Her biri script ile BFS doğrulaması geçti.
- 16 belirsiz/rastgele ifade ("değişken", "swing", "çoğunlukla" vb.) bayrak/eşik tabanlı deterministik sonuçlara bağlandı; kalan 8 "ya da/etkisiz" geçişi ya zaten bayrağa bağlı ya da salt anlatı özeti (yeniden oynanabilirlik referansı).
- 21 kart içindeki çiğ "*Etiket:*" varyant metni doğal, ayrık sahnelere dönüştürüldü; belirsiz-konuşmacı kartları (K7, K44, K213) netleştirildi.
- Her "Nötr" başlıklı kart özel bir isim aldı; 250 karttan 93'ü hâlâ mekanik olarak etkisiz (bilinçli tempo/karakter kartları) — v11'de 107'ydi.
- K226-250 bandında anlamlı kararlar 2/25'ten 9/25'e çıkarıldı; Ali/Veli büyüme beat'leri (13 kopya) farklı, ayırt edici içerikle yeniden yazıldı — kart sayısı hâlâ 250, hiçbir kart silinmedi.
- A/B seçimi hiç olmayan 29 "sonuç" kartının tamamına gerçek karar eklendi (0 kart artık kararsız).
- Kart numaralama şeması (K1-K250) korunmuştur — sadece hedef kenarları ve gövde metinleri değişti, yeni ara-etiket icat edilmedi.

---

## 0. KADRO — 21 KİŞİ (9 VASIFLI)
**Zeynep** (Doktor) · **Sabiha** (Erzakçı) · **Ömer** (Gözcü) · **Kemal** (Mühendis) · **Atilla** (Sığınak Görevlisi) · **Aziz** (Tarımcı) · **İsmet** (Telsizci) · **Mustafa** (Asker) · **Mete** (Asker)
**Diğer 12:** Emine Teyze · Rıza · Ali & Veli · Semra · Necati · Gül · Tarık · Sibel · Yusuf · Fatma · Cem

## 1. SİSTEM
🥫 Erzak · 🏠 Barınak · 🩺 Toplum Sağlığı · ☺ Toplum Morali *(0-10, başlangıç 5)* + 👑 Lider Sağlığı *(0-10, başlangıç 10, ayrı ölçü)*.

**Saltanat Döngüsü:** Herhangi bir ölçü 0'a düşerse lider değişir. Hikaye **bir sonraki karttan** devam eder (asla geriye/baştan başlanmaz), sıfırlanan madde 3'e resetlenir, 👑 yeni lider için 10 olur, diğer her şey korunur. Kartlardaki her "→K(sayı)" oku, SALTANAT SONU tetiklense de tetiklenmese de geçerlidir.

## 2. DALLANMA KURALI — BASİT VE NET
**Hiçbir ara-etiket, hiçbir "K1a/K1b" yok.** Her kartta A ve B, doğrudan **gerçek kart numaralarına** gider. Bir kartın iki farklı bayrak durumuna göre iki ayrı sahne göstermesi gerektiğinde (ör. "temas edildiyse" / "izlendiyse"), bu artık düz metin içinde koşullu cümle olarak değil, **iki ayrı, kendi başına tamamlanmış sahne** olarak yazılır ve hangi bayrağın hangisini seçtiği sahnenin sonunda küçük bir editör notuyla belirtilir — motor tarafında bu `CardVariant` olarak karşılık bulur.

## 3. TEKNİK NOTLAR
- **Ölüm kuralı (kesin):** Hikaye hiçbir zaman kendiliğinden, anlatı gereği lideri öldürmez. Lider yalnızca **oyuncunun seçimlerinin doğrudan sonucu** olarak ölür: (a) bir seçenek bilerek bir maddeyi sıfırlarsa, ya da (b) "Lider Riski" kartlarında, önceki kararların birikimiyle 👑 zaten kritik düşükken (`<5`) yine de riskli seçenek seçilirse. Madde/👑 sağlıklıysa aynı seçenek asla ölümle sonuçlanmaz, sadece küçük bir bedeli olur. Şans, zar ya da "bazen ölürsün" gibi rastgelelik hiçbir yerde yoktur.
- **Determinizm ilkesi (v12):** Hiçbir sonuç "değişken", "swing", "çoğunlukla" gibi belirsiz ifadeyle bırakılmaz. Her çok-değerli sonuç, ya önceden set edilmiş bir bayrağa, ya bir kaynak eşiğine, ya da doğrudan önceki bir oyuncu kararına bağlanır.
- **Değişken gecikmeli zincirler:** bir zincirin kararı ile sonucu arasındaki mesafe oyuncunun izlediği yola göre değişebilir — kasıtlıdır. **v12 ek kuralı:** bir sonuç kartı, o rotada garanti şekilde set edilmemiş bir bayrağı asla talep etmez; her gecikmeli zincirin *her iki* dalı da kendi sonuç kartına mutlaka ulaşır.
- **Çok kartlı olaylar:** bazı olaylar 2-4 kart boyunca sürer, her adımında yeni bir karar vardır; bu kartlar kendi aralarında sıralı ilerler.

---

## 4. KART KATALOĞU

### BÖLÜM I (K1-K25)

**K1 — İlk Gün**
Ömer kapıdan koşarak gelir. “Dışarıda hâlâ insanlar var. Şimdi kapatırsak bazıları dışarıda kalacak.”
A) Kapıları kapat. `🏠+1 ☺-1`→K2
B) Birkaç kişiyi daha içeri al. `☺+1 🥫-1 🏠-1`→K3
**K2 — Vicdan**
Atilla bir süre sessizce yanında durur. “Kapının dışında bıraktıklarımızı herkes gördü. Kimse konuşmuyor ama unutmuş da değiller.”
A) Konuyu kapat. `☺-1`→K4
B) Herkesi topla, konuşalım. `☺+1`→K5
**K3 — Kalabalık**
Sabiha elindeki defteri açar. “İçeride planladığımızdan fazla insan var. Depo bu hızla uzun süre dayanmaz.”
A) Porsiyonları küçült. `🥫-1 ☺+1` *(k3_yolu=evet)*→K6
B) Kimseyi kısmadan dağıt. `🥫-2 ☺+2`→K7
**K4 — Gerginlik**
Mustafa öfkeli bir hâlde gelir. “İki grup birbirine girdi. Biraz daha sürerse yumruklar konuşacak.”
A) Araya gir, düzeni sağla. `🏠+1 ☺-1`→K7
B) İki tarafı da masaya oturt. `☺+1 🏠-1`→K8💀
**K5 — Karar Anı**
Sabiha çantasını hazırlamış bekliyor. “Dışarı çıkıp çevreyi tarayabiliriz. Ama içeride toparlanacak çok iş var.”
A) Keşif ekibini çıkar. `☺0`→K9⚑
B) Önce sığınağı toparla. `🏠+1`→K6
**K6 — Yüzleşme**
Kemal krokiyi masaya serer. “Bu kadar kişiyi tek bölmede tutmak güvenli değil. Araya duvar çekebilirim ama yer daha da daralır.”
A) Bölmeleri ayır. `🏠-1` *(bolme_karari=evet)*→K7
B) Herkesi bir arada tut. `☺+1 🩺-1`→K13
**K7 — Bölme/Otorite Sonrası**
*(Sınırsız paylaşımdan gelindiyse — K3-B)*
Mustafa yüzünü asar. “Kısıtlama olmayınca kimi iki pay aldı, kimi aç kaldı. Bir daha olmaması için bir düzen koymamız gerek.”
A) Kesin kurallar koy. `🏠+1 🥫-1`→K10
B) Bu kez uyarıyla geçiştir. `☺-1`→K10

*(Bölme kararından gelindiyse — K6-A, varyant: bolme_karari)*
Kemal ellerindeki tozu silkeler. “Duvar tamam ama ayrılanlar homurdanıyor. Bu düzen kalıcı mı olacak?”
A) Kararın arkasında dur. `🏠+1 🥫-1`→K10
B) Geçici olduğunu söyle. `☺-1`→K10
**K8 — YIKICI 💀🥫**
Rıza kalabalığın içinden bağırır: “Bir geceliğine de olsa karnımız doysun!” Sabiha hemen karşı çıkar: “Depoyu boşaltırsak yarını çıkaramayız.”
A) Dağıtımı kısıtlı tut. `🥫-1 ☺-1`→K11
B) Depoyu aç, bir gecelik ziyafet ver. `🥫=0`→**SALTANAT SONU**→K11
**K9 — Nöbet Kararı ⚑**
Ömer gözlerini ovuşturarak gelir. “Nöbetçi az. Ya çevreyi geniş tutup seyrek gezeceğiz ya da girişlere yığılıp sıkı nöbet tutacağız.”
A) Çevreyi geniş tut. `☺+1` *(nobet=gevsek)*→K11
B) Girişleri sıkı tut. `🏠+1` *(nobet=siki)*→K12
**K10 — Gece Yarısı**
Zeynep elindeki feneri yüzüne tutar. “Kaç gecedir doğru dürüst uyumadın. Biraz daha zorlarsan ayakta duramayacaksın.”
A) Bu gece dinlen. `👑+1`→K14
B) Nöbete katıl. `👑-1 ☺+1`→K11
**K11 — Sızıntı (Gevşek Nöbet Sonucu) ⚑**
Ömer nefes nefese gelir. “Birisi içeri sızmış. Henüz kimse fark etmedi.”
A) Sessizce etkisiz hâle getir. `🩺-1 👑-1`→K14
B) Herkesi uyandır. `☺-1 🩺-1`→K15
**K12 — Yaralı (Sıkı Nöbet Sonucu) ⚑**
Ömer bu kez gururlu ama gergindir. “Saldırıyı püskürttük. Bir nöbetçi yaralandı.”
A) Zeynep’i hemen çağır. `🩺+1`→K14
B) Şimdilik beklesin. `🩺-1`→K15
**K13 — Hastalık Belirtisi ⚑**
Zeynep aceleyle gelir. “Birinde döküntü başladı. Ne olduğunu bilmiyoruz; diğerlerinden ayırmazsak risk almış oluruz.”
A) Karantinaya al. `🩺+1 ☺-1` *(karantina=evet)*→K17
B) Şimdilik dokunma. `☺+1 🩺-1` *(karantina=hayir)*→K18
**K14 — LİDER RİSKİ 💀👑**
Mete kapıda bekler. “Dışarıda bir gölge dolaşıyor. Kim olduğunu göremedim.”
A) Kendin kontrol et. *(👑<5 ise ANİ ÖLÜM; değilse 👑-3)*→K18
B) Nöbetçiyi gönder. `👑0 ☺-1`→K15
**K15 — Yaralı Yabancı**
Zeynep kapının önündeki yaralı yabancıyı gösterir. “Durumu kötü. İçeri alırsak ne taşıdığını da içeri almış oluruz.”
A) İçeri al. `🩺-1 ☺+1`→K19
B) Dışarıda gözlem altında tut. *(etki yok)*→K16
**K16 — Kart Gecesi**
Ali ile Veli köşede kart oynuyor. Atilla boş bir sandalye çekip sana bakar. “Bir el sürer, hepsi bu.”
A) Bir el otur. *(etki yok)*→K19
B) İşinin başına dön. *(etki yok)*→K17
**K17 — Sonuç (Karantina Evet) ⚑**
Zeynep gözlerini kaçırır. “Hastalık yayılmadı. Ama karantinaya aldığımız kişi geceyi çıkaramadı.”
A) Küçük bir tören düzenle. `☺+1`→K19
B) Sessizce gömüp işlere dön. `🥫+1`→K20
**K18 — Sonuç (Karantina Hayır) ⚑**
Zeynep telaşla gelir. “Döküntü başkalarında da çıktı. Artık bekleyemeyiz.”
A) Geç de olsa karantina uygula. `🩺-2 ☺-1`→K20
B) Eldekilerle tedavi etmeye çalış. `🩺-1`→K19
**K19 — Vertak İpucu**
İsmet yaralının eşyaları arasında bir kimlik kartı bulur. Kartın üzerinde tek bir isim okunuyor: “Vertak.”
A) Sahibine sorular sor. `☺-1` *(vertak_ipucu+1)*→K21
B) Kartı şimdilik sakla. *(etki yok)*→K20
**K20 — Sürü Yaklaşıyor ⚑**
Ömer koşarak içeri girer. “Sürü yaklaşıyor. Çok kalabalıklar; hazırlanmak için fazla vaktimiz yok.”
A) Barikat kur. `🏠+1 🥫-1` *(savunma=barikat)*→K23
B) Işıkları söndür, herkes saklansın. `☺-1` *(savunma=saklanma)*→K21
**K21 — Son Kahve**
Aziz saklanırken kalan son kahveyi demler. Kupayı uzatır. “Böyle bir gecede işe yarar.”
A) Kahveyi iç. *(etki yok)*→K24
B) Başkasına ver. *(etki yok)*→K22
**K22 — Sessizlik**
Sığınak sessizliğe gömülür. Mustafa fısıldar: “Herkesi tek yerde tutabilirim. Ya da nöbetçileri girişlere dağıtırım.”
A) Herkesi içeride topla. `☺+1`→K24
B) Nöbetçileri girişlere yerleştir. `👑-1 🏠+1`→K24
**K23 — Sonuç (Barikat) ⚑**
Kemal hasarlı barikata bakar. “İşe yaradı ama bir darbeyi daha kaldırmaz.”
A) Hemen onar. `🏠-1 🥫-1`→K26
B) Onarımı ertele. `🏠-2`→K26
**K24 — Sonuç (Saklanma) ⚑**
Ömer sesini alçaltır. “Sürü bizi fark etmeden geçti. Ama içeriden biri az daha ses çıkarıyordu.”
A) Herkesi açıkça uyar. `☺-1`→K25
B) Konuyu büyütme. *(nöbet gevşekse `🩺-2`, sıkıysa etkisiz)*→K25
**K25 — Eşik**
İsmet kulaklığı çıkarıp masaya bırakır. “Vertak frekansı hâlâ açık. İstersek ilk teması şimdi kurabiliriz.”
A) Sinyal gönder. →K26
B) Sessiz kal, kendi yolumuzda devam et. →K29
---

### BÖLÜM II (K26-K60)

**K26 — Tozlu Gitar**
Semra depoda tozlanmış bir gitar bulur. Tellerini yoklayıp sana bakar. “Biraz uğraşırsam yine ses verir.”
A) Tamir etmesine izin ver. *(etki yok)*→K29
B) Şimdilik olduğu yerde kalsın. *(etki yok)*→K27
**K27 — Çitteki Ses (1/2) ⚑🧟**
Ömer nöbetten inerken duraksar. “Çitin ötesinden bir ses geliyor. Kelimelere benziyor.”
A) Yakından bak. `☺-1` *(cit_yaklastik=evet)*→K31
B) Uzaktan izlemeye devam et. *(etki yok)* *(cit_yaklastik=hayir)*→K28
**K28 — Çitteki Ses (2/2) Sonuç**
*(Yaklaşıldıysa)*
Çitin ötesindeki yaratık boğuk bir sesle “Yardım” der. Gözleri artık insana ait görünmüyor.
A) Ateş et. `☺-1`→K31
B) Ne söyleyeceğini dinle. `☺-2` *(zombi_konustu=evet)*→K31

*(Uzaktan izlendiyse — varyant: cit_yaklastik)*
Ses bir anda kesilir. Ömer karanlığa bakar. “Yerini kaybettim.”
A) Devriyeyi artır. `🏠+1`→K30
B) Nöbet düzenini değiştirme. *(etki yok)*→K31
**K29 — Zeynep'in Yorumu**
Zeynep duyduklarını düşünür. “Vertak notlarında buna benzeyen bir vaka vardı. Aynı şeyse bilmemiz gereken çok şey var.”
A) İzini araştır. `☺-1` *(pharma_arastirma+1)*→K34
B) Konuyu kapat. *(etki yok)*→K30
**K30 — Emine Teyze'nin Anısı**
Emine Teyze eski günlerden bir hikâye anlatmaya başlar. Bir süreliğine sığınağın duvarları yokmuş gibi olur.
A) Yanına oturup dinle. `☺+1`→K33
B) İşine dön. *(etki yok)*→K33
**K31 — Sabiha'nın Seferi (1/2) ⚑**
Sabiha haritayı masaya açar. “Yakındaki depoya ulaşabiliriz. Üç kişi daha sessiz olur; beş kişi daha çok yük taşır.”
A) Üç kişilik ekip gönder. `🥫0` *(sefer_ekip=kucuk)*→K32
B) Beş kişilik ekip gönder. `🥫0 🏠-1` *(sefer_ekip=buyuk)*→K32
**K32 — Sabiha'nın Seferi (2/2) Sonuç**
İsmet telsizi sana uzatır. Hattın öbür ucunda bağrışmalar vardır: ekip bir sürüye yakalanmıştır.
A) Hemen geri çekilmelerini emret. `☺+1 🥫+1`→K35
B) Görevi tamamlamalarını iste. *(ekip küçükse `🥫+1 🩺-1`; büyükse `🥫+3 ☺-2`)*→K35
**K33 — Aziz'in Yemeği**
Aziz topladığı sebzelerden sıcak bir yemek çıkarır. “Bugün yiyebiliriz. Ya da yarına bırakırız.”
A) Bugün ye. *(etki yok)*→K36
B) Sakla. *(etki yok)*→K36
**K34 — Kemal'in Şüphesi ⚑**
Kemal duvara birkaç kez vurup sesi dinler. “Temelde çatlak var. Beklersek büyüyebilir.”
A) Şimdi onar. `🏠0 🥫-1` *(catlak=onarildi)*→K37
B) Şimdilik bekle. *(catlak=bekletildi)*→K36
**K35 — Balkon Konseri**
Ali ile Veli gitarı ele geçirip küçük bir “konser” verir. Sığınakta ilk kez birkaç kişi gerçekten güler.
A) Alkışla. *(etki yok)*→K38
B) Kenardan izle. *(etki yok)*→K38
**K36 — Salgının Kökeni**
İsmet eski bir Vertak raporunu masaya bırakır. Suş-7 deneyinin kontrolden çıktığı yazılıdır. Kemal’in sözünü ettiği nemli çatlak da rapordaki koşullarla ürkütücü biçimde örtüşür.
A) Bildiklerini herkese anlat. `☺-2`→K37
B) Şimdilik yalnızca kadroyla paylaş. *(etki yok)*→K37
**K37 — Kemal'in Şüphesi Sonucu ⚑**
*(Onarıldıysa)*
Kemal duvarı yeniden kontrol eder. “Şimdilik sağlam. İstersen burada bırakırız, istersen son bir kez baştan sona bakarım.”
A) İşi burada bitir. `☺+1`→K40
B) Ayrıntılı kontrol yap. `🏠+1 👑-1`→K40

*(Bekletildiyse — varyant: catlak=bekletildi)*
Çatlak büyümüş, içeri su almaya başlamıştır. Kemal küfreder. “Artık erteleyemeyiz.”
A) Hasarlı bölümü onar. `🏠-1 🥫-1`→K40
B) Bölmeyi boşalt. `🏠-2 ☺-1`→K39
**K38 — Atilla Arada**
Rıza ile Tarık birbirine girmiştir. Atilla ikisinin arasında durup sana bakar. “İstersen sen konuş. İstersen ben halledeyim.”
A) Araya kendin gir. *(etki yok)*→K42
B) Atilla’ya bırak. *(etki yok)*→K40
**K39 — YIKICI 💀☺**
Necati kalabalığın ortasında sesini yükseltir: “Bize her şeyi anlatmıyor!” İnsanlar dönüp sana bakar.
A) Bildiklerini açıkça anlat. `☺+1`→K42
B) Tartışmayı zorla kes. *(☺≤3 ise `☺=0`→SALTANAT SONU; ☺>3 ise `☺-1`)*→K43
**K40 — İsmet'in Sinyali (1/3) ⚑**
İsmet kulaklığını çıkarmaz. “Aynı frekanstan tekrar tekrar sinyal geliyor. Bizi özellikle arıyor olabilirler.”
A) Cevap ver. `☺+1` *(sinyal=cevaplandi)*→K41
B) Sessiz kal. *(sinyal=sessiz)*→K41
**K41 — İsmet'in Sinyali (2/3)**
*(Cevaplandıysa)*
Karşı taraf doğrudan koordinat ister. İsmet eli vericinin üzerinde bekler.
A) Konumu paylaş. *(konum_paylasildi=evet)*→K43
B) Konumu verme. *(etki yok)*→K43

*(Sessiz bırakıldıysa — varyant: sinyal=sessiz)*
Sinyal daha sık gelmeye başlar. Karşı taraf cevap almadan vazgeçmiyordur.
A) Cihazı kapat. `🥫-1`→K43
B) Frekansı açık bırak. *(etki yok)*→K43
**K42 — Gül'ün Bebeği**
Gül’ün bebeği ilk kez kahkaha atar. Tartışmaların ortasında herkes birkaç saniyeliğine susar.
A) Gülümse. *(etki yok)*→K45
B) İşine dön. *(etki yok)*→K43
**K43 — İsmet'in Sinyali (3/3) Sonuç**
Ömer kapıdan haber verir. “Dışarıda bir araç durdu. İçindekiler bekliyor.”
A) Kapıda karşıla. →K46
B) Kapıları kilitle. →K44
**K44 — Vertak Konuşması**
*(Koordinat paylaşıldıysa)* Kapıda gerçekten bir Vertak temsilcisi vardır; sakin, temiz ve hazırlıklıdır. *(Paylaşılmadıysa)* Kapıda kimliği belirsiz, gergin bir grup bekler.
A) Konuşmayı kabul et. *(konum paylaşıldıysa `☺+1`, paylaşılmadıysa `☺-2`)*→K47
B) Mesafeyi koru. *(etki yok)*→K45
**K45 — Bebeğin Ateşi ⚑**
Zeynep bebeğin başında bekler. “Ateşi yükseliyor. Elimizde bir doz ilaç kaldı.”
A) Son ilacı kullan. `🥫-1` *(ates_ilac=evet)*→K47
B) Bir süre daha gözlemle. *(ates_ilac=hayir)*→K46
**K46 — Sibel'in Sessizliği**
Sibel köşede sessizce yıpranmış ayakkabıları onarır. Bebeğin ateşi konuşulurken bile elindeki işe devam eder.
A) Emeği için teşekkür et. *(etki yok)*→K49
B) Sessizce geç. *(etki yok)*→K47
**K47 — Bebeğin Ateşi Sonucu ⚑**
*(İlaç kullanıldıysa)*
Sabaha karşı bebeğin ateşi düşer. Zeynep sonunda omuzlarını gevşetir.
A) Tehlikenin geçtiğini kabul et. `☺+1`→K50
B) Bir gece daha gözlem altında tut. `☺+1`→K50

*(Beklenildiyse — varyant: ates_ilac=hayir)*
Ateş daha da yükselince son ilaç yine kullanılır. Zeynep yorgun gözlerle sana bakar.
A) Kimseyi suçlama. `🥫-1 🩺-1`→K48
B) Kararını sorgula. `🥫-1 🩺-1 👑-1`→K48
**K48 — LİDER RİSKİ 💀👑**
Mustafa aceleyle gelir. “Birkaç enfekteli dış hatta kadar sokuldu. Komutayı biri almalı.”
A) Savunmayı kendin yönet. *(👑<5 ise ANİ ÖLÜM; değilse `👑-2 🏠+1`)*→K51
B) Komutayı Mustafa’ya bırak. `👑0 🏠+1 ☺-1`→K49
**K49 — Zar Oyunu**
Cem ile Yusuf bir çift zar bulmuş, kendi kurallarını uydurmuşlardır. Sana da yer açarlar.
A) Oyuna katıl. *(etki yok)*→K52
B) İzleyip geç. *(etki yok)*→K50
**K50 — YIKICI 💀🏠**
Kemal kapının menteşesini söküp önüne bırakır. “Bununla bir saldırı daha karşılamayız. Ya düzgünce yenileriz ya da şansımıza güveniriz.”
A) Kaynak ayırıp tamamen yenile. `🥫-2 🏠+2`→K53
B) Şimdilik idare et. *(🏠≤3 ise `🏠=0`→SALTANAT SONU; 🏠>3 ise `🏠-1`)*→K51
**K51 — Vertak Teması**
İsmet Vertak frekansını yeniden açar. “Hat açık. İstersek konuşabiliriz.”
A) Teması sürdür. →K54
B) Bağlantıyı reddet. →K52
**K52 — Halkın Öfkesi**
Tarık bu kez herkesin önünde konuşur. “Bu kararları neden hep sen veriyorsun?” Oda sessizleşir.
A) Sakin kalıp cevap ver. `☺+1`→K55
B) Sert karşılık ver. `☺-1`→K53
**K53 — Emine Teyze'nin Tarifi**
Emine Teyze elde kalanlarla ne olduğu pek anlaşılmayan bir yemek yapar. Kaşığı sana uzatır.
A) Tadına bak. *(etki yok)*→K56
B) Bu kez pas geç. *(etki yok)*→K54
**K54 — Duman Kararı ⚑**
Sabiha uzakta yükselen ince bir duman sütununu gösterir. “Ateşse insan vardır. Tuzaksa da bizi bekliyor olabilir.”
A) Dumanın kaynağını araştır. *(duman_arastir=evet)*→K57
B) O bölgeden uzak dur. *(duman_arastir=hayir)*→K55
**K55 — Ali'nin Doğum Günü**
Ali’nin doğum günü gelir. Büyük bir kutlama yapacak hâliniz yoktur ama herkes günü hatırlar.
A) Küçük de olsa kutla. *(etki yok)*→K58
B) Sade bir tebrikle geç. *(etki yok)*→K56
**K56 — Yabancı Grup (1/2)**
Ömer uzakta ilerleyen bir grup görür. “Bizi fark ettiler mi emin değilim.”
A) Temas kur. *(yabanci_temas=evet)*→K59
B) Uzaktan izle. *(yabanci_temas=hayir)*→K57
**K57 — Yabancı Grup (2/2) Sonuç**
*(Temas kurulduysa)*
Grup yiyecek ve malzeme takası teklif eder. Sabiha malları hızlıca gözden geçirir.
A) Takası kabul et. `🥫+1 ☺+1`→K60
B) Teklifi reddet. `☺-1`→K58

*(Uzaktan izlendiyse — varyant: yabanci_temas=hayir)*
Grup yakınlarda kamp kurar. Ömer, birkaç gün burada kalabileceklerini düşünür.
A) Nöbeti artır. `🏠0`→K60
B) Düzeni değiştirme. *(nöbet zaten sıkıysa etkisiz, gevşekse `🏠-1`)*→K58
**K58 — Fatma'nın Gökkuşağı**
Fatma duvara kocaman bir gökkuşağı çizer. Gri betonun ortasında fazlasıyla canlı durur.
A) Bir süre yanında dur. *(etki yok)*→K61
B) Yoluna devam et. *(etki yok)*→K59
**K59 — Duman Kararı Sonucu ⚑**
*(Araştırıldıysa)*
Dumanın yanında küçük bir grup bulunur. Sığınağa katılmak istediklerini söylerler.
A) İçeri al. `🥫-1 ☺+1`→K62
B) Geri çevir. `☺-1`→K60

*(Girilmediyse — varyant: duman_arastir=hayir)*
Sonradan gelen haber, dumanın bir tuzağın parçası olduğunu doğrular. Uzak durmak doğru karar olmuştur. →`☺+1`→K62
**K60 — YIKICI 💀🩺**
Zeynep su kabını ışığa tutar. “Kokusu normal değil. İçmeden önce test etmemiz gerek.”
A) Suyu test et. `🥫-1`→K63
B) Beklemeden kullan. *(🩺≤3 ise `🩺=0`→SALTANAT SONU; 🩺>3 ise `🩺0`)*→K61
---

### BÖLÜM III (K61-K100)

**K61 — Necati'nin Kumarı**
Necati zar oyununda üst üste kaybeder. Masadakiler onun söylenmesine gülmeye başlar.
A) Sen de gül. *(etki yok)*→K64
B) Ciddiyetini koru. *(etki yok)*→K62
**K62 — LİDER RİSKİ 💀👑**
Mete haritayı önüne koyar. “İhtiyacımız olan malzeme burada olabilir. Yol kötü, bölge daha da kötü.”
A) Keşfe kendin çık. *(👑<5 ise ANİ ÖLÜM; değilse `👑-2`)*→K65
B) Bir ekip gönder. `👑0 🥫-1`→K63
**K63 — Vertak Dosyası: Faz 4**
İsmet eski Vertak kayıtlarından “Faz 4” başlıklı bir dosya çıkarır. İçeriği, bildiklerinizi biraz daha karanlık bir yere bağlar. *(pharma_arastirma+1)*
A) Dosyayı herkesle paylaş. `☺-1`→K66
B) Şimdilik arşivde tut. *(etki yok)*→K64
**K64 — Balık Tutma**
Yusuf derede balık tutmaya uğraşır. Oltası sürekli bir yerlere takılır ama vazgeçmez.
A) Yardım et. *(etki yok)*→K65
B) Kenardan izle. *(etki yok)*→K65
**K65 — Güneş Paneli Projesi (1/3)**
Kemal çatının en güneş alan bölümünü işaretler. “Yeterli malzeme bulursak kendi elektriğimizi üretebiliriz.”
A) Projeyi hemen başlat. *(proje=baslatildi)*→K68
B) Şimdilik ertele. *(proje=ertelendi)*→K66
**K66 — Güneş Paneli (2/3)**
Kemal parça listesini uzatır. “Elimizdekiler yetmiyor. Ya komşulardan isteyeceğiz ya da başka yerlerden söküp kullanacağız.”
A) Başka bir sığınaktan yardım iste. *(ittifak_baslangic=evet)* `🥫-1`→K69
B) Kendi kaynaklarımızla devam et. `🏠-1`→K67
**K67 — Güneş Paneli (3/3) Sonuç**
*(Proje zamanında başlatıldıysa)*
Panel sonunda tam kapasite çalışır. Sığınakta ilk kez kesintisiz bir elektrik kaynağı vardır.
A) Kemal’in emeğini takdir et. `🏠+2 🥫-1`→K70
B) Vakit kaybetmeden sıradaki işe geç. `🏠+2 🥫-1`→K70

*(Proje ertelenip kendi kaynaklarıyla yapıldıysa — varyant: proje=ertelendi)*
Eldeki parçalarla kurulan panel kusursuz değildir ama çalışır. Gecikmenin bedeli, daha düşük kapasitedir.
A) Bu hâliyle yeterli say. `🏠+1 🥫-1`→K70
B) İleride genişletmek üzere kayda geçir. `🏠+1 🥫-1`→K70
**K68 — Sibel'in Geçmişi**
Panel konuşulurken Sibel, elektriğin ona eski bir şeyi hatırlattığını söyler. Meğer salgından önce piyanistmiş.
A) Bir gün çalmasını iste. *(etki yok)*→K71
B) Konuyu uzatma. *(etki yok)*→K67
**K69 — İttifak Teklifi ⚑**
Komşu sığınaktan resmi bir teklif gelir: kaynak ve haber paylaşımı karşılığında karşılıklı destek.
A) İttifakı kabul et. *(ittifak=evet)*→K72
B) Teklifi reddet. *(ittifak=hayir)*→K70
**K70 — Beklenmedik Barış**
Tarık ile Rıza günlerdir ilk kez aynı masada kavga etmeden oturur. Kimse bunun nasıl olduğunu tam anlayamaz.
A) Barışmalarını kutla. *(etki yok)*→K73
B) Üzerinde durma. *(etki yok)*→K71
**K71 — YIKICI 💀🥫**
Sabiha büyük miktarda erzak getirebilecek bir takas fırsatı bulur. Güvenli seçenek az kazandırır; diğerinde kayıp ihtimali çok daha büyüktür.
A) Güvenli takası seç. `🥫-1 🩺+1`→K74
B) Büyük riski al. *(🥫≤3 ise `🥫=0`→SALTANAT SONU; 🥫>3 ise `🥫+3`)*→K72
**K72 — İsim Koyma**
Gül bebeğine isim koyacağı gün herkesi yanına çağırır. Sığınakta uzun zamandır böyle bir şey için toplanılmamıştır.
A) Törene katıl. *(etki yok)*→K75
B) Kısa bir tebrikle yetin. *(etki yok)*→K73
**K73 — Konuşan Zombi**
Ömer çitin yanında yine aynı boğuk sesi duyar. Bu kez kelimeler daha nettir: “Biz de... insandık.”
A) Dinlemeye devam et. `☺-1` *(zombi_ikinci_temas=evet)*→K76
B) Çitten uzaklaş. *(etki yok)*→K74
**K74 — Kayıp Defter**
Aziz telaşla tohum defterini arar. Yıllardır tuttuğu bütün ekim notları o defterdedir.
A) Aramasına yardım et. *(etki yok)*→K77
B) Kendi işine dön. *(etki yok)*→K75
**K75 — İttifak Sonucu ⚑**
*(Kabul edildiyse)*
İttifakın şartları zamanla tek taraflı hâle gelir. Karşı taraf daha çok isterken verdiği destek azalır.
A) Şartlara itiraz et. `☺-1`→K78
B) Anlaşmayı bozmamak için boyun eğ. `🥫-2 ☺-1`→K76

*(Reddedildiyse — varyant: ittifak=hayir)*
Komşu sığınaktan gelen haberler, teklifin göründüğü kadar masum olmadığını doğrular. Reddetmek sizi bir yükten kurtarmıştır. →`☺+1`→K77
**K76 — LİDER RİSKİ 💀👑**
Ömer sabaha karşı seni kenara çeker. “Birisi sana ulaşmaya çalıştı. Tesadüf değildi.”
A) Şüphelinin peşine düş. *(👑<5 ise ANİ ÖLÜM; değilse `👑-1 ☺-1`)*→K79
B) Şimdilik üstünü kapat. `👑0`→K77
**K77 — Ali'nin İlk Nöbeti**
Ali ilk kez gerçek nöbete çıkmak istediğini söyler. Çocukluğundan kalan hâliyle ona bakmak artık giderek zorlaşmaktadır.
A) Nöbete katılmasına izin ver. *(etki yok)*→K80
B) Biraz daha beklemesini söyle. *(etki yok)*→K78
**K78 — Kış Hazırlığı (1/2)**
Kemal kış için iki ısınma planı çıkarır. Odun daha güvenilir ama dumanlıdır; elektrik daha temiz ama sisteme yük bindirir.
A) Odunla ısın. *(kis_hazirlik=odun)* `🥫-1`→K81
B) Elektrikli sistemi kullan. *(kis_hazirlik=elektrik)* `🏠-1`→K79
**K79 — Kış Hazırlığı (2/2) Sonuç**
*(Odun seçildiyse)* Kışın ilk haftasında sığınak sıcak kalır ama baca kurum bağlar, hava ağırlaşır.
A) Bacayı düzenli temizlet. `🏠+1 🩺0`→K80
B) Sezonu böyle çıkarmaya çalış. `🏠+1 🩺-1`→K80

*(Elektrik seçildiyse — varyant: kis_hazirlik=elektrik)* Elektrikli sistem çalışır ama yük altında sık sık kararsızlaşır. Buna karşılık içerideki hava temizdir.
A) Kemal’e sürekli kontrol ettir. `🏠0 🩺+1`→K80
B) Arızalar çıkana kadar müdahale etme. `🏠-1 🩺+1`→K80
**K80 — Semra'nın Geleneği**
Semra’nın küçük konserleri artık sığınağın alışkanlıklarından biri olmuştur. O akşam yine gitarını çıkarır.
A) Dinlemeye git. *(etki yok)*→K84
B) Bu kez çalışmaya devam et. *(etki yok)*→K81
**K81 — Vertak'ın Planı**
*(pharma_arastirma≥2 ise İsmet, Vertak'ın asıl planını çözer: sığınakları toplamak; düşükse yalnızca dağınık, tedirgin edici ipuçları bulur.)* İsmet bulduklarını önüne dizer. Ne kadarının halka açıklanacağına karar vermek gerekir.
A) Bildiklerini yayımla. `☺-1`→K83
B) Bilgiyi kadroyla sınırla. *(etki yok)*→K82
**K82 — YIKICI 💀☺**
“Vertak’a katılalım” diyenlerin sayısı artar. Tartışma artık birkaç kişinin homurdanmasından çıkıp açık bir bölünmeye dönüşmüştür.
A) Gitmek isteyenleri serbest bırak. `☺+1`→K84
B) Kimsenin ayrılmasına izin verme. *(☺≤3 ise `☺=0`→SALTANAT SONU; ☺>3 ise `☺-1`)*→K83
**K83 — Emine Teyze'nin Son Günü**
Emine Teyze o gün alışılmadık derecede sakindir. Bahçesinin yanında oturup eski günlerden konuşur; bu, onun son iyi günüdür. *(Not: bu günden sonra nüfus 20'ye düşer.)*
A) Yanında otur. `☺+1`→K87
B) Dinlenmesi için yalnız bırak. *(etki yok)*→K84
**K84 — Zeynep'in Tükenmişliği ⚑**
Zeynep’in elleri titremeye başlamıştır. Günlerdir herkese bakmış, kendisi neredeyse hiç uyumamıştır.
A) Dinlenmesini zorunlu tut. *(zeynep_zorla_dinlendirildi=evet)*→K86
B) Kararı ona bırak. →K85
**K85 — Yeni Oyun**
Cem ile Yusuf yeni bir masa oyunu uydurmuştur. Kuralları her tur değişiyor ama kimsenin umurunda değildir.
A) Oyuna katıl. *(etki yok)*→K87
B) Kenardan izle. *(etki yok)*→K86
**K86 — Mülteci Grubu (1/2)**
Sınırda yorgun ve bitkin bir mülteci grubu belirir. Yanlarında çocuklar da vardır.
A) Grubu içeri al. *(multeci=kabul)*→K89
B) Sığınaktan uzaklaştır. *(multeci=ret)*→K87
**K87 — Mülteci Grubu (2/2) Sonuç**
*(Kabul edildiyse)*
İçeri alınanlardan birinde kısa süre sonra hastalık belirtisi görülür.
A) Hemen karantinaya al. `🩺+1 ☺-1`→K90
B) Belirti ağırlaşana kadar bekle. `🩺-1`→K88

*(Reddedildiyse — varyant: multeci=ret)*
Grup çevreden ayrılmaz. Yakınlarda dolaşmaları içerideki huzursuzluğu artırır.
A) Bölgeden uzaklaştır. `☺-1`→K90
B) Görmezden gel. *(🏠≤4 ise `🏠-1`; 🏠>4 ise etkisiz)*→K88
**K88 — Zeynep'in Tükenmişliği Sonucu ⚑**
*(Zorla dinlendirildiyse)* Zeynep birkaç gün sonra belirgin biçimde toparlanmış döner. Mülteci meselesine yeniden el atabilecek durumdadır.
A) Tam görevine dönsün. `☺+1`→K90
B) İş yükünü kademeli artır. `☺+1 🩺+1`→K90

*(Kendi bilsin dendiyse — varyant: zeynep_zorla_dinlendirildi≠evet)* Zeynep tam mülteci krizi sırasında hastalanır. Revirin başında artık kimin duracağı belirsizdir.
A) Geçici birini görevlendir. `🩺-2`→K89
B) Zeynep’in işi sürdürmesine izin ver. `🩺-2 👑-1`→K89
**K89 — Yıl Dönümü**
Sığınağın kuruluşunun üzerinden bir yıl geçmiştir. Kimse bunu tam olarak kutlama saymasa da tarih herkesin aklındadır.
A) Küçük bir yıl dönümü düzenle. *(etki yok)*→K91
B) Günü sıradan geçir. *(etki yok)*→K90
**K90 — LİDER RİSKİ 💀👑**
Mustafa dışarıdan gelen uğultuyu dinler. “Şimdiye kadarki en büyük sürü bu. Hatları ben tutarım ama senin kararın lazım.”
A) Cepheye çıkıp savunmayı yönet. *(👑<5 ise ANİ ÖLÜM; değilse `👑-3 🏠+2`)*→K93
B) Komutayı Mustafa’ya bırak. `👑0 🏠+1 ☺-1`→K91
**K91 — Ali'nin İlk Görevi**
Ali artık çocuk değildir. İlk kez uzmanlık isteyen gerçek bir görev için adını yazdırır.
A) Görevi ona ver. *(ali_deneyim+1)*→K94
B) Bir süre daha beklet. *(etki yok)*→K92
**K92 — YIKICI 💀🏠**
Kemal yapısal raporu masaya bırakır. “Bu bina bizi daha ne kadar taşır, emin değilim. Taşınmak pahalı; kalmak da riskli.”
A) Yeni bir yere taşın. `🥫-2 🏠+1`→K94
B) Mevcut sığınakta kal. *(🏠≤3 ise `🏠=0`→SALTANAT SONU; 🏠>3 ise `🏠-1`)*→K93
**K93 — Vertak'taki Çatlak**
*(pharma_arastirma≥3 ise İsmet, Vertak'ın içeriden bölündüğünü öğrenir — bir hizip barış istiyor; düşükse yalnızca çelişkili söylentiler duyar.)* İsmet’in getirdiği bilgiler ilk kez Vertak’ın tek parça olmadığını düşündürür.
A) Barış isteyenlerle temas ara. `☺+1`→K95
B) Kimseye güvenme. *(etki yok)*→K94
**K94 — Eski Kaset**
İsmet eski bir kaset bulur. Cızırtıların arasından salgın öncesi bir şarkı ve insanların sıradan konuşmaları duyulur.
A) Hep birlikte dinleyin. `☺+1`→K97
B) İşine dön. *(etki yok)*→K95
**K95 — Ateşkes ya da Saldırı (1/2)**
Ömer, konuşan enfektelerden gelen ilk açık temas teklifini iletir. Bu kez çitin ötesinde bekleyip cevap vermenizi istemektedirler.
A) Ateşkes görüşmesi yap. *(ateskes=evet)*→K97
B) Önce saldır. *(ateskes=hayir)*→K96
**K96 — Ateşkes Sonucu (2/2)**
*(Ateşkes denendiyse)* Görüşmeler beklenmedik biçimde sonuç verir. Sınırın öte yanı ilk kez yalnızca bir tehdit değil, konuşulabilen bir komşu gibi görünür.
A) Anlaşmayı törenle duyur. `☺+2 🩺+1`→K100
B) Gösterişsiz biçimde yürürlüğe koy. `☺+2 🩺+1`→K100

*(Saldırıldıysa — varyant: ateskes=hayir)* Çatışma büyür ve iki tarafta da kayıplar olur.
A) Kayıplar için anma düzenle. `☺-2 🩺-1`→K97
B) Savunmayı toparlayıp devam et. `☺-2 🩺-1`→K97
**K97 — Sessiz Akşam**
O akşam sığınakta alışılmadık bir sessizlik vardır. Herkes bir şekilde hâlâ burada olduğunun farkındadır.
A) Bir süre oturup olanları düşün. *(etki yok)*→K100
B) Uyumaya git. *(etki yok)*→K98
**K98 — İlk Adımlar**
Gül’ün çocuğu ilk kez kendi başına birkaç adım atar. Yakındakiler istemsizce alkışlar.
A) Onlarla birlikte kutla. *(etki yok)*→K102
B) İşine devam et. *(etki yok)*→K99
**K99 — Resim Dersi**
Fatma yeni gelen çocuklara duvarın kenarında resim yaptırır. Birkaç dakikalığına sığınak okul gibi görünür.
A) Derse katıl. *(etki yok)*→K101
B) Kenardan izle. *(etki yok)*→K100
**K100 — Dönüm Noktası (Sezon 1 Kapanışı)**
İlk büyük dönemin sonunda sığınak hâlâ ayaktadır. Buraya kadar gelen yol; verdiğin kararlar, kurduğun ilişkiler ve geride bıraktığın sonuçlarla şekillenmiştir. Bu bir final değildir.
A) Devam et. →K101
B) Yeni döneme geç. →K101
---

### BÖLÜM IV (K101-K150)

**K101 — Radyo Tamiri**
Yeni dönem sakin bir sabahla açılır. Necati eski bir radyoyu söküp önüne dizer. “Belki bundan hâlâ ses alırız.”
A) Tamire yardım et. *(etki yok)*→K104
B) Kenardan izle. *(etki yok)*→K102
**K102 — Vertak Baskısı ⚑**
İsmet kulaklığını çıkarır. “Vertak sinyalleri son günlerde belirgin biçimde arttı. Bizi dinliyor olabilirler.”
A) Yayını karart. *(vertak_karartma=evet)*→K104
B) Frekansı açık bırak. *(vertak_karartma=hayir)*→K103
**K103 — Duvar Resimleri**
Fatma boş kalan duvara yeni resimler çizmeye başlar. Çocuklar da etrafına toplanır.
A) Sen de bir şey ekle. *(etki yok)*→K106
B) Bir süre izle. *(etki yok)*→K104
**K104 — Meydan Okuma (1/2)**
Tarık bu kez kapalı kapılar ardında değil, herkesin önünde konuşur. “Liderliği oylayalım. Kimin ne düşündüğü ortaya çıksın.”
A) Oylamaya izin ver. *(meydan_okuma=evet)*→K105
B) Toplantıyı dağıt. *(gizli_gerginlik=evet)*→K105
**K105 — Meydan Okuma (2/2) Sonuç**
*(İzin verildiyse)*
Toplantı saatler sürer. Herkes ilk kez açıkça söz alabilmektedir.
A) Kendi kararlarını açıkça savun. `☺+2`→K108
B) Dinlemeyi tercih et. `☺+1`→K106

*(Bastırıldıysa — varyant: gizli_gerginlik=evet)*
Tarık yasağa rağmen gizlice destek toplamaya başlar. Ömer bunu kısa sürede fark eder.
A) Ömer’e takip ettir. `☺-1`→K108
B) Şimdilik görmezden gel. *(ayaklanma_riski=evet)*→K106
**K106 — Vertak Baskısı Sonucu ⚑**
*(Karartıldıysa)*
Sinyal kesilir. İsmet yine de rahat değildir. “Kayboldular mı, yoksa sadece sustular mı bilmiyorum.”
A) İsmet’e güvenip konuyu kapat. `☺+1`→K108
B) Frekansı gizlice izlemeyi sürdür. `👑-1`→K108

*(Açık bırakıldıysa — varyant: vertak_karartma=hayir)*
İsmet birkaç gün sonra kötü haberi verir: Vertak konumunuzu belirlemiştir. *(vertak_yolda=evet)*
A) Savunmayı hızla güçlendir. `🏠+1 ☺-1`→K107
B) Panik yaratmadan bekle. *(etki yok)*→K107
**K107 — Sibel'in Konserleri**
Sibel’in piyano konserleri artık düzenli hâle gelmiştir. Dışarıdaki belirsizliğe rağmen o akşam da birkaç kişi sandalyeleri dizer.
A) Konseri dinle. *(etki yok)*→K110
B) İşine dön. *(etki yok)*→K108
**K108 — Çırak Nöbetçi**
Ali artık genç bir yetişkindir ve ilk kez resmen “çırak nöbetçi” sayılır. Ömer ona gerçek bir vardiya çizelgesi verir.
A) Ali’yi tebrik et. *(etki yok)*→K111
B) Bunu görevin doğal parçası say. *(etki yok)*→K109
**K109 — Çitteki Düzenli Ziyaretçi**
Ömer aynı enfektenin artık düzenli aralıklarla çite geldiğini söyler. “Belli ki bizimle konuşmak istiyor.”
A) Ona bir isim verip teması kişiselleştir. *(zombi_isimlendirildi=evet)*→K112
B) Mesafeyi koru. *(etki yok)*→K110
**K110 — Vertak Keşif Ekibi (1/2)**
*(vertak_yolda=evet ise)* Yakında bir araç durur. İçindekiler silahsız görünse de Vertak işareti taşımaktadır.
A) Kapıyı kontrollü aç. →K112
B) Herkesi silah başına geçir. →K111

*(değilse — varyant: vertak_yolda≠evet)* Sabiha haritada daha önce taranmamış bir bölgeyi gösterir. “Malzeme çıkabilir. Yol da temiz görünüyor.”
A) Bölgeyi araştır. →K112
B) Bu kez çıkma. →K111
**K111 — Vertak Keşif Ekibi (2/2) Sonuç**
*(Vertak'sa)*
Vertak temsilcisi ayrılırken tek bir cümle bırakır: “Gözlemleneceksiniz.” *(vertak_gozlem=evet)*
A) Tehdidi ciddiye alıp güvenliği artır. `🏠+1 ☺-1`→K115
B) Gözdağı sayıp rutine dön. *(etki yok)*→K115

*(Diğerse — varyant: vertak_yolda≠evet)*
Keşif ekibi kilitli eski bir depo bulur. İçeriden işe yarar miktarda erzak çıkar.
A) Erzağı hemen dağıt. `🥫+2 ☺+1`→K112
B) İhtiyaç için depola. `🥫+2`→K112
**K112 — Aziz'in Yeni Tarifi**
Aziz yeni hasattan farklı bir yemek dener. Tadı konusunda kendisi bile emin değildir.
A) İlk lokmayı sen al. *(etki yok)*→K116
B) Başkalarının denemesini bekle. *(etki yok)*→K113
**K113 — YIKICI 💀🩺**
Revir kısa sürede mide bulantısı ve ateş şikâyetleriyle dolar. Zeynep ortak bir gıda zehirlenmesinden şüphelenir.
A) Yiyecekleri test ettir. `🥫-1 🩺+1`→K116
B) Kendiliğinden geçmesini bekle. *(🩺≤3 ise `🩺=0`→SALTANAT SONU; 🩺>3 ise kendiliğinden atlatılır, etkisiz)*→K114
**K114 — Kalıcı Oyun**
Cem ile Yusuf’un uydurduğu oyun artık sığınağın eski alışkanlıklarından biri olmuştur. Yeni gelenler bile kuralları bilir.
A) Bir tur oyna. *(etki yok)*→K117
B) Kenardan izle. *(etki yok)*→K115
**K115 — Yeniden Yapılanma (1/2)**
Kemal yeni bir yapısal rapor getirir. “Yama yaparak gidiyoruz. İstersek bu kez kökten çözebiliriz.”
A) Kapsamlı onarım başlat. *(onarim=tam)*→K118
B) Yalnızca zorunlu yerleri düzelt. *(onarim=minimal, onarim_gecici=evet)*→K116
**K116 — Yeniden Yapılanma (2/2) Sonuç**
*(Tam proje seçildiyse)* Aylar süren çalışma sonunda sığınak baştan aşağı güçlendirilir. Kemal ilk kez “Bu bina artık uzun süre gider” der.
A) Ekiple birlikte kutla. `🏠+3 👑-1`→K118
B) Dinlenmeden sıradaki işe geç. `🏠+3 👑-1`→K118

*(Minimal seçildiyse — varyant: onarim_gecici=evet)* Hızlı yamalar işe yarar. Kemal yine de bazı bölgelerin ileride yeniden sorun çıkaracağını not eder.
A) Sorunlu noktaları takip listesine al. `🏠+1`→K118
B) Şimdilik yeterli say. `🏠+1`→K118
**K117 — Küçük Pazar**
Sığınağın ortak alanında küçük bir takas pazarı kurulmaya başlanır. İnsanlar ihtiyaç fazlasını birbirleriyle değiştirir.
A) Pazara katıl. *(etki yok)*→K120
B) Uzaktan gözlemle. *(etki yok)*→K118
**K118 — LİDER RİSKİ 💀👑**
*(ayaklanma_riski=evet ise)* Gizlice büyüyen huzursuzluk sonunda patlar. Kalabalığın içinde sana doğru ilerleyenler vardır.
A) Karşılarına çık. *(👑<5 ise ANİ ÖLÜM; değilse `👑-3 ☺+2`)*→K120
B) Güvenli bir yere çekil. `👑0 ☺-2`→K119
**K119 — Paylaşma İhtiyacı**
Olayların ardından sığınak sessizleşir. O akşam birkaç kişi konuşmak için yanına gelir; kimse ne diyeceğini tam bilemez.
A) Onlarla otur. *(etki yok)*→K122
B) Yalnız kal. *(etki yok)*→K120
**K120 — Eski Vertak Çalışanı**
Kapıda yaralı bir kadın belirir. Üzerindeki eski kimlik, bir dönem Vertak için çalıştığını gösterir.
A) İçeri al. *(eski_vertak_calisan=evet)*→K124
B) Sığınağa sokma. →K121
**K121 — Sorgu Sonucu**
*(Alındıysa)*
İsmet kadını uzun süre sorgular. Hikâyesinde açık bir çelişki bulamaz ama güvenmek için de erken olduğunu söyler.
A) Söylediklerine güven. `☺-1`→K124
B) Ayrıntılı sorgulamayı sürdür. *(icerden_bilgi=evet, pharma_arastirma+2)* `☺-1`→K124

*(Uzak tutulduysa — varyant: eski_vertak_calisan≠evet)*
Kadın giderken kapının yakınına katlanmış bir not bırakır. İçinde Vertak hakkında parçalı bilgiler vardır.
A) Notu hemen incele. *(etki yok)*→K122
B) Arşive kaldırıp sonra bak. *(etki yok)*→K122
**K122 — Terfi**
Ali artık tam yetkili bir nöbetçidir. Ömer vardiya çizelgesinde adının yanındaki “çırak” notunu siler.
A) Ali’yi tebrik et. *(etki yok)*→K124
B) Tören yapmadan göreve devam et. *(etki yok)*→K123
**K123 — Zeynep'in Halefi ⚑**
Zeynep revirdeki defterleri gösterir. “Bir gün burada olmayacağım. Birini şimdiden yetiştirmeliyiz.”
A) Atilla’yı yetiştir. *(halef=atilla)*→K125
B) Sibel’i yetiştir. *(halef=sibel)*→K124
**K124 — Sığınak Kütüphanesi**
Yıllar içinde biriken kitaplar, notlar ve eski dergiler için ayrı bir köşe oluşmuştur. İnsanlar buraya artık “kütüphane” demektedir.
A) Arşive bir şey ekle. *(etki yok)*→K127
B) Olduğu gibi bırak. *(etki yok)*→K125
**K125 — Konuşan Enfekte (1/2)**
Ömer, konuşan enfektenin son günlerde hep aynı yöne işaret ettiğini fark eder. “Bizi bir yere götürmeye çalışıyor olabilir.”
A) İşaret ettiği yönü takip et. *(zombi_takip=evet)*→K128
B) Bu kez peşinden gitme. *(etki yok)*→K126
**K126 — Konuşan Enfekte (2/2) Sonuç**
*(Takip edildiyse)*
İzler eski bir Vertak tesisine çıkar. Dışarıdan terk edilmiş görünür. *(vertak_tesis_bulundu=evet, pharma_arastirma+2)*
A) Tesise gir. `☺-1`→K129
B) Konumu işaretleyip geri dön. *(etki yok)*→K129

*(Görmezden gelindiyse — varyant: zombi_takip≠evet)*
Konuşan enfekte birkaç gün sonra gelmeyi bırakır. Nereye gittiğini kimse öğrenemez.
A) Kaydını tut. *(etki yok)*→K127
B) Konuyu kapat. *(etki yok)*→K127
**K127 — Zeynep'in Halefi Sonucu ⚑**
Halef eğitimi tamamlanır. Revirde artık Zeynep dışında gerektiğinde sorumluluk alabilecek ikinci bir sağlıkçı vardır. *(ikinci_saglikci=evet)*
A) Zeynep’le birlikte çalışsın. `🩺+1`→K131
B) Kendi vardiyasını yönetsin. `🩺+1 👑-1`→K131
**K128 — Dışarıdan Katılım**
Sibel’in konserlerine çevredeki birkaç kişi de gelmeye başlar. İlk kez kapının dışından gelenler yalnızca ticaret veya yardım için değildir.
A) Kalabalığa katıl. *(etki yok)*→K132
B) Kenardan izle. *(etki yok)*→K129
**K129 — YIKICI 💀☺ — Vertak'ın Gözetimi**
*(vertak_gozlem=evet ise)* Zamanla Vertak’ın “gözlem” dediği şeyin sürekli takip olduğu anlaşılır. İnsanlar izlendiğini bildikçe huzursuzlanır.
A) Durumu sakin biçimde açıkla. `☺+1`→K133
B) Tehdidi olduğu gibi anlat. *(☺≤3 ise `☺=0`→SALTANAT SONU; ☺>3 ise `☺-1`)*→K130
**K130 — İlk Kelimeler**
Gül’ün çocuğu ilk kelimelerini söylemeye başlar. Söylediği şeyin ne olduğu konusunda herkes farklı bir şey duyar.
A) Onlarla birlikte kutla. *(etki yok)*→K133
B) İşine devam et. *(etki yok)*→K131
**K131 — Büyük Sürü Krizi (1/3) ⚑**
Mustafa haritanın üzerine geniş bir yay çizer. “Şimdiye kadar gördüğümüz hiçbir sürü buna benzemiyordu. Doğrudan buraya geliyor.”
A) Herkesi savunmaya seferber et. *(kriz=seferberlik)*→K134
B) Tahliyeyi başlat. *(kriz=tahliye)*→K132
**K132 — Büyük Sürü Krizi (2/3)**
Sürü artık çıplak gözle seçilebilecek kadar yakındır. Mustafa ile Mete savunma noktalarına geçer.
A) Cepheye çıkıp komutayı al. *(👑<5 ise ANİ ÖLÜM; değilse `👑-3`)*→K135
B) Komutayı geriden yürüt. `👑0`→K133
**K133 — Büyük Sürü Krizi (3/3) Sonuç**
*(Seferberlik + cepheden yönetildiyse)* Savunma hattı kayıp verir ama dayanır. Sürü geri çekilirken içeride ilk kez zafer sesleri yükselir.
A) Önce kayıpları an. `🏠-1 ☺+3`→K136
B) Zaferi kutla. `🏠-1 ☺+3`→K136

*(Seferberlik + geriden yönetildiyse — varyant: kriz=seferberlik)* Sürü durdurulur ama emirler birbirine girer; gereğinden fazla hasar oluşur.
A) Komuta zincirini sorgula. `🏠-2 ☺+1`→K136
B) Kriz geçtiği için konuyu kapat. `🏠-2 ☺+1`→K136

*(Tahliye + sürdürüldüyse — varyant: kriz=tahliye)* Tahliye tamamlanır. Herkes çıkamaz; geride bırakılanların adı uzun süre konuşulur.
A) Geride kalanları an. `🏠-2 🥫-1`→K136
B) Hayatta kalanlara odaklan. `🏠-2 🥫-1`→K136

*(Tahliye + son anda vazgeçildiyse — varyant: kriz=tahliye)* Tahliye emri geri çekilince insanlar neye güveneceğini şaşırır.
A) Kararın sorumluluğunu üstlen. `☺-1`→K136
B) Konuyu açıklamadan geç. `☺-1`→K136
**K134 — Kriz Sonrası Sükûnet**
Büyük krizden sonra ilk kez alarm çalmadan bir gün geçer. İnsanlar ne yapacağını şaşırmış gibidir.
A) Bir süre kalabalığın içinde kal. *(etki yok)*→K137
B) Yalnız kal. *(etki yok)*→K135
**K135 — Vertak'la Yüzleşme (1/2)**
*(pharma_arastirma yüksekse)* İsmet topladığı belgeleri masaya dizer. “Artık Vertak’ın ne yaptığını biliyoruz. Onlar da bizim bildiğimizi biliyor olabilir.”
A) Doğrudan yüzleş. →K139
B) Temastan kaçın. →K136

*(Düşükse — varyant: pharma_arastirma<3)* Vertak hakkında hâlâ yalnızca parçalı bilgiler vardır. Her yeni ipucu bir başka soruyu açmaktadır.
A) Araştırmayı sürdür. →K139
B) Bu dosyayı kapat. →K136
**K136 — Vertak'la Yüzleşme (2/2) Sonuç**
*(Yüzleşme/devam kabul edildiyse)* Vertak, koruması altına girmenizi teklif eder. Daha güvenli bir düzen vaat eder; karşılığında bağımsızlığınızdan vazgeçmenizi ister.
A) Koruma teklifini kabul et. `🏠+1 ☺-1`→K139
B) Bağımsız kal. `☺+1`→K139

*(Kaçınma/unutma seçildiyse — varyant)* Vertak’la açık bir anlaşma kurulmaz. Sığınak bağımsız kalır ama tehdidin ne kadar yakında olduğu belirsizdir. *(pharma_arastirma+1)*
A) Araştırmayı gizlice sürdür. `☺-1`→K139
B) Konuyu tamamen kapat. *(etki yok)*→K139
**K137 — Büyük Toplantı**
Sığınakta uzun zamandır ilk kez herkesin katıldığı geniş bir toplantı düzenlenir. Sorunlardan çok geleceğin nasıl yönetileceği konuşulur.
A) Söz al. *(etki yok)*→K140
B) Bu kez dinle. *(etki yok)*→K138
**K138 — Zombi Anlaşması**
*(ateskes=evet ise)* Ömer, konuşan enfektelerden yeni bir teklif getirir. Ateşkesin ardından iki taraf arasında açık bir sınır belirlemek istemektedirler.
A) Sınır anlaşmasını kabul et. `☺+1`→K141
B) Ateşkesi koruyup mesafeyi sürdür. *(etki yok)*→K139
**K139 — Genç Uzman**
Ali artık sığınağın en genç uzman üyelerinden biridir. İnsanlar karar verirken onun fikrini de sormaya başlamıştır.
A) Başardıklarını açıkça takdir et. *(etki yok)*→K143
B) Onu diğer uzmanlardan farklı görme. *(etki yok)*→K140
**K140 — LİDER RİSKİ 💀👑 (Final Tehlike)**
Mustafa ile Mete birlikte gelir. İkisinin de yüzündeki ifade yeterince açıktır: büyük bir tehdit daha yaklaşıyordur.
A) Ön hatta çık. *(👑<5 ise ANİ ÖLÜM; değilse `👑-4`)*→K142
B) Savunmayı geriden yönet. `👑0 ☺-1`→K141
**K141 — Fırtına Dinince**
Tehlike geçtikten sonra sığınak yine ayaktadır. Birkaç saatliğine kimse yeni bir krizden söz etmez.
A) Kendine biraz zaman ayır. *(etki yok)*→K143
B) Hemen işlere dön. *(etki yok)*→K142
**K142 — Tarih Yazımı**
İsmet yıllardır tuttuğu notları düzenlemeye başlar. “Bunları birileri okumalı. Yoksa burada ne yaşandığını kimse bilmeyecek.”
A) Kendi anılarını da anlat. `☺+1`→K144
B) Kaydı ona bırak. *(etki yok)*→K143
**K143 — Son Sakin Akşam**
O akşam hayatta kalan kadronun tamamı aynı masadadır. Böyle anların ne kadar seyrek olduğunu herkes bilir.
A) Hepsine teşekkür et. *(etki yok)*→K146
B) Sessizce onlarla otur. *(etki yok)*→K144
**K144 — Emine Teyze'nin Bahçesi**
Emine Teyze’nin yıllar önce başlattığı bahçe yeniden çiçek açar. Aziz onun bıraktığı düzeni sürdürmektedir.
A) Bahçede biraz kal. *(etki yok)*→K147
B) Yoluna devam et. *(etki yok)*→K145
**K145 — Eski Dostlar**
Necati eski dostlarından söz eder. İsimlerin çoğunu artık yalnızca o hatırlamaktadır.
A) Hikâyelerini dinle. *(etki yok)*→K149
B) Konuyu uzatma. *(etki yok)*→K146
**K146 — Yeni Tarif**
Aziz yeni hasattan başka bir tarif denemektedir. Bu kez senden de fikir ister.
A) Yardım et. *(etki yok)*→K148
B) Onu kendi hâline bırak. *(etki yok)*→K147
**K147 — İstikrar**
Sığınağın nüfusu uzun süredir ilk kez büyük dalgalanmalar yaşamadan sabit kalır. Bu, eskiden sıradan sayılacak kadar basit bir başarıdır.
A) Bu istikrarın değerini vurgula. *(etki yok)*→K151
B) Günlük hayatın parçası say. *(etki yok)*→K148
**K148 — Ticaret Ağı**
Sabiha artık yalnızca yakın çevreyle değil, birkaç farklı toplulukla düzenli takas yapmaktadır.
A) Ticaret ağını destekle. `🥫+1`→K152
B) Büyümeyi sınırlı tut. *(etki yok)*→K149
**K149 — Arşiv Kaydı**
İsmet arşive yeni kayıtlar ekler. Boş kalan birkaç sayfayı sana uzatır.
A) Sen de bir kayıt ekle. *(etki yok)*→K150
B) Kaydı ona bırak. *(etki yok)*→K150
**K150 — SEZON 2 DÖNÜM NOKTASI**
İkinci büyük dönemin sonunda sığınak artık yalnızca hayatta kalmaya çalışan bir yer değildir. K1’den beri biriken liderlik değişimleri, ittifaklar, Vertak’la kurulan ilişki ve konuşan enfektelerle verilen kararlar burada birlikte ağırlık kazanır. Bu bir final değildir.
A) Devam et. →K151
B) Yeni döneme geç. →K151
---

### BÖLÜM V (K151-K200)

**K151 — Ali'nin Yolu**
Yeni dönem sakin bir günle açılır. Ali artık hangi alanda ilerlemek istediğine karar verecek yaştadır.
A) Tarımı seçmesini destekle. *(ali_yol=tarim)*→K155
B) Savunmayı seçmesini destekle. *(ali_yol=savunma)*→K152
**K152 — Veli'nin Kıskançlığı**
Veli, ikizinin önünde açılan yolu sessizce izler. Kendi yerinin hâlâ belli olmaması onu rahatsız etmeye başlamıştır.
A) Onunla açıkça konuş. `☺+1`→K155
B) Kendi zamanını bulmasına izin ver. *(etki yok)*→K153
**K153 — Karakol**
Kemal çevrede “Karakol” adıyla bilinen, düzenli ve silahlı bir yerleşimden söz eder. Şimdiye kadar doğrudan temas kurulmamıştır.
A) Temas kurmayı dene. *(karakol_temas=evet)*→K155
B) Mesafeyi koru. *(karakol_temas=hayir)*→K154
**K154 — Resim Dersi**
Fatma çocuklara resim yaptırır. Masaların üzeri boya, kâğıt ve eski dergi parçalarıyla doludur.
A) Derse katıl. *(etki yok)*→K157
B) Kenardan izle. *(etki yok)*→K155
**K155 — Karakol İlişkisi (1/2)**
*(Temas kurulduysa)*
İsmet Karakol’la radyo bağlantısı kurar. Karşı taraf düzenli konuşur ama tonları emre alışkın olduklarını belli eder.
A) İşbirliği öner. →K156
B) Mesafeli bir ilişki kur. →K156

*(Uzak durulduysa — varyant: karakol_temas=hayir)*
Mete devriye sırasında Karakol’dan bir ekiple karşılaşır. İki taraf da birbirini önceden fark etmiştir.
A) Resmî biçimde selam ver. →K156
B) Teması uzatmadan geri çekil. →K156
**K156 — Karakol İlişkisi (2/2) Sonuç**
*(İşbirliği önerildiyse)* Karakol teklifi kabul eder ama erzak ve geçiş hakkı konusunda ağır şartlar öne sürer.
A) Şartları kabul et. `🥫+2 ☺-1`→K158
B) Daha dengeli şartlar için pazarlık et. `🥫+1 ☺+1`→K158

*(Mesafeli kalınıp devam edildiyse — varyant)* Karakol mesafeli tavrınıza karşılık verir; ilişki açık bir çatışmaya dönüşmez ama soğukluk hissedilir.
A) Tavırlarını resmî olarak eleştir. `☺-1`→K158
B) Konuyu büyütme. *(karakol_gerginlik=evet)*→K158

*(Selamlaşıldı ya da çekinildiyse — varyant: karakol_temas=hayir)* Karşılaşma kısa ve olaysız biter. İki taraf da diğerini artık tanımaktadır.
A) Olayı kayda geçir. *(etki yok)*→K158
B) Üzerinde durma. *(etki yok)*→K158
**K157 — Karakol Söylentisi**
Necati Karakol hakkında çevreden duyduğu söylentileri anlatmaya başlar. Hangisinin doğru olduğunu kendisi de bilmiyordur.
A) Bildiklerini dinle. *(etki yok)*→K160
B) Söylentilere kulak asma. *(etki yok)*→K158
**K158 — Örgütlenen Enfekteler ⚑**
Ömer, enfektelerin artık rastgele dolaşmadığını fark eder. Aynı bölgelerde toplanıyor, birbirlerine göre hareket ediyor gibidirler.
A) Davranışlarını yakından izle. *(zombi_izle=evet)*→K160
B) Uzaktan gözlemle yetin. *(zombi_izle=hayir)*→K159
**K159 — Çocuklara Müzik**
Sibel müzik derslerine çocukları da almaya başlar. Eski notalar, tahtaya çizilmiş birkaç çizgiyle yeniden anlam kazanır.
A) Bir derse katıl. *(etki yok)*→K163
B) Kapıdan izle. *(etki yok)*→K160
**K160 — YIKICI 💀🏠**
Kemal eski onarım noktalarını gösterir. Bazıları yeniden açılmıştır; özellikle geçici yamalar artık yük taşımamaktadır.
A) Büyük bir onarım başlat. `🥫-2 🏠+2`→K163
B) Bir kez daha ertele. *(🏠≤3 ise `🏠=0`→SALTANAT SONU; 🏠>3 ise `🏠-1`)*→K161
**K161 — Yürümeye Başlıyor**
Gül’ün çocuğu artık sığınağın içinde kendi başına dolaşmaktadır. Peşinden koşan yetişkinler ona yetişmekte zorlanır.
A) Onlarla birlikte sevincini paylaş. *(etki yok)*→K165
B) İşine devam et. *(etki yok)*→K162
**K162 — Örgütlenen Enfekteler Sonucu ⚑**
*(İzlendiyse)*
Ömer’in gözlemleri enfektelerin gerçekten örgütlü hareket ettiğini doğrular. Bu artık tek bir rastlantıyla açıklanamaz.
A) Bulguları Zeynep’e aktar. `☺-1` *(bilimsel_gozlem=evet)*→K164
B) Şimdilik bilgiyi sakla. *(etki yok)*→K163

*(Mesafeli rapor edildiyse — varyant: zombi_izle=hayir)*
Uzaktan yapılan gözlemler kesin bir sonuç vermez. Enfektelerin ne kadar bilinçli hareket ettiği hâlâ belirsizdir.
A) Konuyu aklında tut. *(etki yok)*→K165
B) Günlük işlere dön. *(etki yok)*→K165
**K163 — Yeni Nesil (1/2)**
Ali’ye ilk kez tek başına sorumluluk taşıyacağı büyük bir görev verilir. Artık yanında sürekli bir yetişkin olmadan da karar vermesi beklenmektedir.
A) Görevi bağımsız yürütmesine izin ver. *(ali_bagimsiz=evet)*→K166
B) Yakınında deneyimli biri bulunsun. →K164
**K164 — Yeni Nesil (2/2) Sonuç**
Görev sırasında Ali beklenmedik bir tehlikeyle karşılaşır. Haber sığınağa ulaştığında hâlâ kendi başına çözüm aramaktadır.
A) Yardım ekibi gönder. `🥫-1 ☺+1 🩺0`→K166
B) Müdahale etmeyip kendi çözmesini bekle. *(ali_sinandi=evet)* `☺+1`→K165
**K165 — Oyun Yayılıyor**
Cem ile Yusuf’un oyunu artık gençler arasında senden habersiz oynanacak kadar yayılmıştır. Yeni kurallar bile çıkarmışlardır.
A) Bir oyuna katıl. *(etki yok)*→K168
B) Uzaktan izle. *(etki yok)*→K166
**K166 — Karakol Gerginliği ⚑**
*(karakol_gerginlik=evet ise)* Kemal sınır işaretlerinin giderek sığınağa yaklaştığını gösterir. “Karakol bunu bilerek yapıyor olabilir.”
A) Resmî uyarı gönder. *(karakol_uyari=evet)*→K168
B) Bir süre daha izle. *(karakol_uyari=hayir)*→K167

*(değilse — varyant: karakol_gerginlik≠evet)* Sabiha yeni bir ticaret rotası çıkarır. Kısa yol daha tehlikeli, uzun yol daha güvenlidir.
A) Riskli rotayı kullan. *(rota=riskli)*→K168
B) Güvenli rotayı kullan. *(rota=guvenli)*→K167
**K167 — Bahçe Yine Açıyor**
Emine Teyze’nin bahçesi bir kez daha çiçek açar. Aziz her yıl aynı düzeni korumaya özen göstermiştir.
A) Bahçede biraz dur. *(etki yok)*→K170
B) Yoluna devam et. *(etki yok)*→K168
**K168 — Lider'le Yeni Temas**
Çitteki “Lider” artık düzenli aralıklarla gelmektedir. Bu kez uzun süre bekler ve doğrudan sana bakar.
A) Zeynep’i de çağır. →K171
B) Onu tek başına dinle. →K169
**K169 — Eski Frekans**
İsmet eski bir frekansta yıllardır duymadığınız türden bir yayın yakalar: zayıf ama gerçek bir müzik istasyonu.
A) Bir süre dinle. *(etki yok)*→K172
B) Frekansı kapat. *(etki yok)*→K170
**K170 — YIKICI 💀☺**
Karakol hakkında dolaşan söylentiler sığınağı ikiye böler. Bir grup ilişkiyi sürdürmek, diğer grup tüm teması kesmek ister.
A) Herkesin konuşabileceği açık toplantı yap. `☺+1`→K173
B) Tartışmayı zorla bastır. *(☺≤3 ise `☺=0`→SALTANAT SONU; ☺>3 ise `☺-1`)*→K171
**K171 — İlk Başarı**
Ali ilk kez önemli bir görevi başarıyla tamamlar. Döndüğünde bunu belli etmemeye çalışsa da yüzündeki ifade değişmiştir.
A) Başarısını kutla. *(etki yok)*→K173
B) Görevin doğal sonucu gibi karşıla. *(etki yok)*→K172
**K172 — Karakol Gerginliği Sonucu ⚑**
*(Uyarıldıysa)* Karakol, Kemal’in gönderdiği uyarının ardından sınır işaretlerini geri çeker. Buna karşılık iki taraf arasındaki güven daha da azalır.
A) Tansiyonu düşürmek için özür dile. `☺-1`→K175
B) Uyarının gerekli olduğunu açıkça söyle. `☺-1`→K175

*(İzlendiyse — varyant: karakol_uyari=hayir)* Sınır birkaç gün içinde daha da yaklaşır. Artık bunun tesadüf olmadığı açıktır.
A) Kendi sınırınızı belirgin biçimde işaretle. `🏠-1 🩺-1`→K175
B) Şimdilik karşılık verme. `🏠-1 🩺-1`→K175

*(Riskli rota seçildiyse — varyant: rota=riskli)* Ekip büyük bir yükle döner ama yol boyunca birkaç kez ölümden dönmüştür.
A) Ekibin riskini takdir et. `🥫+2`→K175
B) Bir daha bu kadar ileri gitmemelerini söyle. `🥫+2`→K175

*(Güvenli rota seçildiyse — varyant: rota=guvenli)* Yolculuk olaysız geçer. Kazanç büyük değildir ama düzenlidir.
A) Bu istikrarı yeterli bul. `🥫+1`→K175
B) Sonraki sefer daha fazlasını hedefle. `🥫+1`→K175
**K173 — Hasat Bayramı**
Hasat ve inşaat çalışmalarının aynı dönemde tamamlanması küçük bir bayrama dönüşür. İnsanlar buna kendiliğinden bir isim bile takar.
A) Kutlamaya katıl. *(etki yok)*→K177
B) Kalabalığın dışında kal. *(etki yok)*→K174
**K174 — LİDER RİSKİ 💀👑**
Karakol’dan doğrudan görüşme daveti gelir. Yer ve saat onlar tarafından belirlenmiştir.
A) Görüşmeye kendin git. *(👑<5 ise ANİ ÖLÜM; değilse `👑-3`)*→K176
B) Bir temsilci gönder. `👑0`→K175
**K175 — Sakin Hafta**
Gergin günlerin ardından bir hafta boyunca önemli hiçbir şey olmaz. Bu kadar sessizlik bile artık garip gelmektedir.
A) İnsanlarla vakit geçir. *(etki yok)*→K177
B) İşine dön. *(etki yok)*→K176
**K176 — Vertak Yankısı**
*(K135-136'daki karara göre)* Vertak’la kurduğunuz eski ilişkinin izi yeniden belirir. Korumasını kabul ettiyseniz yeni bir talep, reddettiyseniz eski bir frekanstan yeni bir sinyal gelir.
A) Mesajı incele. *(vertak_yanki=evet)*→K179
B) Yok say. *(etki yok)*→K177
**K177 — Necati'nin Vefatı**
Necati bir sabah uyanmaz. Ölümü ani bir saldırının değil, yılların ve yorgunluğun sonucudur. *(Not: nüfus bir azalır.)*
A) Anısını birlikte anın. `☺+1`→K180
B) Sessizce işlere devam edin. *(etki yok)*→K178
**K178 — Genişleme Projesi (1/2)**
Kemal sığınağın artık mevcut sınırlarına sığmadığını söyler. Yeni bölmeler açmak mümkündür ama bunun bedeli vardır.
A) Büyük bir genişleme başlat. *(genisleme=buyuk)* `👑-1`→K181
B) Bölgeyi kademeli genişlet. *(genisleme=kademeli)*→K179
**K179 — Genişleme Projesi (2/2) Sonuç**
*(Büyük yatırım seçildiyse)* Genişleme kısa sürede tamamlanır. Yeni alan etkileyicidir ama çalışma ekibini fazlasıyla yormuştur.
A) Tamamlanışı kutla. `🏠+3`→K181
B) Ekibi dinlenmeye gönder. `🏠+3`→K181

*(Kademeli seçildiyse — varyant: genisleme=kademeli)* Yeni alan yavaş yavaş büyür. Gösterişli değildir ama her bölüm sağlam biçimde tamamlanır.
A) Sabırlı ilerleyişi takdir et. `🏠+2`→K181
B) Bunu işin doğal parçası say. `🏠+2`→K181
**K180 — Yeni Bölgede İlk Gece**
Yeni açılan bölgede ilk geceyi geçirecek kadar yer hazırlanmıştır. Eski bölüm hâlâ daha tanıdık ve güvenli hissettirir.
A) Yeni bölgede kal. *(etki yok)*→K183
B) Eski bölümde kal. *(etki yok)*→K181
**K181 — Enfektelerle Bölge Anlaşması (1/2)**
“Lider” çitin ötesindeki boş araziyi gösterip anlaşılır birkaç kelime kurar. Enfekteler o bölgeyi sizinle paylaşmayı teklif ediyor gibidir.
A) Teklifi kabul et. *(zombi_anlasma=evet)*→K183
B) Teklifi reddet. *(zombi_anlasma=hayir)*→K182
**K182 — Enfektelerle Bölge Anlaşması (2/2)**
*(Kabul edildiyse)* İlk günler garip geçse de iki taraf aynı bölgede birbirine saldırmadan yaşamayı başarır. *(zombi_komsuluk=evet)*
A) Anlaşmayı kadroya açıkça anlat. `☺+1 🩺-1`→K184
B) Ayrıntıları gizli tut. `☺+1 🩺-1`→K184

*(Reddedildiyse — varyant: zombi_anlasma=hayir)* İki taraf arasında belirgin bir sınır çizilir. Mesafe arttıkça güvenlik de artar.
A) Sınırı açıkça işaretle. `🏠+1`→K184
B) İşaret koymadan mesafeyi koru. `🏠+1`→K184
**K183 — Çırak Eğitimi**
Ali artık kendi çırağını yetiştirecek kadar deneyimlidir. İlk kez bir başkasının hatalarından da sorumlu olacaktır.
A) Bu gelişmeyi takdir et. *(etki yok)*→K187
B) Bunu doğal bir geçiş say. *(etki yok)*→K184
**K184 — YIKICI 💀🩺**
Yeni açılan bölgede birkaç kişide aynı belirtiler görülür. Zeynep bunun yayılmadan durdurulabilecek bir hastalık olabileceğini söyler.
A) Sıkı karantina uygula. `🥫-1 🩺+1`→K186
B) Hayatı normal sürdür. *(🩺≤3 ise `🩺=0`→SALTANAT SONU; 🩺>3 ise `🩺-1`)*→K185
**K185 — Bir Konser Daha**
Sibel ve öğrencileri yeni bölgede ilk konserlerini verir. Çalanların bir kısmı yıllar önce notayı bile bilmiyordu.
A) Konseri dinle. *(etki yok)*→K188
B) Uzaktan izle. *(etki yok)*→K186
**K186 — İsmet'in Keşfi ⚑**
İsmet eski bir askerî frekansta kodlanmış, tekrar eden bir mesaj yakalar. Kaynağı oldukça uzaktadır.
A) Mesajı çözmeye çalış. *(mesaj_cozuldu=evet)*→K188
B) Frekansı yok say. *(etki yok)*→K187
**K187 — Hediye Resimler**
Fatma’nın resimleri artık diğer topluluklara da hediye edilmektedir. Bazılarının duvarlarında sığınağın çizimleri görülmeye başlar.
A) Bu geleneği destekle. *(etki yok)*→K190
B) Üzerinde durma. *(etki yok)*→K188
**K188 — İkinci Kelime**
Gül’ün çocuğu “anne” dışında yeni bir kelime söyler. Odanın yarısı ne dediğini anlamaz, diğer yarısı farklı bir kelime duyduğunu iddia eder.
A) Gülümseyip kutla. *(etki yok)*→K192
B) Şaşkınlığını belli et. *(etki yok)*→K189
**K189 — İsmet'in Keşfi Sonucu ⚑**
*(Deşifre edildiyse)*
Kod çözülünce mesajın uzak bir topluluktan gönderilmiş SOS çağrısı olduğu anlaşılır.
A) Yardım göndermek için harekete geç. `🥫-1 ☺+1` *(uzak_topluluk=evet)*→K192
B) Mesafeyi koru. *(etki yok)*→K190

*(Yok sayıldıysa — varyant: mesaj_cozuldu≠evet)*
Sinyal günler içinde zayıflayıp tamamen kaybolur. Ne olduğu hiçbir zaman öğrenilemez.
A) Kaydını arşivde tut. *(etki yok)*→K192
B) Konuyu kapat. *(etki yok)*→K192
**K190 — Haftalık Toplantı**
Haftalık toplantılar artık sığınağın olağan düzeninin bir parçasıdır. İnsanlar sorunlarını doğrudan burada dile getirir.
A) Tartışmaya katıl. *(etki yok)*→K193
B) Bu kez yalnızca dinle. *(etki yok)*→K191
**K191 — LİDER RİSKİ 💀👑**
*(Yardıma gidildiyse)* SOS çağrısının geldiği bölgeye ulaşmak tehlikelidir. Yolun bir kısmı enfekte bölgelerden geçmektedir.
A) Ekibe kendin liderlik et. *(👑<5 ise ANİ ÖLÜM; değilse `👑-3`)*→K194
B) Bir ekip gönder. `👑0`→K192

*(Mesafeli kalındıysa — varyant: uzak_topluluk≠evet)* O gün olağan dışı hiçbir gelişme olmaz. Nöbet çizelgesi bile sakindir.
A) Devriyeye çık. *(etki yok)*→K192
B) Dinlen. *(etki yok)*→K192
**K192 — Vertak Yankısı Sonucu**
*(İncelendiyse)* Vertak sinyalinin içinde önceki kayıtlarla bağlantılı yeni bir ayrıntı bulunur. `pharma_arastirma+1`
A) Bulguyu paylaş. `☺-1`→K195
B) Arşivde sakla. *(etki yok)*→K195

*(Yok sayıldıysa — varyant: vertak_yanki≠evet)* Sinyal zamanla tamamen kaybolur ve geride doğrulanabilir hiçbir iz bırakmaz. *(etki yok)*→K195
**K193 — Herkes Döner**
Dış görevde olanlar geri döner. O akşam uzun zamandır ilk kez herkes aynı çatı altındadır.
A) Dinlen. *(etki yok)*→K196
B) İşlere dön. *(etki yok)*→K194
**K194 — Tarih Arşivi**
İsmet’in tarih arşivi artık birkaç defterden çok daha fazlasıdır. Eski liderlerin kararları bile ayrı ayrı kaydedilmiştir.
A) Kendi bildiklerini ekle. *(etki yok)*→K198
B) Arşivi ona bırak. *(etki yok)*→K195
**K195 — Komşuluk Sınavı**
*(zombi_komsuluk=evet ise)* Enfektelerle kurulan komşuluk ilk kez ciddi biçimde sınanır. Sınırda beklenmedik bir hareketlilik başlar.
A) Sakin kalıp önce ne olduğunu anlamaya çalış. `☺+1`→K198
B) Sert biçimde karşılık ver. `☺-1` *(zombi_komsuluk_gergin=evet)*→K196
**K196 — İsim Koyma Günü**
Sığınakta yeni bir çocuk doğar. İsim koyma günü yıllar önce Gül’ün bebeğinde olduğu gibi yine küçük bir törene dönüşür.
A) Törene katıl. *(etki yok)*→K200
B) Kısa bir tebrikle yetin. *(etki yok)*→K197
**K197 — Küçük Şebeke**
Kemal seni yeni kurduğu küçük elektrik şebekesinin başına götürür. Birkaç bölme artık birbirinden bağımsız enerji alabilmektedir.
A) Ekibi tebrik et. *(etki yok)*→K201
B) Çalışmayı normal bir gelişme say. *(etki yok)*→K198
**K198 — Çırağın Çırağı**
Ali’nin yetiştirdiği çırak artık kendi yanında birini eğitmeye hazırlanır. Bilgi ilk kez üçüncü ele geçmektedir.
A) Bu gelişmeyi takdir et. *(etki yok)*→K200
B) Zamanın ne kadar geçtiğine şaşır. *(etki yok)*→K199
**K199 — Mete'nin Şüphesi ⚑**
Mete Karakol’la ilgili raporları önüne koyar. “Bize söyledikleriyle yaptıkları tam örtüşmüyor. Kendi hesapları olabilir.”
A) Şüphesini araştır. *(son_kusku=evet)*→K201
B) Karakol’a güven. →K201
**K200 — Dönüm Noktası (Sezon 3 Ara Kapanışı)**
Üçüncü dönemin bu noktasında sığınak artık çevresinden kopuk değildir; komşuları, ticaret yolları ve düşmanları vardır. Hikâye burada bitmez, yalnızca başka bir ölçeğe geçer.
A) Devam et. →K201
B) Yeni döneme geç. →K201
---

### BÖLÜM VI (K201-K250)

**K201 — Yeni Mevsim**
Yeni bir mevsim başlar. Hava değişirken sığınağın günlük düzeni şaşırtıcı ölçüde aynı kalır.
A) Mevsimin gelişini küçük bir kutlamayla karşıla. *(etki yok)*→K203
B) Günü olağan şekilde geçir. *(etki yok)*→K202
**K202 — Mete'nin Şüphesi Sonucu ⚑**
*(Araştırıldıysa)* Mete’nin şüphesi kısmen doğrulanır: Karakol, ittifakları kendi çıkarına göre yönlendirmeyi planlamaktadır; henüz açık bir hamle yapmamıştır. *(karakol_niyet_bilindi=evet)*
A) Kadroyu durumdan haberdar et. `☺-1`→K205
B) Bilgiyi şimdilik dar bir çevrede tut. *(etki yok)*→K205

*(Güvenildiyse — varyant: son_kusku≠evet)* Günler geçer, Karakol’dan şüpheyi doğrulayacak bir hareket gelmez. Mete’nin kaygısı şimdilik yersiz görünür. →`☺+1`→K205
**K203 — Veli'nin Yolu**
Veli sonunda kendi alanını seçmeye hazırlanır. Kemal’in atölyesiyle İsmet’in telsiz odası arasında gidip gelmektedir.
A) Mühendisliğe yönelmesini destekle. *(veli_yol=muhendislik)*→K205
B) Telsizciliği kendi başına seçmesine izin ver. *(veli_yol=telsizcilik)*→K204
**K204 — Büyük Kriz Habercisi ⚑**
Mustafa ile Mete ufukta alışılmadık bir hareketlilik fark eder. Ne yaklaştığını henüz seçememektedirler.
A) Erken uyarı düzeni kur. `🏠-1` *(erken_uyari=evet)*→K206
B) Daha fazla bilgi gelene kadar izle. *(erken_uyari=hayir)*→K205
**K205 — Karakol Krizi (1/2)**
Karakol’dan dağınık haberler gelmeye başlar: içeride yönetim kavgası çıkmış, eski düzen çözülmektedir.
A) Değişimi yakından değerlendir. *(karakol_yeni_yonetim=evet)*→K206
B) İç işlerine karışma. →K206
**K206 — Karakol Krizi (2/2) Sonuç**
*(Değerlendirilip yakınlaşıldıysa)* Karakol’daki yeni yönetimle önceki dönemden daha dengeli bir ilişki kurulur.
A) Şartları yazılı anlaşmaya bağla. `🥫+1 ☺+1`→K207
B) Sözlü mutabakatla yetin. `🥫+1 ☺+1`→K207

*(Değerlendirilip temkinli kalındıysa — varyant)* Yeni yönetim izlenir ama iki taraf da yakınlaşmak için acele etmez.
A) Gözlemleri kadroya raporla. *(etki yok)*→K207
B) Notları şimdilik kendinde tut. *(etki yok)*→K207

*(Karışılmayıp hazırlanıldıysa — varyant: karakol_yeni_yonetim≠evet)* Karakol’daki belirsizliğe karşı sığınağın sınırları sıkılaştırılır.
A) Yeni önlemleri kadroya açıkla. `🏠+1`→K208
B) Önlemleri sessizce uygula. `🏠+1`→K208

*(Karışılmayıp beklenildiyse — varyant)* Karakol’daki belirsizlik çözülmez. Söylentiler sığınağa ulaştıkça huzursuzluk biraz daha artar.
A) Soğukkanlı kal. `☺-1`→K208
B) Olası sorunlara karşı insanları uyar. `☺-1`→K208
**K207 — Bağımsız İlk Görev**
Ali’nin yetiştirdiği çırak ilk görevini tek başına tamamlayıp geri döner. Artık yalnızca bir öğrenciden söz etmek zordur.
A) Başarısını takdir et. *(etki yok)*→K210
B) Görevin doğal sonucu gibi karşıla. *(etki yok)*→K208
**K208 — YIKICI 💀🏠**
Sığınak büyüdükçe eski yapıya eklenen bölmeler birbirini zorlamaya başlar. Kemal bir taşıyıcı noktadaki sorunu gösterir. “Bunu ertelemek artık kumar.”
A) Acil müdahale başlat. `🥫-2 🏠+1`→K211
B) Riski göze alıp bekle. *(🏠≤3 ise `🏠=0`→SALTANAT SONU; 🏠>3 ise `🏠-1`)*→K209
**K209 — Büyük Kriz Sonucu ⚑**
*(Erken uyarı kurulduysa)* Yaklaşan kriz hazırlıklar sayesinde beklenenden daha hafif atlatılır. Mustafa’nın kurduğu düzen ilk alarmda çalışır.
A) Mustafa’nın hazırlığını özellikle takdir et. `☺+1`→K211
B) Başarıyı bütün ekibe mal et. `☺+1`→K211

*(Kurulmadıysa — varyant: erken_uyari=hayir)* Kriz sığınağı hazırlıksız yakalar. Zarar sınırlanır ama bunun bedeli ağır olur.
A) Hazırlıksızlığın sorumluluğunu kabul et. `🩺-1 ☺-1`→K211
B) Kararı savunup hızla toparlanmaya geç. `🩺-1 ☺-1`→K211
**K210 — Dayanışma**
Krizden sonra insanlar birbirlerinin işine kendiliğinden yardım etmeye başlar. Birkaç gün boyunca görev listesine bakmaya bile gerek kalmaz.
A) Bu dayanışmayı birlikte kutla. *(etki yok)*→K212
B) Sessizce sürmesine izin ver. *(etki yok)*→K211
**K211 — Lider'in Mesajı**
*(zombi_komsuluk=evet ise)* “Lider” çite gelir ve bu kez uzun, parçalı cümlelerle bir şey anlatmaya çalışır. Söylediğinin önemli olduğu bellidir.
A) Zeynep’i çağırıp birlikte dinle. →K213
B) Onu tek başına dinle. →K212

*(değilse — varyant: zombi_komsuluk≠evet)* Nöbet günü olaysız geçer. Çitin ötesinde yalnızca rüzgâr ve uzaktaki hareketler vardır. →K212
**K212 — Sığınağın Gururu**
İsmet’in arşivi artık yalnızca onun işi sayılmaz. İnsanlar kendi notlarını, haritalarını ve hatıralarını da buraya bırakmaktadır.
A) Arşive kendi katkını ekle. *(etki yok)*→K215
B) Kaydı İsmet’e bırak. *(etki yok)*→K213
**K213 — Yeni Nesil Liderlik**
*(ali_yol=savunma ise Ali, değilse Veli — hangisi sığınağın güvenlik/karar çizgisine daha yakınsa o)* Yeni nesilden biri ilk kez resmî karar toplantısında masaya oturur. Bu kez yalnızca dinleyen bir çırak değildir.
A) Görüşünü açıkça söylemesini iste. `☺+1`→K215
B) İlk toplantıda gözlemlemesine izin ver. →K214
**K214 — Ticaret Ağı Genişliyor**
Sabiha’nın kurduğu ticaret ağı artık birden fazla topluluğu birbirine bağlamaktadır. Yeni bir rota daha eklemek mümkündür ama ağ büyüdükçe denetim zorlaşır.
A) Ağı daha da genişlet. `🥫+1` *(ticaret_agi=genis)*→K216
B) Mevcut ölçekte tut. *(ticaret_agi=sinirli)*→K215
**K215 — LİDER RİSKİ 💀👑**
Karakol’daki kriz doğrudan sığınağın çevresine sıçrar. Silahlı grupların hareket ettiği haberi gelir ve hızlı karar vermek gerekir.
A) Duruma kendin müdahale et. *(👑<5 ise ANİ ÖLÜM; değilse `👑-3`)*→K218
B) Müdahaleyi ekibe bırak. `👑0`→K216
**K216 — Sakinlik**
Karakol’daki hareketlilik yatışınca sığınağa yeniden gündelik sessizlik döner.
A) Bir gün dinlen. *(etki yok)*→K218
B) İşlere dön. *(etki yok)*→K217
**K217 — Aziz'in Büyük Hasadı (1/2)**
Aziz genişleyen tarlaları gösterir. “Hava böyle giderse rekor kırabiliriz. Ama sonuna kadar zorlarsak bir terslikte daha çok kaybederiz.”
A) Verimi zorlayıp riske gir. *(hasat=riskli)*→K219
B) Güvenli yöntemle ilerle. *(hasat=guvenli)*→K218
**K218 — Aziz'in Büyük Hasadı (2/2) Sonuç**
*(Riskli hasat, tarımı Ali seçtiyse iyi gitti)* Ali’nin tarım bilgisiyle alınan risk karşılığını verir; depolar yıllardır görülmemiş ölçüde dolar.
A) Hasadı şenlikle kutla. `🥫+4`→K221
B) Fazlayı doğrudan depola. `🥫+4`→K221

*(Riskli hasat, tarımı kimse desteklemediyse — varyant: hasat=riskli)* Hava son anda döner. Ürünün yalnızca bir kısmı kurtarılabilir.
A) Kurtarılan ürünle yetin. `🥫+1`→K221
B) Gelecek sezon aynı yöntemi yeniden denemeyi planla. `🥫+1`→K221

*(Güvenli hasat seçildiyse — varyant: hasat=guvenli)* Hasat beklendiği gibi gelir: büyük değildir ama kayıp da yoktur.
A) Aziz’in planını takdir et. `🥫+2`→K221
B) Sonucu olağan kabul et. `🥫+2`→K221
**K219 — İlk İhracat**
Depolar ilk kez sığınağın ihtiyacından fazlasını verir. Sabiha, artan erzağın başka topluluklarla düzenli takasa çıkarılmasını önerir.
A) İlk büyük dış satışı kutla. *(etki yok)*→K223
B) Fazlayı verirken temkinli davran. *(etki yok)*→K220
**K220 — Vertak'ın Sonu ya da Devamı**
*(pharma_arastirma ve K135-136'daki karara göre)* Yıllardır süren Vertak meselesi sonunda net bir biçim alır: ya içeriden çözülür ya da gücünü kaybedip bölgeden çekilir. İlk kez adı günlük bir tehdit gibi anılmaz.
A) Tehlikenin geçtiğini kabul et. `☺+2`→K223
B) Yine de savunmayı gevşetme. `🏠+1`→K221
**K221 — Geçmiş Anlatısı**
Sığınaktaki en yaşlı kişi gençleri etrafına toplayıp ilk yılları anlatır. Bazıları anlattığı olaylar yaşanırken henüz doğmamıştır.
A) Oturup birlikte dinle. *(etki yok)*→K224
B) Günlük işine dön. *(etki yok)*→K222
**K222 — YIKICI 💀☺**
Yeni yetişenlerle eski kuşak arasında ilk kez açık bir değer çatışması yaşanır. Mesele tek bir karar değil, sığınağın bundan sonra nasıl yönetileceğidir.
A) Ortak bir karar arayın. `☺+1`→K226
B) Son sözü otoriteyle ver. *(☺≤3 ise `☺=0`→SALTANAT SONU; ☺>3 ise `☺-1`)*→K223
**K223 — Uzlaşma Sonrası**
Tartışma çözülmüş olsa da etkisi hemen geçmez. Sığınak bir hafta boyunca alınan kararın havasını taşır.
A) İnsanların arasında kal. *(etki yok)*→K226
B) Bir süre yalnız kal. *(etki yok)*→K224
**K224 — Ne Kadar Değişti**
İlk günkü sığınakla bugünkü yer arasında neredeyse yalnızca duvarların adı ortaktır. Ali ile Veli’nin yolları, Karakol’la kurulan ilişki ve Vertak’tan kalan izler artık bu hayatın parçasıdır.
A) Değişimi arşive kaydet. `☺+1`→K225
B) Üzerinde konuşmadan kabul et. *(etki yok)*→K225
**K225 — Gelenek Günü**
Yıllar içinde kendiliğinden bir “gelenek günü” oluşmuştur. İlk dönemden beri yaşamış olanlar ve artık aranızda bulunmayanlar o gün isimleriyle anılır; Necati de onlardan biridir.
A) Anmaya katıl. *(etki yok)*→K229
B) Kenardan izle. *(etki yok)*→K226
**K226 — Mühendislik Mirası**
Kemal’in yaptığı işler artık tek tek projeler olmaktan çıkmıştır. Bölmeler, güneş panelleri, onarımlar ve genişleme sığınağın kalıcı altyapısına dönüşmüştür.
A) Yaptıklarının değerini ona söyle. `☺+1`→K230
B) Bunları artık düzenin doğal parçası say. *(etki yok)*→K227
**K227 — Konuşan Enfekte: Son Mesaj (1/2)**
“Lider” son kez çitin önünde belirir. Bu kez birkaç kelimeyi açıkça söyleyebilir ama mesajının uyarı mı, veda mı yoksa teklif mi olduğu henüz anlaşılmaz.
A) Sonuna kadar dinle. →K230
B) Mesafeyi koru. →K228
**K228 — Konuşan Enfekte: Son Mesaj (2/2) Sonuç**
*(Dikkatle dinlendiyse)* Parçalar bir araya gelince mesaj anlaşılır: enfekte topluluğu kendi içinde bölünmektedir ve “Lider” yaklaşan ayrışma konusunda sizi uyarmaktadır. *(zombi_son_mesaj=evet)*
A) Kadroyu olası çatışmaya hazırla. `🏠+1`→K230
B) İkinci bir işaret gelene kadar bekle. *(etki yok)*→K230

*(Mesafede kalındıysa — varyant)* “Lider” bir süre çite yaslanır, sonra tek kelime etmeden uzaklaşır. Mesajın ne olduğu öğrenilemez.
A) Karşılaşmayı kayda geçir. *(etki yok)*→K230
B) Konuyu kapat. *(etki yok)*→K230
**K229 — Artık Bir Ev**
Akşam olduğunda sığınak ilk günkü gibi geçici bir barınak değil, insanların geri döndüğü bir ev gibi görünür.
A) Bir süre oturup bunu düşün. *(etki yok)*→K231
B) Düşünmeden günlük hayatına devam et. *(etki yok)*→K230
**K230 — LİDER RİSKİ 💀👑 (Son Büyük Tehlike)**
Yıllardır ertelenen, bastırılan ve çözülen gerilimlerin bir kısmı aynı anda yeniden yüzeye çıkar. Bu, sığınağın karşılaştığı son büyük sınavlardan biridir.
A) Krizin önüne kendin çık. *(👑<5 ise ANİ ÖLÜM; değilse `👑-4`)*→K234
B) Yetiştirdiğin kadroya güven. `👑0 ☺+1`→K231
**K231 — Fırtına Yine Dinince**
Kriz sona erdiğinde duvarlar hâlâ ayaktadır. Alarm seslerinin ardından gelen sessizlik bu kez yenilgi değil, rahatlamadır.
A) Biraz soluklan. *(etki yok)*→K234
B) Hemen işlere dön. *(etki yok)*→K232
**K232 — Kadronun Mirası**
Sabiha’nın ticareti, Aziz’in tarımı, Kemal’in yapıları ve İsmet’in arşivi artık kişilerden bağımsız işleyen düzenlere dönüşmüştür. Her biri sığınakta kalıcı bir iz bırakmıştır.
A) Hepsine açıkça teşekkür et. `☺+1`→K236
B) İşlerin artık böyle yürümesini doğal karşıla. *(etki yok)*→K233
**K233 — Elleriyle Şekillendiriyor**
Ali, Veli ve onların ardından gelenler artık yalnızca öğrenmiyor; tarımı, savunmayı, mühendisliği ve haberleşmeyi kendileri yürütüyor.
A) Sorumluluğu yeni nesle bırakmaya güven. *(etki yok)*→K235
B) Denetimi bir süre daha sıkı tut. *(etki yok)*→K234
**K234 — Yılların Bilançosu**
Vertak, Karakol ve enfektelerle kurulan bütün ilişkiler artık aynı tabloda görülebilir. Sığınağın bölgede ne kadar güçlü ya da kırılgan olduğu, yıllar boyunca verilen kararların toplamıyla belirlenmiştir. *(Bayrakların toplamına göre metin değişir.)*
A) Elde edilen gücü sahiplen. `☺+1`→K238
B) Kırılganlığı unutmadan savunmayı koru. `🏠+1`→K236
**K235 — Kaç Gündür Ayaktayız**
İsmet’in arşivinde kaç liderin görev yaptığı ve sığınağın kaç gündür ayakta olduğu bile yazılıdır. Sayılar, hatırladığından daha büyüktür.
A) Kayıtları kendin oku. *(etki yok)*→K237
B) Arşivi İsmet’e bırak. *(etki yok)*→K236
**K236 — Gerçek Bir Topluluk**
Büyük toplantıda artık tek bir kişinin sözü belirleyici değildir. Uzmanlar, gençler ve eski sakinler aynı masada konuşur.
A) Görüşünü söyle. `☺+1`→K238
B) Bu kez yalnızca dinle. *(etki yok)*→K237
**K237 — Halefin Yeterliliği**
Zeynep’in yetiştirdiği halef artık reviri tek başına yönetebilecek kadar deneyimlidir. Sağlık hizmeti ilk kez tek bir kişiye bağlı değildir.
A) Başardıklarını takdir et. *(etki yok)*→K239
B) Bunu sistemin doğal sonucu say. *(etki yok)*→K238
**K238 — Yerleşen Düzen**
Ömer’in kurduğu nöbet düzeniyle Mustafa ve Mete’nin savunma sistemi artık kişiler değişse bile işleyecek kadar yerleşmiştir.
A) Bu düzenin değerini açıkça vurgula. *(etki yok)*→K242
B) Günlük hayatın parçası say. *(etki yok)*→K239
**K239 — Son Sakin Akşam (2)**
Bir akşam herkes aynı yerde toplanır. Kimse bunun “son sakin akşam” olduğunu söylemez; yalnızca uzun zamandır ilk kez masada boşluk azdır.
A) Oradakilere teşekkür et. *(etki yok)*→K242
B) Sessizce onlarla otur. *(etki yok)*→K240
**K240 — Ne Kadar Yol Alındı**
İsmet’in eski kayıtları ilk günün korkusunu hatırlatır. Bugünkü sığınak, o kapının önündeki birkaç saatten çok uzakta bir yerdedir.
A) İlk günü yeniden hatırla. *(etki yok)*→K242
B) Gelecek yıllara odaklan. *(etki yok)*→K241
**K241 — Sığınağın Adı**
İnsanlar sığınağa yıllardır aynı adı takmaktadır. İsim artık haritalarda ve ticaret notlarında bile görünmeye başlar.
A) Adı resmî olarak kabul et. `☺+1`→K244
B) Halkın kullandığı biçimiyle bırak. *(etki yok)*→K242
**K242 — Sembol Bahçe**
Emine Teyze’nin bahçesi yine çiçektedir. Kendisinden sonra da her yıl birileri toprağı havalandırmış, tohumları yenilemiştir.
A) Bahçede biraz kal. *(etki yok)*→K245
B) Yoluna devam et. *(etki yok)*→K243
**K243 — Atilla'nın Mirası**
Gül’ün çocuğu artık düzenli derslere katılır. Atilla’nın yıllar önce başlattığı eğitim düzeni, onu kuranlardan bağımsız biçimde sürmektedir.
A) Bir derse katıl. *(etki yok)*→K246
B) Kapıdan izle. *(etki yok)*→K244
**K244 — Tarımın Mirası**
Aziz’in kurduğu tarım düzeni artık sığınağın ana geçim kaynağıdır. Bir zamanlar her porsiyonun hesabı yapılırken şimdi ekim takvimleri konuşulmaktadır.
A) Bu değişimi özellikle takdir et. *(etki yok)*→K248
B) Artık olağan kabul et. *(etki yok)*→K245
**K245 — Son Kart Öncesi**
Gece yaklaşırken kadro bir kez daha aynı yerde toplanır. Ortamdaki sessizlik yorgunluktan çok, uzun bir işi tamamlamış insanların sessizliğidir.
A) O anın içinde kal. *(etki yok)*→K248
B) Bir sonraki güne odaklan. *(etki yok)*→K246
**K246 — Son Toplantı**
Zeynep, Sabiha, Ömer, Kemal, Atilla, Aziz, İsmet, Mustafa ve Mete aynı masada son bir geniş toplantıya katılır. Yıllarca farklı krizlerde verilen kararlar artık ortak bir geçmişe dönüşmüştür.
A) Tartışmaya katıl. *(etki yok)*→K250
B) Bu kez yalnızca dinle. *(etki yok)*→K247
**K247 — Günlüğe Son Kayıt**
İsmet sığınak günlüğünün son boş sayfalarından birini açar. Kalemi masanın ortasına bırakır.
A) Son kaydı kendin yaz. *(etki yok)*→K250
B) Kaydı İsmet yazsın. *(etki yok)*→K248
**K248 — Huzurlu Sessizlik**
Gece çöker ve sığınak yavaşça sessizleşir. İlk yıllardaki sessizlik tehlike beklemek demekti; bu kez insanlar yalnızca uyuyordur.
A) Bir süre dışarıyı izle. *(etki yok)*→K250
B) İçeri dön. *(etki yok)*→K249
**K249 — Son An**
Kaç liderin gelip geçtiği, kaç günün sayıldığı artık tek başına önemli değildir. Duvarların içinde hayat sürmekte ve sığınak hâlâ ayaktadır.
A) Geçen yılları düşün. *(etki yok)*→K250
B) Hiçbir şey söylemeden o anı yaşa. *(etki yok)*→K250
**K250 — SEZON 3 DÖNÜM NOKTASI (Büyük Kapanış)**
K1’den bu yana verilen 250 kararın izi sığınağın bugünkü hâlinde görünür: kaç liderin görev yaptığı, Karakol ve Vertak’la kurulan ilişkiler, konuşan enfektelerle savaş mı yoksa birlikte yaşam mı seçildiği burada birleşir. Bu bir son değildir; sığınağın tarihi buradan sonra da aynı kararların ağırlığıyla devam edebilir.
A) Günlüğü kapat.
B) Sessizce otur.
---

## 5. ÖZET
250 kart, tek dosya, tek kadro. **Hiçbir ara-etiket (K1a/K1b tarzı) yok** — her kart doğrudan gerçek kart numaralarına gider. v12'de her gecikmeli zincirin (⚑) her iki dalı da kendi sonuç kartına garanti ulaşır; hiçbir sonuç kartı, o rotada set edilmemiş bir bayrak talep etmez. Belirsiz doğal-dil sonuçları ("değişken", "swing", "çoğunlukla") kaldırılmış, yerine bayrak/eşik tabanlı deterministik dallanma konmuştur. Değişken gecikmeli zincirler, çok kartlı olaylar, deterministik ölüm kuralı, doğru saltanat geçişi (asla baştan başlanmaz) korunmuştur.
