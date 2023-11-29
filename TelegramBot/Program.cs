using System.Security.Cryptography.X509Certificates;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data.SqlClient;
using System.Data;


SqlConnection myConnection = new SqlConnection("TOPA-KOMP\\SQLEXPRESS01; database = AddBot; Integrated Security=True; TrustServerCertificate = True");

TelegramBotClient botClient = new("6762325774:AAHXTbacyLzyYmYh8VYZf7SZuh-Ozh_NxG4");

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{

    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    if (message.Text == "/start")
    {
        ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
        {
            new KeyboardButton("Рекламодатель"),
            new KeyboardButton("Паблишер"),
        })
        {
            ResizeKeyboard = true
        };

        Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Пожалуйста, выберите роль",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    if (message.Text == "Рекламодатель")
    {
        myConnection.Open();
        string query = $"inser into users values('1', '{message.Chat.Username}', '{message.Chat.LastName}', '{message.Chat.FirstName}', '10000', '{message.Chat.Id}';";
        SqlDataAdapter adpt = new SqlDataAdapter(query, myConnection);
        DataTable table = new DataTable();
        adpt.Fill(table);
        myConnection.Close();

        Message sentmsg = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"Вы зарегестрированы!",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }
    else if (message.Text == "Паблишер")
    {
        Message sentmsg = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"Проверьте данные: \n Имя - {message.Chat.FirstName} \n Фамилия - {message.Chat.LastName} \n Имя пользователя - {message.Chat.Username} \n Роль - {message.Text}",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }




    Console.WriteLine(value: $"{message.From.FirstName} sent message {message.MessageId} to chat {message.Chat.Id} at {message.Date.ToLocalTime()} \n - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -");
    
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

Console.ReadLine();
