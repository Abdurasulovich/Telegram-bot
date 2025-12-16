# Telegram Survey Bot

Ushbu bot talabalar uchun so'rovnomalar o'tkazish uchun mo'ljallangan. Bot ikki xil so'rovnoma o'tkazadi:
1. **Korrupsiya so'rovnomasi** (faqat o'zbek tilida)
2. **O'qituvchilarni baholash** (O'zbek, Qoraqalpoq, Rus tillarida)

## Xususiyatlar

- ✅ Foydalanuvchilarni ro'yxatdan o'tkazish (telefon raqami va username)
- ✅ Til tanlash (O'zbek, Qoraqalpoq, Rus)
- ✅ Test shaklida so'rovnoma o'tkazish
- ✅ Javoblarni SQLite ma'lumotlar bazasida saqlash
- ✅ Interaktiv tugmalar (inline keyboard)

## O'rnatish

1. .NET SDK o'rnatilganligiga ishonch hosil qiling:
```bash
dotnet --version
```

2. Loyihani restore qiling:
```bash
cd TelegramSurveyBot
dotnet restore
```

## Telegram Bot yaratish

1. Telegram'da [@BotFather](https://t.me/BotFather) ga o'ting
2. `/newbot` buyrug'ini yuboring
3. Bot uchun nom kiriting (masalan: "Survey Bot")
4. Bot uchun username kiriting (masalan: "my_survey_bot")
5. BotFather sizga bot tokenini beradi (masalan: `1234567890:ABCdefGHIjklMNOpqrsTUVwxyz`)

## Sozlash

`appsettings.json` faylidagi `YOUR_BOT_TOKEN_HERE` ni o'zingizning bot tokeningiz bilan almashtiring:

```json
{
  "BotConfiguration": {
    "BotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
  }
}
```

Yoki dasturni ishga tushirganda token so'raladi va uni kiritishingiz mumkin.

## Ishga tushirish

```bash
dotnet run
```

Bot ishga tushgandan so'ng, Telegram'da botingizni toping va `/start` buyrug'ini yuboring.

## Bot ishlashi

1. Foydalanuvchi `/start` buyrug'ini yuboradi
2. Bot telefon raqamini so'raydi
3. Foydalanuvchi telefon raqamini yuboradi
4. Bot til tanlashni taklif qiladi (O'zbek, Qoraqalpoq, Rus)
5. Foydalanuvchi tilni tanlaydi
6. Bot so'rovnomalarni ko'rsatadi:
   - Agar **O'zbek tili** tanlangan bo'lsa: ikkala so'rovnoma (Korrupsiya va O'qituvchilarni baholash)
   - Agar **boshqa til** tanlangan bo'lsa: faqat O'qituvchilarni baholash
7. Foydalanuvchi so'rovnomani tanlaydi
8. Bot savollarni birin-ketin yuboradi
9. Foydalanuvchi variantlardan birini tanlaydi
10. Barcha savollarga javob berilgandan so'ng, bot rahmat aytadi va qayta menu ko'rsatadi

## Ma'lumotlar bazasi

Barcha ma'lumotlar `survey.db` SQLite fayliga saqlanadi. Ma'lumotlar bazasida:
- `Users` jadvali: foydalanuvchilar ma'lumotlari
- `UserResponses` jadvali: so'rovnoma javoblari

## Texnologiyalar

- .NET 9.0
- Telegram.Bot 22.7.6
- Entity Framework Core 9.0 (SQLite)

## Muammolar

Agar qandaydir muammo yuzaga kelsa:
1. Bot tokenini to'g'ri kiritganingizga ishonch hosil qiling
2. Internetga ulanganligingizni tekshiring
3. Consoledagi xato xabarlarini o'qing

## Mualliflik huquqi

Bu bot o'quv maqsadida yaratilgan.
