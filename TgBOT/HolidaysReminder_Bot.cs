using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using TgBOT.Models;
using Npgsql;
using static TgBOT.User;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace TgBOT
{
    public class HolidaysReminder_Bot
    {
        static TelegramBotClient botClient = new TelegramBotClient("6159861449:AAHMy15J5CtPlA6A0XEO8XbdMwfAxWZ-ouI");
        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Бот {botMe.Username} почав працювати");

        }
        public Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Сталася помилка в API боту Telegram:\n {apiRequestException.ErrorCode}" +
                $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            return Task.CompletedTask;
        }
        public async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(botClient, update);
            }

        }
        private Dictionary<long, string> step = new Dictionary<long, string>();
        public async Task HandlerMessageAsync(ITelegramBotClient botClient, Update update)
        {
            var message = update.Message;
            if (!step.ContainsKey(message.Chat.Id))
            {
                step.Add(message.Chat.Id, "default");
            }
            switch (step[message.Chat.Id])
            {
                case "/code":
                    await GetPublicHolidaysWithCode(message.Text);
                    break;
                case "userEvent":
                    await GetUserEvent(message.Text);
                    break;
                case "userUpdateEvent":
                    await UpdateUserEvent(message.Text);
                    break;
                case "deleteEvent":
                    await DeleteEvent(message.Text);
                    break;
                case "/delete":
                    await DeleteEvent(message.Text);
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Ваша подiя була виделена");
                    break;
                default:
                    break;
            }
            switch (message.Text)
            {
                case "/start":
                    step[message.Chat.Id] = "/start";
                    break;
                case "/keyboard":
                    step[message.Chat.Id] = "/keyboard";
                    break;
                case "🎉 Список свят":
                    step[message.Chat.Id] = "holidays";
                    break;
                case "/code":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Впишіть код країни\n Приклад: UA");
                    step[message.Chat.Id] = "/code";
                    break;
                case "🏳️ Перелiк доступних країн":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Список країн та їх код");
                    step[message.Chat.Id] = "Countries";
                    break;
                case "📝 Створити свою подiю":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Створіть свою подію у чотири кроки:\n" +
                        "1) Напишіть дату\n" +
                        "2) Напишіть назву\n" +
                        "3) Напишіть нотатки до події\n" +
                        "4) Відправте все одним повідомленням, як показано в прикладі\n" +
                        "Приклад:\n" +
                        "12.06.2023\n" +
                        "День Народження\n" +
                        "Купити торт");
                    step[message.Chat.Id] = "userEvent";
                    break;
                case "✍️ Змiнити свою подiю":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Щоб змінити свою подію, вам потрібно вказати назву події, яку " +
                        "ви хочете змінити і дотримуватись вказівок, що були зазначені в команді 📝 Створити свою подiю\n" +
                        "Приклад:\n" +
                        "12.06.2023\n" +
                        "День Народження\n" +
                        "Купити торт і свічки");
                    step[message.Chat.Id] = "userUpdateEvent";
                    break;
                case "🗒 Переглянути список своїх подiй":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Всі ваші створені події:\n");
                    step[message.Chat.Id] = "lookAllEvents";
                    break;
                case "🗑 Видалити свою подiю":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Оберіть цю команду, щоб продовжити роботу /delete \n" +
                        "Далі вкажіть назву події, що ви хочете видалити");
                    step[message.Chat.Id] = "deleteEvent";
                    break;
                case "/delete":
                    step[message.Chat.Id] = "/delete";
                    break;

                case "❓Чи є сьогодні моя подія":
                    step[message.Chat.Id] = "❓Чи є сьогодні моя подія";
                    break;
            }
            switch (step[message.Chat.Id]) //не чекає відповіді
            {
                case "/start":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Бот нагадування почав працювати. Дотримуйтесь прикладів " +
                        "та вказівок для коректної роботи боту ☺️\n" +
                        "Виберіть команду /keyboard щоб відкрити меню\n");
                    break;
                case "/keyboard":
                    ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(
                        new[]
                        {
                new KeyboardButton[] { "🏳️ Перелiк доступних країн"},
                new KeyboardButton[] { "🎉 Список свят", "📝 Створити свою подiю"},
                new KeyboardButton[] { "✍️ Змiнити свою подiю", "🗑 Видалити свою подiю"},
                new KeyboardButton[] { "🗒 Переглянути список своїх подiй", "❓Чи є сьогодні моя подія" }
                        }
                    )
                    {
                        ResizeKeyboard = true
                    };
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Оберіть пункт меню:", replyMarkup: replyKeyboardMarkup);
                    break;
                case "holidays":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Оберіть цю команду, щоб продовжити роботу /code");
                    break;
                case "Countries":
                    await GetCountries();
                    break;
                case "lookAllEvents":
                    await GetAllUserEvents(message.Chat.Id);
                    break;
                case "❌ Delete my review":
                    await DeleteEvent(message.Text);
                    break;
                case "❓Чи є сьогодні моя подія":
                    await CheckTodayEvent(message.Chat.Id);
                    break;
            }
            async Task GetPublicHolidaysWithCode(string countryCode)
            {
                User userHolidays = new User();
                var holidays = await userHolidays.GetPublicHolidaysAsync(countryCode);
                foreach (var item in holidays)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Дата: {item.date}\n" +
                     $"Мiсцева назва: {item.localName}\n" +
                     $"Назва: {item.name}\n" +
                     $"Код країни: {item.countryCode}\n");
                }
                step[message.Chat.Id] = "";

            }
            async Task GetCountries()
            {
                User userCountries = new User();
                var result = await userCountries.GetCountriesAsync();
                int maxMessageLength = 4096;
                StringBuilder messageBuilder = new StringBuilder();
                long chatId = message.Chat.Id;
                foreach (var item in result)
                {
                    StringBuilder countryInfoBuilder = new StringBuilder();
                    countryInfoBuilder.AppendLine($"Код країни: {item.countryCode}");

                    string countryName = item.name;
                    if (countryName.Contains("Russia"))
                    {
                        countryName = countryName.Replace("Russia", "Terrorist country");
                    }
                    countryInfoBuilder.AppendLine($"Назва країни: {countryName}\n");
                    if (messageBuilder.Length + countryInfoBuilder.Length > maxMessageLength)
                    {
                        await botClient.SendTextMessageAsync(chatId, messageBuilder.ToString());
                        messageBuilder.Clear();
                    }
                    messageBuilder.Append(countryInfoBuilder);
                }
                if (messageBuilder.Length > 0)
                {
                    await botClient.SendTextMessageAsync(chatId, messageBuilder.ToString());
                }
                step[chatId] = "";
            }
            async Task GetUserEvent(string input)
            {
                string[] lines = input.Split('\n');
                Regex regex = new Regex(@"\d{2}\.\d{2}\.\d{4}");
                if (lines.Length >= 3)
                {
                    string date = lines[0];
                    string name = lines[1];
                    string notes = lines[2];
                    if (!regex.IsMatch(date))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Некоректний формат дати. Використовуйте формат дд.мм.рррр");
                        return;
                    }
                    var id = message.Chat.Id;
                    await DoUserEvent(date, name, notes, id);
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Помилка у вхідних даних");

                }
                step[message.Chat.Id] = "";
            }
            async Task DoUserEvent(string date, string name, string notes, long id)
            {
                User userEvent = new User();
                UserEvents userEvents = new UserEvents();
                var resultEvent = await userEvent.PutUserEventsAsync(date, name, notes, id);

                await botClient.SendTextMessageAsync(message.Chat.Id, $"Дата: {resultEvent.Date}\n" +
            $"Назва: {resultEvent.Name}\n" +
            $"Нотатки: {resultEvent.Notes}\n");
                step[message.Chat.Id] = "";
            }
            async Task UpdateUserEvent(string input)
            {
                User userevent = new User();
                string[] lines = input.Split('\n');
                Regex regex = new Regex(@"\d{2}\.\d{2}\.\d{4}");
                if (lines.Length >= 3)
                {
                    string date = lines[0];
                    string name = lines[1];
                    string notes = lines[2];
                    if (!regex.IsMatch(date))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Некоректний формат дати. Використовуйте формат дд.мм.рррр");
                        return;
                    }
                    var id = message.Chat.Id;
                    User userEventUpdate = new User();
                    UserEventDB userEventDB = new UserEventDB();
                    var updatedReviews = await userEventUpdate.PutUpdateUserEventsAsync(date, name, notes, id);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Ваша подія була змінена\nДата: {updatedReviews.Date}\n" +
           $"Назва: {updatedReviews.Name}\n" +
           $"Нотатки: {updatedReviews.Notes}\n");
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Помилка у вхідних данних");
                }
                step[message.Chat.Id] = "";
            }
            async Task<List<UserEventDB>> SelectUserEventDB(long id)
            {
                NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect);
                List<UserEventDB> review = new List<UserEventDB>();
                await connection.OpenAsync();
                var sql = $"select \"Date\", \"Name\", \"Notes\" from public.\"UserEvents\" where \"Id\" = {message.Chat.Id}";
                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                NpgsqlDataReader reader = command.ExecuteReader();
                while (await reader.ReadAsync())
                {
                    review.Add(new UserEventDB
                    {
                        Date = reader.GetString(0),
                        Name = reader.GetString(1),
                        Notes = reader.GetString(2),

                    });
                }
                await connection.CloseAsync();
                return review;
            }
            async Task GetAllUserEvents(long id)
            {
                UserEventDB userEvents = new UserEventDB();
                var result = SelectUserEventDB(id).Result;
                foreach (var item in result)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Дата: {item.Date}\n" +
                $"Назва: {item.Name}\n" +
                $"Нотатки: {item.Notes} \n");
                    step[message.Chat.Id] = "";
                }
            }
            async Task DeleteEvent(string name)
            {
                Database database = new Database();
                await database.DeleteUserEventAsync(name);
                step[message.Chat.Id] = "";
            }
            return;
        }
        public async Task<UserEventDB> GetTodayEventAsync(long chatId)
        {
            NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect);
            UserEventDB todayEvent = null;
            await connection.OpenAsync();
            DateTime today = DateTime.Today;
            var sql = $"select \"Date\", \"Name\", \"Notes\" from public.\"UserEvents\" where \"Id\" = {chatId} and \"Date\" = '{today.ToShortDateString()}'";
            NpgsqlCommand command = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = command.ExecuteReader();
            if (await reader.ReadAsync())
            {
                todayEvent = new UserEventDB
                {
                    Date = reader.GetString(0),
                    Name = reader.GetString(1),
                    Notes = reader.GetString(2)
                };
            }
            await connection.CloseAsync();
            return todayEvent;
        }
        async Task<UserEventDB> CheckTodayEvent(long chatId)
        {
            UserEventDB userEvents = new UserEventDB();
            var todayEvent = await GetTodayEventAsync(chatId);

            if (todayEvent != null)
            {
                await botClient.SendTextMessageAsync(chatId, $"Сьогодні є подія:\n" +
                    $"Назва: {todayEvent.Name}\n" +
                    $"Нотатки: {todayEvent.Notes}");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "На жаль, сьогодні подій немає.");
            }
            return userEvents;
        }
    }
}