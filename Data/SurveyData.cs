namespace TelegramSurveyBot.Data;

public class Question
{
    public string Text { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public bool AllowMultiple { get; set; } = false; // Bir nechta javob tanlash mumkinmi
    public bool RequireTextInput { get; set; } = false; // Text kiritish kerakmi
    public int MaxBall { get; set; } = 0; // O'qituvchilarni baholash uchun maximal ball
}

public static class SurveyData
{
    // Korrupsiya so'rovnomasi (O'zbek tili)
    public static List<Question> CorruptionSurveyUz = new()
    {
        new Question
        {
            Text = "1. Tahsil olayotgan kursingizni belgilang!",
            Options = new() { "1-kurs", "2-kurs", "3-kurs", "4-kurs" }
        },
        new Question
        {
            Text = "2. Siz universitetda pora olish yoki berish jarayoniga guvoh bo'lganmisiz?",
            Options = new() { "xa, guvoh bo'lganman", "yo'q, guvoh bo'lmaganman", "men bilmayman" }
        },
        new Question
        {
            Text = "3. Universitetda pora miqdori qancha deb bilasiz?",
            Options = new()
            {
                "100 ming so'mdan 200 ming so'mgacha",
                "200 ming so'mdan 500 ming so'mgacha",
                "500 ming so'mdan 800 ming so'mgacha",
                "bilmayman, duch kelmaganman"
            }
        },
        new Question
        {
            Text = "4. O'qituvchilarga pora berish kimlar orqali amalga oshiriladi deb bilasiz?",
            Options = new()
            {
                "Guruh sardori orqali",
                "Tyutor orqali",
                "To'g'ridan-to'g'ri",
                "Tanishlar orqali",
                "Boshqa o'qituvchilar orqali",
                "Duch kelmaganman"
            }
        },
        new Question
        {
            Text = "5. Korrupsion holatlar to'g'risida xabar berishda Universitetning Korrupsiyaga qarshi kurashish bo'limining ishonch telefonlarin va telegram bot aloqa kanallarini bilasizmi?",
            Options = new() { "xa, bilaman", "yo'q, bilmayman", "Endi bilib olaman" }
        },
        new Question
        {
            Text = "6. Siz tahsil olayotgan oliy ta'lim muassasasida pul mablag'larini yig'ish holati bo'lib turadimi?",
            Options = new() { "Ha, doim bo'lib turadi", "Ha, faqat ba'zida", "Bunday holatlar mavjud emas" }
        },
        new Question
        {
            Text = "7. Universitetda korruptsion holatlarga duch kelsangiz, bu to'g'risida xabar berishga tayyormisiz?",
            Options = new() { "Ha", "Ha, faqat anonim tarzda", "Yo'q" }
        },
        new Question
        {
            Text = "8. Dars mashg'ulotlariga muntazam qatnashmaydigan talabalar mavjudmi?",
            Options = new() { "Ha", "Yo'q" }
        },
        new Question
        {
            Text = "9. Qaysi fanlardan baho olishda korruptsion holatlarga duch kelgansiz? (Fan va o'qituvchi ismini yozing)",
            Options = new() { },
            RequireTextInput = true
        },
        new Question
        {
            Text = "10. Siz universitetda korruptsion vaziyatga duch kelganmisiz? (bir nechta variantni tanlash mumkin)",
            Options = new()
            {
                "Oraliq nazoratlar topshirish vaqtida",
                "Yakuniy nazoratlar topshirish vaqtida",
                "Kurs ishlarini bajarish va topshirish vaqtida",
                "Amaliyot ishini bajarish va topshirish vaqtida",
                "Nomdor stipendiyalar tanlovi vaqtida",
                "Turli xil tanlov, olimpiadalar jarayonida",
                "TTJga joylashishda",
                "O'qishni tiklash va ko'chirish davomida",
                "O'qishga bora olmagan kunlari yo'qlamada bor deb ko'rsatish",
                "Duch kelmaganman"
            },
            AllowMultiple = true
        },
        new Question
        {
            Text = "11. Sizningcha, universitetda uchraydigan korruptsiyaviy holatlarning yuzaga kelishiga asosiy sabab nima? (bir nechta javobni tanlash mumkin)",
            Options = new()
            {
                "Talabalarning o'z haq-huquqlarini yaxshi bilmasligi",
                "Ortiqcha rasmiyatchiliklarning ko'pligi",
                "Jazolarning muqarrar emasligi",
                "Belgilanadigan jazolarning engilligi",
                "Oylik maoshlarning kamligi",
                "Korruptsiyaga qarshi kurashish darajasining pastligi"
            },
            AllowMultiple = true
        },
        new Question
        {
            Text = "12. Sizningcha, universitetda uchraydigan korrupsiyaviy holatlarning eng ko'pi kimlar tomonidan sodir etiladi? (bir nechta javobni tanlash mumkin)",
            Options = new()
            {
                "Prorektorlar",
                "Talabalar Ilmiy rahbarlar",
                "Professor-o'qituvchilar",
                "Dekan va dekan o'rinbosarlari",
                "Talabalarga xizmat ko'rsatish xodimlari",
                "Tyutor",
                "Universitetda bunday holatlarga duch kelmaganman"
            },
            AllowMultiple = true
        },
        new Question
        {
            Text = "13. Sizningcha, korruptsiyaning qanday yangi ko'rinishlari uchramoqda? (bir nechta javobni tanlash mumkin)",
            Options = new()
            {
                "Kitob sotish",
                "Sovg'alarni uyiga etkazib berish",
                "Telefonga pul o'tkazish",
                "Duch kelmaganman"
            },
            AllowMultiple = true
        }
    };
    // O'qituvchilarni baholash so'rovnomasi - O'zbek tili
    public static List<Question> TeacherEvaluationUz = new()
{
    new Question
    {
        Text = "1. O'qituvchining bilim darajasi va pedagogik mahoratini qanday baholaysiz?\n\n"+
               "1 - O'z fanini chuqur biladi, darslari mazmunli, tushunarli va qiziqarli o'tadi. (1 ball)\n"+
               "2 - O'z fanini yaxshi biladi, darslarni mazmunli, oddiy va qiziqarli o'tadi (0.9 ball)\n"+
               "3 - O'z fanini o'rtacha biladi, darslarda faqat ma'ruza matnidan foydalanadi (0.7 ball)\n"+
               "4 - O'z fanini yuzaki biladi, mazmuni yo'q hisobi, darsni zerikarli o'tadi (0.5 ball)\n"+
               "5 - O'z fanini bilmaydi, darslar mazmunsiz va tushunarsiz (0 ball)",
        Options = new()
        {
            "1 ball",
            "0.9 ball",
            "0.7 ball",
            "0.5 ball",
            "0 ball"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "2. Sizning fikringizcha o'qituvchining dars o'tish sifati qanday?\n\n"+
               "1 - Darsda dolzarb masalalar ko'tariladi, turmush bilan bog'langan, qiziqarli va yangi ma'lumotlar hamda ularning tahlili taqdim etiladi (1 ball)\n"+
               "2 - Darsda ba'zida dolzarb masalalar ko'tarilib, yangi ma'lumotlar keltirib o'tiladi (0.9 ball)\n"+
               "3 - Darsda dolzarb masalalar umuman bayon qilinmaydi, turmush bilan bog'lanmagan (0.6 ball)\n"+
               "4 - Darsda yangilik yo'q, faktlar eskirgan (0.5 ball)\n"+
               "5 - Darsda yangi bilim berilmaydi (0 ball)",
        Options = new()
        {
            "1 ball",
            "0.9 ball",
            "0.6 ball",
            "0.5 ball",
            "0 ball"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "3. O'qituvchining odob-axloqi, xulq-atvori va shaxsiy fazilatlari haqida nima deysiz?\n\n"+
               "1 - Madaniyatli, xushmuomala, to'g'ri so'z (1 ball)\n"+
               "2 - Unchalik kamtarin va xushmuomala emas (0.7 ball)\n"+
               "3 - Qo'rs, muomala madaniyati past (0.5 ball)\n"+
               "4 - Talabalarga nisbatan befarq (0.3 ball)\n"+
               "5 - Talabalarni pisand qilmaydi, o'ta qo'pol (0 ball)",
        Options = new()
        {
            "1 ball",
            "0.7 ball",
            "0.5 ball",
            "0.3 ball",
            "0 ball"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "4. O'qituvchining talabalarni baholashdagi xolisligi qanday?\n\n"+
               "1 - Adolatli, talaba bilimiga xolisona baho beradi (1 ball)\n"+
               "2 - Talabaning bilimi bo'yicha reytingini biladi (0.7 ball)\n"+
               "3 - Ba'zan adolatsizlik qilib turadi (0.5 ball)\n"+
               "4 - Goho-goho adolatsiz, talabadan manfaat kutadi (0.3 ball)\n"+
               "5 - Juda adolatsiz va talabadan faqat manfaat kutadi (0 ball)",
        Options = new()
        {
            "1 ball",
            "0.7 ball",
            "0.5 ball",
            "0.3 ball",
            "0 ball"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "5. O'qituvchida ta'magirlik holati uchraganmi?\n\n"+
               "1 - Yo'q, bunday holat bo'lmagan (2 ball)\n"+
               "2 - Bundan xabarim yo'q (1.7 ball)\n"+
               "3 - Bo'lishi mumkin, lekin o'zim duch kelmaganman (0.9 ball)\n"+
               "4 - Ta'magirlik qiladi, gaplarida sezilib turadi (0.3 ball)\n"+
               "5 - Oshkora ta'magirlik qilish holati bo'lgan (-2 ball)",
        Options = new()
        {
            "2 ball",
            "1.7 ball",
            "0.9 ball",
            "0.3 ball",
            "-2 ball"
        },
        MaxBall = 2
    },
    new Question
    {
        Text = "6. O'qituvchi dars jarayonida qanday o'qitish uslublaridan foydalanadi?\n\n"+
               "1 - Interaktiv o'qitish uslublaridan samarali foydalanadi, talabalarni faol jalb qiladi (1 ball)\n"+
               "2 - Ba'zan qiziqarli uslublardan foydalanadi, talabalar bilan fikr almashadi (0.7 ball)\n"+
               "3 - Darslar doim bir xil shaklda, asosan ma'ruza tarzida o'tadi (0.5 ball)\n"+
               "4 - Dars boshida qisqacha aytib o'tadi, qolgan vaqtda talabalar o'z holiga tashlab qo'yiladi (0.3 ball)\n"+
               "5 - Darsga aloqasi yo'q, mavzu bo'yicha hech qanday tushuncha bermaydi (0 ball)",
        Options = new()
        {
            "1 ball",
            "0.7 ball",
            "0.5 ball",
            "0.3 ball",
            "0 ball"
        },
        MaxBall = 1
    }
 };

    // O'qituvchilarni baholash - Qoraqalpoq tili
    public static List<Question> TeacherEvaluationKk = new()
{
    new Question
    {
        Text = "1. Sizin pikirinizshe oqitiwshinin bilim dárejesi hám pedagogikaliq sheberligi qanday dárejede dep oylaysiz?\n\n"+
               "1 - Óz pánin tereń biledi, sabaqti mazmunli, túsinikli hám qiziqli ótedi (1 ball)\n"+
               "2 - Óz pánin jaqsi biledi, sabaqti mazmunli, ápiwayí hám qiziqli ótedi (0.9 ball)\n"+
               "3 - Óz pánin ortasha biledi, sabaq ótiwde tek lekciya tekstinen paydalanadı (0.7 ball)\n"+
               "4 - Óz pánin júzeki biledi, sabaqti mazmuni jaǵınan sayız, zeriktirip ótedi (0.5 ball)\n"+
               "5 - Óz pánin bilmeydi, sabaqlar mazmunsız hám túsiniksiz (0 ball)",
        Options = new()
        {
            "1 ball",
            "0.9 ball",
            "0.7 ball",
            "0.5 ball",
            "0 ball"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "2. Sizin pikirinizshe oqitiwshi sabağinin sapası qanday?\n\n"+
               "1 - Sabaqta aktual máseleler kóteriledi, turmıs penen baylanısadı, qızıqlı hám jańa maǵlıwmatlar hám olardıń analizi keltiriledi (1 ball)\n"+
               "2 - Sabaqta bazıda aktual máseleler kóterilip, jańa maǵlıwmatlar keltiriledi (0.9 ball)\n"+
               "3 - Sabaqta aktual máseleler ulıwma bayan etilmeydi, turmıs penen baylanıspaydı (0.6 ball)\n"+
               "4 - Sabaqta jańalıq joq, faktler gónergen (0.5 ball)\n"+
               "5 - Sabaqta jańa bilim bermeydi (0 ball)",
        Options = new()
        {
            "1 ball",
            "0.9 ball",
            "0.6 ball",
            "0.5 ball",
            "0 ball"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "3. Oqitiwshinin quliq-ádebi, tártibi hám jeke pazıyletleri haqqında ne aytasız?\n\n"+
               "1 - Mádeniyatlı, kishi peyil, tuwrı sózli (1 ball)\n"+
               "2 - Juda kishpeyil hám kewilli emes (0.7 ball)\n"+
               "3 - Turpayı, qatnas mádeniyatı pás (0.5 ball)\n"+
               "4 - Studentlerge biyparq (0.3 ball)\n"+
               "5 - Studentlerdi mensinbeydi, oǵada qopal (0 ball)",
        Options = new()
        {
            "1 ball",
            "0.7 ball",
            "0.5 ball",
            "0.3 ball",
            "0 ball"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "4. Oqitiwshinin studentlerdi bahalawındaǵı ádalatlılıǵı qanday?\n\n"+
               "1 - Ádalatlı, student bilimine saykes baha beredi (1 ball)\n"+
               "2 - Studenttin bilimi boyınsha reytingin biledi (0.7 ball)\n"+
               "3 - Geyde ádalatsızlıq etip turadı (0.5 ball)\n"+
               "4 - Bazı-bazıda ádalatsız, studentten máp kútedi (0.3 ball)\n"+
               "5 - Júdá ádalatsız hám studentten tek máp kútedi (0 ball)",
        Options = new()
        {
            "1 ball",
            "0.7 ball",
            "0.5 ball",
            "0.3 ball",
            "0 ball"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "5. Oqitiwshıda dámegóylik jaǵdayları ushırasqanba?\n\n"+
               "1 - Yaq, bunday halatlar bolmadı (2 ball)\n"+
               "2 - Bunday halat boyınsha xabardar emespen (1.7 ball)\n"+
               "3 - Bolıwı múmkin, biraq ózim ushıraspaǵan (0.9 ball)\n"+
               "4 - Dámegóylik etedi, gáplerinen sezilip turadı (0.3 ball)\n"+
               "5 - Ashıqshasına dámegóylik jaǵdayı bolǵan (-2 ball)",
        Options = new()
        {
            "2 ball",
            "1.7 ball",
            "0.9 ball",
            "0.3 ball",
            "-2 ball"
        },
        MaxBall = 2
    },
    new Question
    {
        Text = "6. Oqitiwshı sabaq processinde oqitıw metodların qanday dárejede qollanadı?\n\n"+
               "1 - Interaktiv oqitıw metodların jaqsı qollanadı, studentlerdi belsendi tartadı (1 ball)\n"+
               "2 - Bazıda qızıqlı metodlardı qollanadı, studentler menen pikir almasıp turadı (0.7 ball)\n"+
               "3 - Sabaq hámme waqıt bir formada, kóbinese lekciya túrinde ótedi (0.5 ball)\n"+
               "4 - Sabaq basında azǵana aytıladı, qalǵan waqıtta studentler óz-ózleri boladı (0.3 ball)\n"+
               "5 - Sabaqqa qatnassız, tema boyınsha hesh nárse aytpaydı (0 ball)",
        Options = new()
        {
            "1 ball",
            "0.7 ball",
            "0.5 ball",
            "0.3 ball",
            "0 ball"
        },
        MaxBall = 1
    }
};

    // O'qituvchilarni baholash - Rus tili
    public static List<Question> TeacherEvaluationRu = new()
{
    new Question
    {
        Text = "1. Как вы оцениваете уровень знаний и педагогическое мастерство преподавателя?\n\n"+
               "1 - Глубоко знает свой предмет, проводит содержательные, познавательные и интересные занятия (1 балл)\n"+
               "2 - Хорошо знает свой предмет, проводит содержательные, стандартные и интересные занятия (0.9 балла)\n"+
               "3 - Знает предмет на среднем уровне, использует только текст лекции (0.7 балла)\n"+
               "4 - Поверхностно знает предмет, занятия скучные и малосодержательные (0.5 балла)\n"+
               "5 - Не знает свой предмет, занятия непонятны и бессмысленны (0 баллов)",
        Options = new()
        {
            "1 балл",
            "0.9 балла",
            "0.7 балла",
            "0.5 балла",
            "0 баллов"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "2. Каково, по вашему мнению, качество проведения занятия преподавателем?\n\n"+
               "1 - Рассматриваются актуальные вопросы, приводятся новые интересные сведения и их анализ, связанные с реальной жизнью (1 балл)\n"+
               "2 - Иногда поднимаются актуальные вопросы и дается новая информация (0.9 балла)\n"+
               "3 - Актуальные вопросы не рассматриваются, отсутствует связь с практикой (0.6 балла)\n"+
               "4 - Отсутствует новизна, факты устарели (0.5 балла)\n"+
               "5 - Новые знания не предоставляются (0 баллов)",
        Options = new()
        {
            "1 балл",
            "0.9 балла",
            "0.6 балла",
            "0.5 балла",
            "0 баллов"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "3. Что вы можете сказать о манерах, поведении и личных качествах преподавателя?\n\n"+
               "1 - Культурный, вежливый, честный (1 балл)\n"+
               "2 - Недостаточно вежлив и тактичен (0.7 балла)\n"+
               "3 - Грубый, низкая культура общения (0.5 балла)\n"+
               "4 - Равнодушен к студентам (0.3 балла)\n"+
               "5 - Проявляет неуважение, чрезмерно груб (0 баллов)",
        Options = new()
        {
            "1 балл",
            "0.7 балла",
            "0.5 балла",
            "0.3 балла",
            "0 баллов"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "4. Объективность преподавателя при оценивании студентов\n\n"+
               "1 - Справедлив и объективно оценивает знания студентов (1 балл)\n"+
               "2 - Знает рейтинг студентов по уровню знаний (0.7 балла)\n"+
               "3 - Иногда проявляет необъективность (0.5 балла)\n"+
               "4 - Периодически необъективен и ожидает выгоды (0.3 балла)\n"+
               "5 - Крайне необъективен и преследует личную выгоду (0 баллов)",
        Options = new()
        {
            "1 балл",
            "0.7 балла",
            "0.5 балла",
            "0.3 балла",
            "0 баллов"
        },
        MaxBall = 1
    },
    new Question
    {
        Text = "5. Были ли замечены случаи вымогательства со стороны преподавателя?\n\n"+
               "1 - Нет, таких случаев не было (2 балла)\n"+
               "2 - Мне об этом неизвестно (1.7 балла)\n"+
               "3 - Возможно, но лично не сталкивался (0.9 балла)\n"+
               "4 - Присутствуют признаки вымогательства (0.3 балла)\n"+
               "5 - Имели место открытые случаи вымогательства (-2 балла)",
        Options = new()
        {
            "2 балла",
            "1.7 балла",
            "0.9 балла",
            "0.3 балла",
            "-2 балла"
        },
        MaxBall = 2
    },
    new Question
    {
        Text = "6. Какие методы обучения использует преподаватель в ходе занятия?\n\n"+
               "1 - Эффективно применяет интерактивные методы, активно вовлекает студентов (1 балл)\n"+
               "2 - Иногда использует интересные методы и обсуждает материал со студентами (0.7 балла)\n"+
               "3 - Занятия проходят однообразно, в форме лекции (0.5 балла)\n"+
               "4 - В начале кратко объясняет, далее студенты предоставлены сами себе (0.3 балла)\n"+
               "5 - Не ориентируется в теме и не проводит полноценное занятие (0 баллов)",
        Options = new()
        {
            "1 балл",
            "0.7 балла",
            "0.5 балла",
            "0.3 балла",
            "0 баллов"
        },
        MaxBall = 1
    }
};

}
