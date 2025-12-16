using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramSurveyBot.Data;
using TelegramSurveyBot.Models;
using Microsoft.EntityFrameworkCore;

namespace TelegramSurveyBot.Services;

public class BotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly AppDbContext _dbContext;
    private readonly Dictionary<long, UserState> _userStates = new();

    // Admin management methods
    private async Task<bool> IsAdminAsync(long telegramId)
    {
        return await _dbContext.Admins.AnyAsync(a => a.TelegramId == telegramId);
    }

    private async Task<List<Models.Admin>> GetAllAdminsAsync()
    {
        return await _dbContext.Admins.ToListAsync();
    }

    private async Task<bool> AddAdminAsync(long telegramId, long addedBy, Telegram.Bot.Types.User? userInfo = null)
    {
        // Check if already admin
        if (await IsAdminAsync(telegramId))
            return false;

        var admin = new Models.Admin
        {
            TelegramId = telegramId,
            Username = userInfo?.Username,
            FirstName = userInfo?.FirstName,
            LastName = userInfo?.LastName,
            AddedBy = addedBy,
            AddedAt = DateTime.UtcNow
        };

        await _dbContext.Admins.AddAsync(admin);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private async Task<bool> RemoveAdminAsync(long telegramId)
    {
        var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.TelegramId == telegramId);
        if (admin == null)
            return false;

        _dbContext.Admins.Remove(admin);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public BotService(string botToken)
    {
        _botClient = new TelegramBotClient(botToken);
        _dbContext = new AppDbContext();

        try
        {
            Console.WriteLine("[LOG] Database yaratilmoqda yoki tekshirilmoqda...");

            // Ensure database is created
            var created = _dbContext.Database.EnsureCreated();

            if (created)
            {
                Console.WriteLine("[LOG] ✅ Database muvaffaqiyatli yaratildi!");
            }
            else
            {
                Console.WriteLine("[LOG] ✅ Database allaqachon mavjud");
            }

            // Test database connection
            var canConnect = _dbContext.Database.CanConnect();
            Console.WriteLine($"[LOG] Database connection: {(canConnect ? "✅ Muvaffaqiyatli" : "❌ Xatolik")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[XATOLIK] Database yaratishda muammo: {ex.Message}");
            Console.WriteLine($"[XATOLIK] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Delete webhook to use long polling
        Console.WriteLine("[LOG] Webhook o'chirilmoqda...");
        await _botClient.DeleteWebhook(cancellationToken: cancellationToken);
        Console.WriteLine("[LOG] Webhook o'chirildi");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );

        var me = await _botClient.GetMe(cancellationToken);
        Console.WriteLine($"Bot @{me.Username} ishga tushdi!");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"[LOG] Update qabul qilindi: Type={update.Type}");

            if (update.Type == UpdateType.Message && update.Message != null)
            {
                if (update.Message.Contact != null)
                {
                    Console.WriteLine($"[LOG] Contact qabul qilindi: {update.Message.Contact.PhoneNumber}");
                    await HandleMessageAsync(update.Message, cancellationToken);
                }
                else if (update.Message.Text != null)
                {
                    Console.WriteLine($"[LOG] Text xabar qabul qilindi: {update.Message.Text}");
                    await HandleMessageAsync(update.Message, cancellationToken);
                }
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                Console.WriteLine($"[LOG] CallbackQuery qabul qilindi: {update.CallbackQuery.Data}");
                await HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[XATOLIK] HandleUpdateAsync: {ex.Message}");
            Console.WriteLine($"[XATOLIK] StackTrace: {ex.StackTrace}");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Xatolik: {exception.Message}");
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        Console.WriteLine($"[LOG] HandleMessageAsync boshlandi, ChatId: {chatId}");

        // Check if user exists
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);
        Console.WriteLine($"[LOG] Database'dan user topildi: {user != null}");

        // Handle /start command
        if (message.Text == "/start")
        {
            Console.WriteLine($"[LOG] /start buyrug'i qabul qilindi");

            if (user == null)
            {
                // New user - show language selection first
                Console.WriteLine($"[LOG] Yangi user, til tanlash oynasini ko'rsatish");
                await ShowLanguageSelectionAsync(chatId, cancellationToken);
            }
            else if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                // User exists but no phone number - ask for phone
                Console.WriteLine($"[LOG] User mavjud ammo telefon yo'q, telefon so'rash");
                await RequestPhoneNumberAsync(chatId, user.SelectedLanguage, cancellationToken);
            }
            else
            {
                // Existing user with phone - show main menu
                Console.WriteLine($"[LOG] Mavjud user, asosiy menyuni ko'rsatish");
                await ShowLanguageSelectionAsync(chatId, cancellationToken);
            }
            return;
        }

        // Handle Back button
        if (message.Text == "🔙 Orqaga" || message.Text == "🔙 Назад" || message.Text == "🔙 Артқa")
        {
            Console.WriteLine($"[LOG] Back button bosildi");

            // Check if admin is in special mode
            if (await IsAdminAsync(chatId) && _userStates.ContainsKey(chatId))
            {
                var state = _userStates[chatId];

                // If viewing statistics, go back to survey selection for stats
                if (state.State == "admin_stats_viewing")
                {
                    state.State = "admin_stats_survey_selection";
                    await ShowAdminSurveySelectionAsync(chatId, "stats", cancellationToken);
                    return;
                }

                // If in survey selection for stats, go back to main menu
                if (state.State == "admin_stats_survey_selection")
                {
                    _userStates.Remove(chatId);
                    await ShowMainMenuAsync(chatId, user, cancellationToken);
                    return;
                }

                // If in survey selection for participation, go back to main menu
                if (state.State == "admin_participate_survey_selection")
                {
                    _userStates.Remove(chatId);
                    await ShowMainMenuAsync(chatId, user, cancellationToken);
                    return;
                }

                // If in admin management menu, go back to main menu
                if (state.State == "admin_management_menu")
                {
                    _userStates.Remove(chatId);
                    await ShowMainMenuAsync(chatId, user, cancellationToken);
                    return;
                }

                // If in old admin_stats mode (backward compatibility)
                if (state.State == "admin_stats")
                {
                    _userStates.Remove(chatId);
                    await ShowMainMenuAsync(chatId, user, cancellationToken);
                    return;
                }
            }

            // Both regular user and admin from main menu - go back to language selection
            await ShowLanguageSelectionAsync(chatId, cancellationToken);
            return;
        }

        // Handle language selection from ReplyKeyboard
        if (message.Text == "🇺🇿 O'zbek" || message.Text == "🇷🇺 Русский" || message.Text == "Qaraqalpaq")
        {
            Console.WriteLine($"[LOG] Til tanlandi: {message.Text}");
            var language = message.Text switch
            {
                "🇺🇿 O'zbek" => "uz",
                "🇷🇺 Русский" => "ru",
                "Qaraqalpaq" => "kk",
                _ => "uz"
            };
            await HandleLanguageSelectionFromTextAsync(chatId, language, message.From!, cancellationToken);
            return;
        }

        // Handle admin buttons
        if (await IsAdminAsync(chatId))
        {
            if (message.Text == "✍️ So'rovnomada qatnashish" || message.Text == "✍️ Примите участие в опросе" || message.Text == "✍️ So'rawnamada qatnasıw")
            {
                Console.WriteLine($"[LOG] Admin - So'rovnomada qatnashish tanlandi");
                await ShowAdminSurveySelectionAsync(chatId, "participate", cancellationToken);
                return;
            }
            else if (message.Text == "📈 Statistikani ko'rish" || message.Text == "📈 Просмотреть статистику" || message.Text == "📈 Statistikanı kóriw")
            {
                Console.WriteLine($"[LOG] Admin - Statistikani ko'rish tanlandi");
                await ShowAdminSurveySelectionAsync(chatId, "stats", cancellationToken);
                return;
            }
            else if (message.Text == "👥 Adminlar ro'yxati" || message.Text == "👥 Список администраторов" || message.Text == "👥 Administratorlar dizimi")
            {
                Console.WriteLine($"[LOG] Admin - Adminlar ro'yxati tanlandi");
                await ShowAdminManagementMenuAsync(chatId, cancellationToken);
                return;
            }
        }

        // Handle survey selection from ReplyKeyboard (both admin and regular users)
        if (message.Text == "📝 Korrupsiya so'rovnomasi" ||
            message.Text == "📝 Опрос о коррупции" ||
            message.Text == "📝 Korrupsiya sorawnomasi" ||
            message.Text == "📊 O'qituvchilarni baholash" ||
            message.Text == "📊 Оценка преподавателей" ||
            message.Text == "📊 Oqıtıwshılardı bahalaw")
        {
            Console.WriteLine($"[LOG] So'rovnoma tanlandi: {message.Text}");

            var surveyType = message.Text == "📝 Korrupsiya so'rovnomasi"
                ? "corruption"
                : "teacher";

            // Check if admin is viewing stats or participating
            if (await IsAdminAsync(chatId) && _userStates.ContainsKey(chatId))
            {
                var state = _userStates[chatId];

                // If admin is in stats mode, show statistics
                if (state.State == "admin_stats_survey_selection" || state.State == "admin_stats")
                {
                    // Admin wants to see statistics
                    await ShowSurveyStatisticsAsync(chatId, surveyType, cancellationToken);
                    return;
                }

                // If admin is in participate mode, clear state and let them participate
                if (state.State == "admin_participate_survey_selection")
                {
                    _userStates.Remove(chatId);
                }
            }

            await HandleSurveySelectionFromTextAsync(chatId, surveyType, cancellationToken);
            return;
        }



        // Handle admin management buttons
        if (await IsAdminAsync(chatId))
        {
            if (message.Text == "➕ Admin qo'shish" || message.Text == "➕ Добавить администратора" || message.Text == "➕ Administrator qosıw")
            {
                Console.WriteLine($"[LOG] Admin qo'shish bosildi");
                await RequestAdminIdAsync(chatId, cancellationToken);
                return;
            }
            else if (message.Text == "📋 Adminlarni ko'rish" || message.Text == "📋 Показать администраторов" || message.Text == "📋 Administratorlardı kóriw")
            {
                Console.WriteLine($"[LOG] Adminlarni ko'rish bosildi");
                await ShowAdminListAsync(chatId, cancellationToken);
                return;
            }
            else if (message.Text == "❌ Bekor qilish" || message.Text == "❌ Отменить" || message.Text == "❌ Biykar etiw")
            {
                Console.WriteLine($"[LOG] Bekor qilish bosildi");
                _userStates.Remove(chatId);
                await ShowAdminManagementMenuAsync(chatId, cancellationToken);
                return;
            }
        }

        // Handle "Tugatish" button
        if (message.Text == "🛑 So'rovnomani tugatish" ||
            message.Text == "🛑 Завершить опрос" ||
            message.Text == "🛑 Sorawnomanı tamamlaw")
        {
            Console.WriteLine($"[LOG] Tugatish bosildi");
            await CancelSurveyAsync(chatId, cancellationToken);
            return;
        }

        // Handle phone number
        if (message.Contact != null)
        {
            Console.WriteLine($"[LOG] Telefon raqam qabul qilindi");
            await HandlePhoneNumberAsync(chatId, message, cancellationToken);
            return;
        }

        // Handle user state
        if (_userStates.ContainsKey(chatId))
        {
            var state = _userStates[chatId];
            Console.WriteLine($"[LOG] User state mavjud: {state.State}");

            if (state.State == "awaiting_admin_id" && message.Text != null)
            {
                Console.WriteLine($"[LOG] Admin ID input qabul qilindi");
                await HandleAdminIdInputAsync(chatId, message.Text, cancellationToken);
            }
            else if (state.State == "awaiting_text_input" && message.Text != null)
            {
                Console.WriteLine($"[LOG] Text input qabul qilindi");
                await HandleSurveyAnswerAsync(chatId, message.Text, state, cancellationToken);
            }
            else if (state.State == "awaiting_answer" && state.CurrentSurvey != null && message.Text != null)
            {
                await HandleSurveyAnswerAsync(chatId, message.Text, state, cancellationToken);
            }
        }
    }

    private async Task ShowLanguageSelectionAsync(long chatId, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] ShowLanguageSelectionAsync - ChatId: {chatId}");

        // Check if user exists to show their name
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);

        var greeting = user != null && !string.IsNullOrEmpty(user.FirstName)
            ? $"Iltimos, tilni tanlang {user.FirstName} {user.LastName ?? ""}!\n\n" +
              $"Пожалуйста, выберите язык {user.FirstName} {user.LastName ?? ""}!\n\n" +
              $"Tildi tańlań {user.FirstName} {user.LastName ?? ""}!"
            : "Assalomu alaykum! Iltimos, tilni tanlang:\n\n" +
              "Здравствуйте! Пожалуйста, выберите язык:\n\n" +
              "Sálem! Tildi tańlań:";

        await _botClient.SendMessage(
            chatId,
            greeting.Trim(),
            replyMarkup: GetLanguageKeyboard(),
            cancellationToken: cancellationToken
        );

        Console.WriteLine($"[LOG] Til tanlash xabari yuborildi");
    }

    private async Task RequestPhoneNumberAsync(long chatId, string language, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] RequestPhoneNumberAsync - ChatId: {chatId}, Language: {language}");

        var message = language switch
        {
            "uz" => "Ro'yxatdan o'tish uchun telefon raqamingizni yuboring:",
            "ru" => "Для регистрации отправьте свой номер телефона:",
            "kk" => "Dizimnen ótiw ushın telefon nomerinizdi jiberiń:",
            _ => "Ro'yxatdan o'tish uchun telefon raqamingizni yuboring:"
        };

        var buttonText = language switch
        {
            "uz" => "📱 Telefon raqamni yuborish",
            "ru" => "📱 Отправить номер телефона",
            "kk" => "📱 Telefon nomerin jiberiw",
            _ => "📱 Telefon raqamni yuborish"
        };

        var backButtonText = language switch
        {
            "uz" => "🔙 Orqaga",
            "ru" => "🔙 Назад",
            "kk" => "🔙 Артқa",
            _ => "🔙 Orqaga"
        };

        await _botClient.SendMessage(
            chatId,
            message,
            replyMarkup: new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { new KeyboardButton(buttonText) { RequestContact = true } },
                new KeyboardButton[] { backButtonText }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            },
            cancellationToken: cancellationToken
        );

        Console.WriteLine($"[LOG] Telefon so'rash xabari yuborildi");
    }

    private async Task HandlePhoneNumberAsync(long chatId, Message message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] HandlePhoneNumberAsync boshlandi - ChatId: {chatId}");

        var contact = message.Contact!;
        Console.WriteLine($"[LOG] Contact ma'lumotlari: Phone={contact.PhoneNumber}, Name={contact.FirstName}");

        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);

            if (user == null)
            {
                Console.WriteLine($"[LOG] User topilmadi, xatolik!");
                await _botClient.SendMessage(
                    chatId,
                    "Xatolik yuz berdi! Iltimos, /start buyrug'ini qayta yuboring.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            Console.WriteLine($"[LOG] User topildi, telefon raqamni yangilash");

            user.PhoneNumber = contact.PhoneNumber;
            user.Username = message.From?.Username;
            user.FirstName = contact.FirstName;
            user.LastName = contact.LastName;

            await _dbContext.SaveChangesAsync(cancellationToken);
            Console.WriteLine($"[LOG] Database yangilandi");

            var successMessage = user.SelectedLanguage switch
            {
                "uz" => "Ro'yxatdan o'tish muvaffaqiyatli! Endi so'rovnomani tanlang:",
                "ru" => "Регистрация успешна! Теперь выберите опрос:",
                "kk" => "Dizimnen ótiw tabıslı! Endi sorawnomanı tańlań:",
                _ => "Ro'yxatdan o'tish muvaffaqiyatli! Endi so'rovnomani tanlang:"
            };

            await _botClient.SendMessage(
                chatId,
                successMessage,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );

            Console.WriteLine($"[LOG] Muvaffaqiyat xabari yuborildi, asosiy menyuni ko'rsatish");

            await ShowMainMenuAsync(chatId, user, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[XATOLIK] HandlePhoneNumberAsync: {ex.Message}");
            Console.WriteLine($"[XATOLIK] StackTrace: {ex.StackTrace}");
        }
    }

    private ReplyKeyboardMarkup GetLanguageKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "🇺🇿 O'zbek", "🇷🇺 Русский" },
            new KeyboardButton[] { "Qaraqalpaq" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message!.MessageId;
        var data = callbackQuery.Data!;

        Console.WriteLine($"[LOG] HandleCallbackQueryAsync - ChatId: {chatId}, Data: {data}");

        if (data.StartsWith("lang_"))
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            await HandleLanguageSelectionAsync(chatId, data, callbackQuery.From, cancellationToken);
        }
        else if (data.StartsWith("survey_"))
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            await HandleSurveySelectionAsync(chatId, data, cancellationToken);
        }
        else if (data.StartsWith("ans_"))
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            // Delete the question message before showing next question
            try
            {
                await _botClient.DeleteMessage(chatId, messageId, cancellationToken);
                Console.WriteLine($"[LOG] Savol xabari o'chirildi - MessageId: {messageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[XATOLIK] Xabarni o'chirishda xatolik: {ex.Message}");
            }

            await HandleAnswerSelectionAsync(chatId, data, cancellationToken);
        }
        else if (data.StartsWith("multi_"))
        {
            await HandleMultiSelectToggleAsync(chatId, data, messageId, cancellationToken);
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        else if (data.StartsWith("save_"))
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            // Delete the question message before showing next question
            try
            {
                await _botClient.DeleteMessage(chatId, messageId, cancellationToken);
                Console.WriteLine($"[LOG] Multi-select savol xabari o'chirildi - MessageId: {messageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[XATOLIK] Xabarni o'chirishda xatolik: {ex.Message}");
            }

            await HandleMultiSelectSaveAsync(chatId, data, cancellationToken);
        }
    }

    private async Task HandleLanguageSelectionFromTextAsync(long chatId, string language, Telegram.Bot.Types.User from, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] HandleLanguageSelectionFromTextAsync boshlandi - ChatId: {chatId}, Language: {language}");

        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);

            if (user == null)
            {
                // Create new user
                Console.WriteLine($"[LOG] Yangi user yaratilmoqda");

                user = new Models.User
                {
                    Id = chatId,
                    SelectedLanguage = language,
                    Username = from.Username,
                    FirstName = from.FirstName,
                    LastName = from.LastName
                };

                await _dbContext.Users.AddAsync(user, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                Console.WriteLine($"[LOG] Yangi user yaratildi va saqlandi");

                // Ask for phone number
                await RequestPhoneNumberAsync(chatId, language, cancellationToken);
            }
            else
            {
                // Update existing user language
                Console.WriteLine($"[LOG] Mavjud user topildi, tilni yangilash");

                user.SelectedLanguage = language;
                await _dbContext.SaveChangesAsync(cancellationToken);

                Console.WriteLine($"[LOG] Til yangilandi");

                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    // Ask for phone number
                    Console.WriteLine($"[LOG] Telefon raqam yo'q, so'rash");
                    await RequestPhoneNumberAsync(chatId, language, cancellationToken);
                }
                else
                {
                    // Show main menu
                    Console.WriteLine($"[LOG] Telefon raqam mavjud, asosiy menyuni ko'rsatish");
                    await ShowMainMenuAsync(chatId, user, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[XATOLIK] HandleLanguageSelectionFromTextAsync: {ex.Message}");
            Console.WriteLine($"[XATOLIK] StackTrace: {ex.StackTrace}");
        }
    }

    private async Task HandleLanguageSelectionAsync(long chatId, string data, Telegram.Bot.Types.User from, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] HandleLanguageSelectionAsync boshlandi - ChatId: {chatId}, Data: {data}");

        var language = data.Replace("lang_", "");
        Console.WriteLine($"[LOG] Tanlangan til: {language}");

        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);

            if (user == null)
            {
                // Create new user
                Console.WriteLine($"[LOG] Yangi user yaratilmoqda");

                user = new Models.User
                {
                    Id = chatId,
                    SelectedLanguage = language,
                    Username = from.Username,
                    FirstName = from.FirstName,
                    LastName = from.LastName
                };

                await _dbContext.Users.AddAsync(user, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                Console.WriteLine($"[LOG] Yangi user yaratildi va saqlandi");

                // Ask for phone number
                await RequestPhoneNumberAsync(chatId, language, cancellationToken);
            }
            else
            {
                // Update existing user language
                Console.WriteLine($"[LOG] Mavjud user topildi, tilni yangilash");

                user.SelectedLanguage = language;
                await _dbContext.SaveChangesAsync(cancellationToken);

                Console.WriteLine($"[LOG] Til yangilandi");

                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    // Ask for phone number
                    Console.WriteLine($"[LOG] Telefon raqam yo'q, so'rash");
                    await RequestPhoneNumberAsync(chatId, language, cancellationToken);
                }
                else
                {
                    // Show main menu
                    Console.WriteLine($"[LOG] Telefon raqam mavjud, asosiy menyuni ko'rsatish");
                    await ShowLanguageSelectionAsync(chatId, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[XATOLIK] HandleLanguageSelectionAsync: {ex.Message}");
            Console.WriteLine($"[XATOLIK] StackTrace: {ex.StackTrace}");
        }
    }

    private async Task ShowMainMenuAsync(long chatId, Models.User user, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] ShowMainMenuAsync - ChatId: {chatId}, Language: {user.SelectedLanguage}");

        var selectTitle = user.SelectedLanguage switch
        {
            "uz" => "Admin Panel\n\nTanlang:",
            "ru" => "Панель администратора\n\nВыберите:",
            "kk" => "Admin Panel\n\nSaylań:",
            _=>"Admin Panel\n\nTanlang:"
        };

        var surveyTitle = user.SelectedLanguage switch
        {
            "uz" => "So'rovnomada qatnashish",
            "ru" => "Примите участие в опросе",
            "kk" => "So‘rawnamada qatnasıw",
            _ => "So'rovnomada qatnashish"
        };

        var seeStatisticTitle = user.SelectedLanguage switch
        {
            "uz" => "Statistikani ko'rish",
            "ru" => "Просмотреть статистику",
            "kk" => "Statistikanı kóriw",
            _ => "Statistikani ko'rish"
        };

        var backTitle = user.SelectedLanguage switch
        {
            "uz" => "Orqaga",
            "ru" => "Назад",
            "kk" => "Артқa",
            _ => "Orqaga"
        };

        var adminListTitle = user.SelectedLanguage switch
        {
            "uz" => "Adminlar ro'yxati",
            "ru" => "Список администраторов",
            "kk" => "Administratorlar dizimi",
            _ => "Adminlar ro'yxati"
        };

        // Check if user is admin - show admin menu with ReplyKeyboard
        if (await IsAdminAsync(chatId))
        {
            var adminMessage = $"👨‍💼 {selectTitle}";

            var adminKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { $"✍️ {surveyTitle}" },
                new KeyboardButton[] { $"📈 {seeStatisticTitle}" },
                new KeyboardButton[] { $"👥 {adminListTitle}" },
                new KeyboardButton[] { $"🔙 {backTitle}" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _botClient.SendMessage(
                chatId,
                adminMessage,
                replyMarkup: adminKeyboard,
                cancellationToken: cancellationToken
            );
            return;
        }

        var message = user.SelectedLanguage switch
        {
            "uz" => "So'rovnomani tanlang:",
            "ru" => "Выберите опрос:",
            "kk" => "Sorawnomanı tańlań:",
            _ => "So'rovnomani tanlang:"
        };

        var teacherButtonText = user.SelectedLanguage switch
        {
            "uz" => "📊 O'qituvchilarni baholash",
            "ru" => "📊 Оценка преподавателей",
            "kk" => "📊 Oqıtıwshılardı bahalaw",
            _ => "📊 O'qituvchilarni baholash"
        };

        var backButtonText = user.SelectedLanguage switch
        {
            "uz" => "🔙 Orqaga",
            "ru" => "🔙 Назад",
            "kk" => "🔙 Артқa",
            _ => "🔙 Orqaga"
        };

        ReplyKeyboardMarkup keyboard;

        if (user.SelectedLanguage == "uz")
        {
            // For Uzbek, show both surveys
            keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { teacherButtonText },
                new KeyboardButton[] { "📝 Korrupsiya so'rovnomasi" },
                new KeyboardButton[] { backButtonText }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }
        else
        {
            // For other languages, show only teacher evaluation
            keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { teacherButtonText },
                new KeyboardButton[] { backButtonText }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        await _botClient.SendMessage(
            chatId,
            message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleSurveySelectionAsync(long chatId, string data, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);
        if (user == null) return;

        var surveyType = data.Replace("survey_", "");

        List<Question> questions;
        string surveyName;

        if (surveyType == "corruption")
        {
            questions = SurveyData.CorruptionSurveyUz;
            surveyName = "corruption";
        }
        else // teacher
        {
            questions = user.SelectedLanguage switch
            {
                "uz" => SurveyData.TeacherEvaluationUz,
                "ru" => SurveyData.TeacherEvaluationRu,
                "kk" => SurveyData.TeacherEvaluationKk,
                _ => SurveyData.TeacherEvaluationUz
            };
            surveyName = "teacher_evaluation";
        }

        _userStates[chatId] = new UserState
        {
            State = "awaiting_answer",
            CurrentSurvey = surveyName,
            Questions = questions,
            CurrentQuestionIndex = 0
        };

        await SendQuestionAsync(chatId, _userStates[chatId], cancellationToken);
    }

    private async Task HandleSurveySelectionFromTextAsync(long chatId, string surveyType, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] HandleSurveySelectionFromTextAsync - ChatId: {chatId}, SurveyType: {surveyType}");

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);
        if (user == null) return;

        List<Question> questions;
        string surveyName;

        if (surveyType == "corruption")
        {
            questions = SurveyData.CorruptionSurveyUz;
            surveyName = "corruption";
        }
        else // teacher
        {
            questions = user.SelectedLanguage switch
            {
                "uz" => SurveyData.TeacherEvaluationUz,
                "ru" => SurveyData.TeacherEvaluationRu,
                "kk" => SurveyData.TeacherEvaluationKk,
                _ => SurveyData.TeacherEvaluationUz
            };
            surveyName = "teacher_evaluation";
        }

        _userStates[chatId] = new UserState
        {
            State = "awaiting_answer",
            CurrentSurvey = surveyName,
            Questions = questions,
            CurrentQuestionIndex = 0
        };

        // Show "Tugatish" button
        var cancelButtonText = user.SelectedLanguage switch
        {
            "uz" => "🛑 So'rovnomani tugatish",
            "ru" => "🛑 Завершить опрос",
            "kk" => "🛑 Sorawnomanı tamamlaw",
            _ => "🛑 So'rovnomani tugatish"
        };

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { cancelButtonText }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await _botClient.SendMessage(
            chatId,
            "📝",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );

        await SendQuestionAsync(chatId, _userStates[chatId], cancellationToken);
    }

    private async Task CancelSurveyAsync(long chatId, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] CancelSurveyAsync - ChatId: {chatId}");

        if (_userStates.ContainsKey(chatId))
        {
            _userStates.Remove(chatId);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);
        if (user == null) return;

        var message = user.SelectedLanguage switch
        {
            "uz" => "So'rovnoma bekor qilindi.",
            "ru" => "Опрос отменен.",
            "kk" => "Sorawnoma biykar etildi.",
            _ => "So'rovnoma bekor qilindi."
        };

        await _botClient.SendMessage(
            chatId,
            message,
            cancellationToken: cancellationToken
        );

        await ShowMainMenuAsync(chatId, user, cancellationToken);
    }

    private async Task SendQuestionAsync(long chatId, UserState state, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] SendQuestionAsync - ChatId: {chatId}, QuestionIndex: {state.CurrentQuestionIndex}");

        if (state.CurrentQuestionIndex >= state.Questions!.Count)
        {
            await CompleteSurveyAsync(chatId, cancellationToken);
            return;
        }

        var question = state.Questions[state.CurrentQuestionIndex];
        Console.WriteLine($"[LOG] Savol: {question.Text.Substring(0, Math.Min(50, question.Text.Length))}...");

        // Agar text input kerak bo'lsa
        if (question.RequireTextInput)
        {
            Console.WriteLine($"[LOG] Text input kerak");
            state.State = "awaiting_text_input";

            await _botClient.SendMessage(
                chatId,
                question.Text + "\n\n💬 Javobingizni yozing:",
                cancellationToken: cancellationToken
            );
            return;
        }

        // Agar multiple select bo'lsa
        if (question.AllowMultiple)
        {
            Console.WriteLine($"[LOG] Multi-select savol");
            state.State = "awaiting_multi_select";
            state.SelectedAnswers.Clear(); // Clear previous selections

            await SendMultiSelectQuestionAsync(chatId, state, question, cancellationToken);
            return;
        }

        // Oddiy single-select savol
        Console.WriteLine($"[LOG] Single-select savol");
        state.State = "awaiting_answer";

        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < question.Options.Count; i++)
        {
            // Truncate long option text to fit in button (max 64 characters for Telegram)
            var buttonText = question.Options[i].Length > 64
                ? question.Options[i].Substring(0, 61) + "..."
                : question.Options[i];

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    buttonText,
                    $"ans_{state.CurrentQuestionIndex}_{i}"
                )
            });
        }

        var keyboard = new InlineKeyboardMarkup(buttons);

        // Check if this is a teacher evaluation survey
        var isTeacherEvaluation = state.CurrentSurvey == "teacher_evaluation";
        var questionText = question.Text;

        if (isTeacherEvaluation && question.MaxBall > 0)
        {
            // Get user to determine language
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);

            // Format: Question text + question number + PS: Maximal X ball
            var questionNumberText = user?.SelectedLanguage switch
            {
                "uz" => $"{state.CurrentQuestionIndex + 1}-chi savol",
                "ru" => $"{state.CurrentQuestionIndex + 1}-й вопрос",
                "kk" => $"{state.CurrentQuestionIndex + 1}-shi sawal",
                _ => $"{state.CurrentQuestionIndex + 1}-chi savol"
            };

            var maxBallText = user?.SelectedLanguage switch
            {
                "uz" => $"PS: Maximal {question.MaxBall} ball",
                "ru" => $"PS: Максимальный {question.MaxBall} балл{(question.MaxBall > 1 ? "а" : "")}",
                "kk" => $"PS: Maximal {question.MaxBall} ball",
                _ => $"\nPS: Maximal {question.MaxBall} ball"
            };

            questionText = $"{questionNumberText}\n\n{question.Text}\n\n{maxBallText}";
        }

        await _botClient.SendMessage(
            chatId,
            questionText,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task SendMultiSelectQuestionAsync(long chatId, UserState state, Question question, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] SendMultiSelectQuestionAsync");

        var buttons = new List<InlineKeyboardButton[]>();

        // Har bir variant uchun checkbox button
        for (int i = 0; i < question.Options.Count; i++)
        {
            var isSelected = state.SelectedAnswers.Contains(i);
            var prefix = isSelected ? "☑️ " : "⬜ ";

            // Truncate long option text to fit in button (accounting for checkbox prefix)
            var optionText = question.Options[i];
            var maxLength = 61; // 64 - 3 for prefix
            var buttonText = optionText.Length > maxLength
                ? prefix + optionText.Substring(0, maxLength - 3) + "..."
                : prefix + optionText;

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    buttonText,
                    $"multi_{state.CurrentQuestionIndex}_{i}"
                )
            });
        }

        // "Saqlash" tugmasi - faqat bitta javob tanlansa yoqiladi
        var saveButtonText = state.SelectedAnswers.Count > 0
            ? "✅ Saqlash va davom etish"
            : "⚠️ Kamida bitta javobni tanlang";

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(
                saveButtonText,
                $"save_{state.CurrentQuestionIndex}"
            )
        });

        var keyboard = new InlineKeyboardMarkup(buttons);

        await _botClient.SendMessage(
            chatId,
            question.Text + "\n\n📌 Bir nechta javobni tanlashingiz mumkin:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleAnswerSelectionAsync(long chatId, string data, CancellationToken cancellationToken)
    {
        if (!_userStates.ContainsKey(chatId)) return;

        var state = _userStates[chatId];
        var parts = data.Split('_');
        var questionIndex = int.Parse(parts[1]);
        var answerIndex = int.Parse(parts[2]);

        var question = state.Questions![questionIndex];
        var answer = question.Options[answerIndex];

        // Save response to database
        var response = new UserResponse
        {
            UserId = chatId,
            SurveyType = state.CurrentSurvey!,
            QuestionNumber = questionIndex + 1,
            QuestionText = question.Text,
            AnswerText = answer
        };

        await _dbContext.UserResponses.AddAsync(response);
        await _dbContext.SaveChangesAsync();

        // Move to next question
        state.CurrentQuestionIndex++;
        await SendQuestionAsync(chatId, state, cancellationToken);
    }

    private async Task HandleSurveyAnswerAsync(long chatId, string text, UserState state, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] HandleSurveyAnswerAsync - Text input: {text}");

        var question = state.Questions![state.CurrentQuestionIndex];

        // Save response to database
        var response = new UserResponse
        {
            UserId = chatId,
            SurveyType = state.CurrentSurvey!,
            QuestionNumber = state.CurrentQuestionIndex + 1,
            QuestionText = question.Text,
            AnswerText = text
        };

        await _dbContext.UserResponses.AddAsync(response);
        await _dbContext.SaveChangesAsync();

        Console.WriteLine($"[LOG] Text javob saqlandi");

        // Move to next question
        state.CurrentQuestionIndex++;
        await SendQuestionAsync(chatId, state, cancellationToken);
    }

    private async Task HandleMultiSelectToggleAsync(long chatId, string data, int messageId, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] HandleMultiSelectToggleAsync - Data: {data}, ChatId: {chatId}");

        if (!_userStates.ContainsKey(chatId))
        {
            Console.WriteLine($"[XATOLIK] UserState topilmadi! ChatId: {chatId}");
            Console.WriteLine($"[LOG] Mavjud userStates keys: {string.Join(", ", _userStates.Keys)}");
            return;
        }

        var state = _userStates[chatId];
        Console.WriteLine($"[LOG] State topildi: QuestionIndex={state.CurrentQuestionIndex}, State={state.State}");
        var parts = data.Split('_');
        var questionIndex = int.Parse(parts[1]);
        var answerIndex = int.Parse(parts[2]);

        Console.WriteLine($"[LOG] Toggle answer index: {answerIndex}");

        // Toggle selection
        if (state.SelectedAnswers.Contains(answerIndex))
        {
            state.SelectedAnswers.Remove(answerIndex);
            Console.WriteLine($"[LOG] Removed from selection");
        }
        else
        {
            state.SelectedAnswers.Add(answerIndex);
            Console.WriteLine($"[LOG] Added to selection");
        }

        Console.WriteLine($"[LOG] Total selected: {state.SelectedAnswers.Count}");

        // Update keyboard
        var question = state.Questions![questionIndex];
        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < question.Options.Count; i++)
        {
            var isSelected = state.SelectedAnswers.Contains(i);
            var prefix = isSelected ? "☑️ " : "⬜ ";

            // Truncate long option text to fit in button (accounting for checkbox prefix)
            var optionText = question.Options[i];
            var maxLength = 61; // 64 - 3 for prefix
            var buttonText = optionText.Length > maxLength
                ? prefix + optionText.Substring(0, maxLength - 3) + "..."
                : prefix + optionText;

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    buttonText,
                    $"multi_{questionIndex}_{i}"
                )
            });
        }

        var saveButtonText = state.SelectedAnswers.Count > 0
            ? "✅ Saqlash va davom etish"
            : "⚠️ Kamida bitta javobni tanlang";

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(
                saveButtonText,
                $"save_{questionIndex}"
            )
        });

        var keyboard = new InlineKeyboardMarkup(buttons);

        try
        {
            await _botClient.EditMessageReplyMarkup(
                chatId,
                messageId,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[XATOLIK] EditMessageReplyMarkup: {ex.Message}");
        }
    }

    private async Task HandleMultiSelectSaveAsync(long chatId, string data, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] HandleMultiSelectSaveAsync - ChatId: {chatId}, Data: {data}");

        if (!_userStates.ContainsKey(chatId))
        {
            Console.WriteLine($"[XATOLIK] UserState topilmadi! ChatId: {chatId}");
            Console.WriteLine($"[LOG] Mavjud userStates keys: {string.Join(", ", _userStates.Keys)}");
            return;
        }

        var state = _userStates[chatId];
        Console.WriteLine($"[LOG] State topildi. Selected answers count: {state.SelectedAnswers.Count}");

        if (state.SelectedAnswers.Count == 0)
        {
            Console.WriteLine($"[LOG] Hech qanday javob tanlanmagan!");
            return;
        }

        var question = state.Questions![state.CurrentQuestionIndex];

        // Tanlangan javoblarni stringga aylantirish
        var selectedOptions = state.SelectedAnswers
            .OrderBy(x => x)
            .Select(i => question.Options[i])
            .ToList();

        var answerText = string.Join("; ", selectedOptions);
        Console.WriteLine($"[LOG] Tanlangan javoblar: {answerText}");

        // Save to database
        var response = new UserResponse
        {
            UserId = chatId,
            SurveyType = state.CurrentSurvey!,
            QuestionNumber = state.CurrentQuestionIndex + 1,
            QuestionText = question.Text,
            AnswerText = answerText
        };

        await _dbContext.UserResponses.AddAsync(response);
        await _dbContext.SaveChangesAsync();

        Console.WriteLine($"[LOG] Multi-select javob saqlandi");

        // Clear selections and move to next question
        state.SelectedAnswers.Clear();
        state.CurrentQuestionIndex++;
        await SendQuestionAsync(chatId, state, cancellationToken);
    }

    private async Task CompleteSurveyAsync(long chatId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);

        if (!_userStates.ContainsKey(chatId))
        {
            if (user != null)
            {
                await ShowMainMenuAsync(chatId, user, cancellationToken);
            }
            return;
        }

        var state = _userStates[chatId];
        var surveyType = state.CurrentSurvey;

        // Get survey name based on type and language
        var surveyName = surveyType switch
        {
            "corruption" => user?.SelectedLanguage switch
            {
                "uz" => "📝 Korrupsiya so'rovnomasi",
                "ru" => "📝 Опрос о коррупции",
                "kk" => "📝 Korrupsiya sorawnomasi",
                _ => "📝 Korrupsiya so'rovnomasi"
            },
            "teacher_evaluation" => user?.SelectedLanguage switch
            {
                "uz" => "📊 O'qituvchilarni baholash",
                "ru" => "📊 Оценка преподавателей",
                "kk" => "📊 Oqıtıwshılardı bahalaw",
                _ => "📊 O'qituvchilarni baholash"
            },
            _ => user?.SelectedLanguage switch
            {
                "uz" => "📋 So'rovnoma",
                "ru" => "📋 Опрос",
                "kk" => "📋 Sorawnoma",
                _ => "📋 So'rovnoma"
            }
        };

        var compatedMessage = user?.SelectedLanguage switch
        {
            "uz" => "Yakunlandi!",
            "ru" => "Завершенный!",
            "kk" => "Juwmaqlandı!",
            _ => "Yakunlandi!"
        };

        var thankYouMessage = user?.SelectedLanguage switch
        {
            "uz" => "So'rovnomani to'ldirib, vaqtingizni ajratganingiz uchun katta rahmat! Sizning fikringiz biz uchun juda muhim.",
            "ru" => "Большое спасибо за то, что заполнили опрос и уделили свое время! Ваше мнение очень важно для нас.",
            "kk" => "Sorawnomanı toltırıp, waqtınızdı ajıratqanınız ushın úlken raxmet! Sizin pikiriiniz biz ushın júdá mańızlı.",
            _ => "So'rovnomani to'ldirib, vaqtingizni ajratganingiz uchun katta rahmat! Sizning fikringiz biz uchun juda muhim."
        };

        var completionMessage = $"{surveyName}\n\n✅ {compatedMessage}\n\n{thankYouMessage}";

        await _botClient.SendMessage(
            chatId,
            completionMessage,
            cancellationToken: cancellationToken
        );

        _userStates.Remove(chatId);

        // Show main menu again
        if (user != null)
        {
            await ShowMainMenuAsync(chatId, user, cancellationToken);
        }
    }

    // ==================== ADMIN PANEL METHODS ====================

    private async Task ShowAdminSurveySelectionAsync(long chatId, string mode, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] ShowAdminSurveySelectionAsync - Mode: {mode}");
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);

        var selectSurveyTitle = user.SelectedLanguage switch
        {
            "uz" => "Admin Panel - Statistika\n\nSo'rovnomani tanlang:",
            "ru" => "Панель администратора - Статистика\n\nВыберите опрос:",
            "kk" => "Admin Panel – Statistika\n\nSo‘rawnamanı saylań:",
            _ => "Admin Panel - Statistika\n\nSo'rovnomani tanlang:"
        };

        var chooseSurvey = user.SelectedLanguage switch
        {
            "uz" => "Admin Panel - Qatnashish\n\nSo'rovnomani tanlang:",
            "ru" => "Панель администратора - Участие\n\nВыберите опрос:",
            "kk" => "Admin Panel – Qatnasıw\n\nSo‘rawnamanı saylań:",
            _ => "Admin Panel - Qatnashish\n\nSo'rovnomani tanlang:"
        };

        var teacherButtonText = user.SelectedLanguage switch
        {
            "uz" => "O'qituvchilarni baholash",
            "ru" => "Оценка преподавателей",
            "kk" => "Oqıtıwshılardı bahalaw",
            _ => "O'qituvchilarni baholash"
        };

        var corruption = user?.SelectedLanguage switch
        {
            "uz" => "Korrupsiya so'rovnomasi",
            "ru" => "Опрос о коррупции",
            "kk" => "Korrupsiya sorawnomasi",
            _ => "Korrupsiya so'rovnomasi"
        };

        var backTitle = user.SelectedLanguage switch
        {
            "uz" => "Orqaga",
            "ru" => "Назад",
            "kk" => "Артқa",
            _ => "Orqaga"
        };

        var message = mode == "stats"
            ? $"👨‍💼 {selectSurveyTitle}"
            : $"👨‍💼 {chooseSurvey}";

        ReplyKeyboardMarkup keyboard;

        // For participation mode, respect language setting (corruption survey only in Uzbek)
        if (mode == "participate" && user.SelectedLanguage != "uz")
        {
            // For non-Uzbek languages, show only teacher evaluation
            keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { $"📊 {teacherButtonText}" },
                new KeyboardButton[] { $"🔙 {backTitle}" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }
        else
        {
            // For stats mode OR Uzbek language in participate mode, show both surveys
            keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { $"📊 {teacherButtonText}" },
                new KeyboardButton[] { $"📝 {corruption}" },
                new KeyboardButton[] { $"🔙 {backTitle}" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        // Set admin state based on mode
        if (mode == "stats")
        {
            if (!_userStates.ContainsKey(chatId))
            {
                _userStates[chatId] = new UserState();
            }
            _userStates[chatId].State = "admin_stats_survey_selection";
        }
        else if (mode == "participate")
        {
            if (!_userStates.ContainsKey(chatId))
            {
                _userStates[chatId] = new UserState();
            }
            _userStates[chatId].State = "admin_participate_survey_selection";
        }

        await _botClient.SendMessage(
            chatId,
            message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task ShowSurveyStatisticsAsync(long chatId, string surveyType, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] ShowSurveyStatisticsAsync - Survey: {surveyType}");

        try
        {
            var surveyName = surveyType == "teacher" ? "teacher_evaluation" : "corruption";
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);


            // Get all responses for this survey
            var responses = await _dbContext.UserResponses
                .Where(r => r.SurveyType == surveyName)
                .ToListAsync(cancellationToken);

            // Get unique users who participated
            var uniqueUsers = responses.Select(r => r.UserId).Distinct().Count();

            // Count total responses
            var totalResponses = responses.Count;

            // Calculate positive and negative responses
            int positiveCount = 0;
            int negativeCount = 0;

            foreach (var response in responses)
            {
                // Check if response is positive or negative based on answer
                var answer = response.AnswerText.ToLower();

                // For teacher evaluation - check ball values
                if (surveyName == "teacher_evaluation")
                {
                    if (answer.Contains("1 ball") || answer.Contains("0.9 ball") || answer.Contains("0.7 ball") ||
                        answer.Contains("2 ball") || answer.Contains("1.7 ball"))
                    {
                        positiveCount++;
                    }
                    else if (answer.Contains("0 ball") || answer.Contains("0.3 ball") || answer.Contains("0.5 ball") ||
                             answer.Contains("-2 ball"))
                    {
                        negativeCount++;
                    }
                }
                // For corruption survey - check positive/negative indicators
                else
                {
                    if (answer.Contains("yo'q") || answer.Contains("bilmayman") || answer.Contains("bunday holatlar mavjud emas") ||
                        answer.Contains("duch kelmaganman") || answer.Contains("ha, tayyorman"))
                    {
                        positiveCount++;
                    }
                    else if (answer.Contains("xa") || answer.Contains("ha") || answer.Contains("bo'ladi") ||
                             answer.Contains("mavjud") || answer.Contains("guvoh"))
                    {
                        negativeCount++;
                    }
                }
            }
            var teachersRating = user.SelectedLanguage switch
            {
                "uz" => "O'qituvchilarni baholash",
                "ru" => "Оценка преподавателей",
                "kk" => "Oqitiwshılardı bahalaw",
                _ => "O'qituvchilarni baholash"
            };
            var corruptionQuestion = user.SelectedLanguage switch
            {
                "uz" => "Korrupsiya so'rovnomasi",
                "ru" => "Антикоррупционный опрос",
                "kk" => "Korrupciyaǵa qarsı so‘rawnama",
                _ => "Korrupsiya so'rovnomasi"
            };

            var adminPanel = user.SelectedLanguage switch
            {
                "uz" => "Admin Panel - Statistika",
                "ru" => "Панель администратора – Статистика",
                "kk" => "Admin Panel – Statistika",
                _ => "Admin Panel - Statistika"
            };
            var totallyInfo = user.SelectedLanguage switch
            {
                "uz" => "Umumiy ma'lumotlar:",
                "ru" => "Общая информация:",
                "kk" => "Umumiy maǵlıwmatlar:",
                _ => "Umumiy ma'lumotlar"
            };

            var participants = user.SelectedLanguage switch
            {
                "uz" => "Ishtirokchilar soni:",
                "ru" => "Количество участников:",
                "kk" => "Qatnasıwshılar sanı:",
                _ => "Ishtirokchilar soni:"
            };

            var totalAnswers = user.SelectedLanguage switch
            {
                "uz" => "Jami javoblar:",
                "ru" => "Всего ответов:",
                "kk" => "Jami juwaplar sanı:",
                _ => "Jami javoblar:"
            };

            var analysis = user.SelectedLanguage switch
            {
                "uz" => "Javoblar tahlili:",
                "ru" => "Анализ ответов:",
                "kk" => "Juwaplar taldawı:",
                _ => "Javoblar tahlili:"
            };

            var positive = user.SelectedLanguage switch
            {
                "uz" => "Ijobiy javoblar:",
                "ru" => "Положительные ответы:",
                "kk" => "Oń juwaplar:",
                _ => "Ijobiy javoblar:"
            };

            var negative = user.SelectedLanguage switch
            {
                "uz" => "Salbiy javoblar:",
                "ru" => "Отрицательные ответы:",
                "kk" => "Teris juwaplar:",
                _ => "Salbiy javoblar:"
            };

            var other = user.SelectedLanguage switch
            {
                "uz" => "Boshqa:",
                "ru" => "Прочие:",
                "kk" => "Basqa:",
                _ => "Boshqa"
            };

            var pieces = user.SelectedLanguage switch
            {
                "uz" => "ta",
                "ru" => "шт",
                "kk" => "dana",
                _ => "ta"
            };
            var surveyTitle = surveyType == "teacher"
                ? $"📊 {teachersRating}"
                : $"📝 {corruptionQuestion}";

            var statsMessage = $"👨‍💼 {adminPanel}\n\n" +
                             $"{surveyTitle}\n\n" +
                             $"📊 {totallyInfo}\n" +
                             $"━━━━━━━━━━━━━━━━\n" +
                             $"👥 {participants} {uniqueUsers} {pieces}\n" +
                             $"💬 {totalAnswers} {totalResponses} {pieces}\n\n" +
                             $"📈 {analysis}\n" +
                             $"━━━━━━━━━━━━━━━━\n" +
                             $"✅ {positive} {positiveCount} {pieces} ({(totalResponses > 0 ? (positiveCount * 100.0 / totalResponses).ToString("F1") : "0")}%)\n" +
                             $"❌ {negative} {negativeCount} {pieces} ({(totalResponses > 0 ? (negativeCount * 100.0 / totalResponses).ToString("F1") : "0")}%)\n" +
                             $"⚪ {other} {totalResponses - positiveCount - negativeCount} {pieces}";
            var backButtonText = user.SelectedLanguage switch
            {
                "uz" => "🔙 Orqaga",
                "ru" => "🔙 Назад",
                "kk" => "🔙 Артқa",
                _ => "🔙 Orqaga"
            };
            // Show back button with ReplyKeyboard
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { $"{backButtonText}" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _botClient.SendMessage(
                chatId,
                statsMessage,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );

            // Keep admin_stats state so user can view other survey stats
            // State will be cleared when user goes back
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[XATOLIK] ShowSurveyStatisticsAsync: {ex.Message}");

            await _botClient.SendMessage(
                chatId,
                "❌ Statistikani yuklashda xatolik yuz berdi!",
                cancellationToken: cancellationToken
            );
        }
    }

    // ==================== ADMIN MANAGEMENT METHODS ====================

    private async Task ShowAdminManagementMenuAsync(long chatId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);
        if (user == null) return;

        var title = user.SelectedLanguage switch
        {
            "uz" => "👥 Adminlar ro'yxati\n\nTanlang:",
            "ru" => "👥 Список администраторов\n\nВыберите:",
            "kk" => "👥 Administratorlar dizimi\n\nSaylań:",
            _ => "👥 Adminlar ro'yxati\n\nTanlang:"
        };

        var addAdminText = user.SelectedLanguage switch
        {
            "uz" => "➕ Admin qo'shish",
            "ru" => "➕ Добавить администратора",
            "kk" => "➕ Administrator qosıw",
            _ => "➕ Admin qo'shish"
        };

        var listAdminsText = user.SelectedLanguage switch
        {
            "uz" => "📋 Adminlarni ko'rish",
            "ru" => "📋 Показать администраторов",
            "kk" => "📋 Administratorlardı kóriw",
            _ => "📋 Adminlarni ko'rish"
        };

        var backText = user.SelectedLanguage switch
        {
            "uz" => "🔙 Orqaga",
            "ru" => "🔙 Назад",
            "kk" => "🔙 Артқa",
            _ => "🔙 Orqaga"
        };

        // Set state for admin management
        if (!_userStates.ContainsKey(chatId))
        {
            _userStates[chatId] = new UserState();
        }
        _userStates[chatId].State = "admin_management_menu";

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { addAdminText },
            new KeyboardButton[] { listAdminsText },
            new KeyboardButton[] { backText }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await _botClient.SendMessage(
            chatId,
            title,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task ShowAdminListAsync(long chatId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);
        if (user == null) return;

        var admins = await GetAllAdminsAsync();

        var title = user.SelectedLanguage switch
        {
            "uz" => "👥 Adminlar ro'yxati:",
            "ru" => "👥 Список администраторов:",
            "kk" => "👥 Administratorlar dizimi:",
            _ => "👥 Adminlar ro'yxati:"
        };

        var message = $"{title}\n\n";

        foreach (var admin in admins)
        {
            var name = !string.IsNullOrEmpty(admin.FirstName)
                ? $"{admin.FirstName} {admin.LastName ?? ""}".Trim()
                : "N/A";
            var username = !string.IsNullOrEmpty(admin.Username) ? $"@{admin.Username}" : "";
            message += $"👤 {name} {username}\n";
            message += $"   ID: {admin.TelegramId}\n";
            message += $"   {admin.AddedAt:dd.MM.yyyy HH:mm}\n\n";
        }

        var backText = user.SelectedLanguage switch
        {
            "uz" => "🔙 Orqaga",
            "ru" => "🔙 Назад",
            "kk" => "🔙 Артқa",
            _ => "🔙 Orqaga"
        };

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { backText }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await _botClient.SendMessage(
            chatId,
            message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task RequestAdminIdAsync(long chatId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);
        if (user == null) return;

        var message = user.SelectedLanguage switch
        {
            "uz" => "📝 Yangi admin qo'shish\n\nQo'shmoqchi bo'lgan adminning Telegram ID raqamini kiriting (9-10 xonali son):",
            "ru" => "📝 Добавить нового администратора\n\nВведите Telegram ID администратора, которого хотите добавить (9-10 значное число):",
            "kk" => "📝 Jańa administrator qosıw\n\nQosıwshı bolǵan administratordıń Telegram ID nomerini kiritiń (9-10 sanli):",
            _ => "📝 Yangi admin qo'shish\n\nQo'shmoqchi bo'lgan adminning Telegram ID raqamini kiriting (9-10 xonali son):"
        };

        var cancelText = user.SelectedLanguage switch
        {
            "uz" => "❌ Bekor qilish",
            "ru" => "❌ Отменить",
            "kk" => "❌ Biykar etiw",
            _ => "❌ Bekor qilish"
        };

        // Set state to await admin ID
        if (!_userStates.ContainsKey(chatId))
        {
            _userStates[chatId] = new UserState();
        }
        _userStates[chatId].State = "awaiting_admin_id";

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { cancelText }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await _botClient.SendMessage(
            chatId,
            message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleAdminIdInputAsync(long chatId, string input, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);
        if (user == null) return;

        // Validate input
        if (!long.TryParse(input, out long adminId) || input.Length < 9 || input.Length > 10)
        {
            var errorMessage = user.SelectedLanguage switch
            {
                "uz" => "❌ Noto'g'ri format! Telegram ID 9-10 xonali son bo'lishi kerak. Qaytadan kiriting:",
                "ru" => "❌ Неверный формат! Telegram ID должен быть 9-10 значным числом. Попробуйте снова:",
                "kk" => "❌ Nadurıs format! Telegram ID 9-10 sanli bolıwı kerek. Qayta kiritiń:",
                _ => "❌ Noto'g'ri format! Telegram ID 9-10 xonali son bo'lishi kerak. Qaytadan kiriting:"
            };

            await _botClient.SendMessage(
                chatId,
                errorMessage,
                cancellationToken: cancellationToken
            );
            return;
        }

        // Check if already admin
        if (await IsAdminAsync(adminId))
        {
            var alreadyAdminMessage = user.SelectedLanguage switch
            {
                "uz" => "⚠️ Bu foydalanuvchi allaqachon admin!",
                "ru" => "⚠️ Этот пользователь уже является администратором!",
                "kk" => "⚠️ Bul paydalanıwshı aldınnan administrator!",
                _ => "⚠️ Bu foydalanuvchi allaqachon admin!"
            };

            await _botClient.SendMessage(
                chatId,
                alreadyAdminMessage,
                cancellationToken: cancellationToken
            );

            // Clear state and go back to admin management menu
            _userStates.Remove(chatId);
            await ShowAdminManagementMenuAsync(chatId, cancellationToken);
            return;
        }

        // Add admin
        var success = await AddAdminAsync(adminId, chatId);

        string resultMessage;
        if (success)
        {
            resultMessage = user.SelectedLanguage switch
            {
                "uz" => $"✅ Admin muvaffaqiyatli qo'shildi!\nTelegram ID: {adminId}",
                "ru" => $"✅ Администратор успешно добавлен!\nTelegram ID: {adminId}",
                "kk" => $"✅ Administrator tabıslı qosıldı!\nTelegram ID: {adminId}",
                _ => $"✅ Admin muvaffaqiyatli qo'shildi!\nTelegram ID: {adminId}"
            };
        }
        else
        {
            resultMessage = user.SelectedLanguage switch
            {
                "uz" => "❌ Admin qo'shishda xatolik yuz berdi!",
                "ru" => "❌ Произошла ошибка при добавлении администратора!",
                "kk" => "❌ Administrator qosıwda qátelik júz berdi!",
                _ => "❌ Admin qo'shishda xatolik yuz berdi!"
            };
        }

        await _botClient.SendMessage(
            chatId,
            resultMessage,
            cancellationToken: cancellationToken
        );

        // Clear state and go back to admin management menu
        _userStates.Remove(chatId);
        await ShowAdminManagementMenuAsync(chatId, cancellationToken);
    }
}

public class UserState
{
    public string State { get; set; } = "";
    public string? CurrentSurvey { get; set; }
    public List<Question>? Questions { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public List<int> SelectedAnswers { get; set; } = new(); // Multi-select uchun tanlangan javoblar
}
