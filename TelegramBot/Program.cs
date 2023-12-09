using System.Security.Cryptography.X509Certificates;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Threading;
using Microsoft.VisualBasic;
using static System.Net.Mime.MediaTypeNames;


TelegramBotClient botClient = new("6762325774:AAHXTbacyLzyYmYh8VYZf7SZuh-Ozh_NxG4");

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

int role = 0;
string addtxt = "";
string menuanswer = " \n \nЧто вы хотите сделать?";
string addhours = "Сколько часов будет активно объявление? \nКоличество часов должно быть целым числом";
string activateadd = "Введите номер заявки, которую надо активировать";
string stopadd = "Введите номер заявки, которую надо приостановить";
string deleteadd = "Введите номер заявки, которую надо удалить";
SqlConnection myConnection = new("Server=localhost\\SQLEXPRESS03;Database=TgBot;Trusted_Connection=True;");


ReplyKeyboardMarkup ArkmMenu = new(new[]
{
    new KeyboardButton("Создать новое объявление"),
    new KeyboardButton("Просмотреть личный кабинет"),
})
{ResizeKeyboard = true};
ReplyKeyboardMarkup ArkmCabinet = new(new[]
            {
                new KeyboardButton("Активировать заявку"),
                new KeyboardButton("Приостановить заявку"),
                new KeyboardButton("Удалить заявку"),
                new KeyboardButton("Вернуться в меню"),
            })
{ResizeKeyboard = true};

ReplyKeyboardMarkup PrkmMenu = new(new[]
{
    new KeyboardButton("Просмотреть доступные заявки"),
    new KeyboardButton("Просмотреть личный кабинет"),
})
{ResizeKeyboard = true};
ReplyKeyboardMarkup PrkmAdds = new(new[]
{
    new KeyboardButton("Выбрать заявку для рекламы"),
    new KeyboardButton("Вернуться в меню"),
})
{ResizeKeyboard = true};

ReplyKeyboardMarkup PrkmCabinet = new(new[]
{
    new KeyboardButton("Отказаться от заявки"),
    new KeyboardButton("Вернуться в меню")
})
{ResizeKeyboard = true};

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

void ConnectToSQL(string query)
{
    myConnection.Open();
    SqlDataAdapter adpt = new(query, myConnection);
    DataTable table = new();
    adpt.Fill(table);
    myConnection.Close();
}

async Task SentMenu(Message message, long chatId, CancellationToken cancellationToken, string txt)
{
    if (role == 1)
    {
        Message sentMenu = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"{txt} {menuanswer}",
            replyMarkup: ArkmMenu,
            cancellationToken: cancellationToken);
    }
    else if(role == 2){
        Message sentMenu = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"{txt} {menuanswer}",
            replyMarkup: PrkmMenu,
            cancellationToken: cancellationToken);
    }
}

async Task SentCabinet(Message message, long chatId, CancellationToken cancellationToken, string txt)
{
    if (role == 1)
    {
        Message sentMenu = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"{txt}",
            replyMarkup: ArkmCabinet,
            cancellationToken: cancellationToken);
    }
    else if(role == 2){
        Message sentMenu = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"{txt}",
            replyMarkup: PrkmCabinet,
            cancellationToken: cancellationToken);
    }
}

async Task ChooseRole(Message message, long chatId, CancellationToken cancellationToken)
{
    ReplyKeyboardMarkup rkmChooseRole = new(new[]
                {
                new KeyboardButton ("Рекламодатель"),
                new KeyboardButton("Паблишер"),
                })
    {
        ResizeKeyboard = true
    };

    Message sentMessage = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: $"Здравствуйте, {message.Chat.FirstName}! \n \nДля начала работы с ботом, пожалуйста, выберите роль",
        replyMarkup: rkmChooseRole,
        cancellationToken: cancellationToken);
}

async Task WrongCommand(long chatId, string txt, CancellationToken cancellationToken)
{
    Message sentMenu = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: txt,
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);
}

async Task WrongData(Message message, long chatId, CancellationToken cancellationToken)
{
    if (role == 1)
    {
        Message sentMenu = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: $"Вы ввели неправильное значение {menuanswer}",
        replyMarkup: ArkmMenu,
        cancellationToken: cancellationToken);
    }
    else if (role == 2)
    {
        Message sentMenu = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: $"Вы ввели неправильное значение {menuanswer}",
        replyMarkup: PrkmMenu,
        cancellationToken: cancellationToken);
    }
    
}

async Task PersonalCabinet(Message message, long chatId, CancellationToken cancellationToken)
{
    try
    {
        string role_str = "";
        if(role == 1)
        {
            role_str = "advertiser";
        }
        else if (role == 2)
        {
            role_str = "publisher";
        }
        string pc_query = $"select adds.id_add, adds.text, addstatus.status, adds.active_hours, adds.hours, adds.status from adds inner join addstatus on adds.status = addstatus.id_status where {role_str} = (select id_user from users where username = '{message.Chat.Username}') and adds.status <> '3'";
        myConnection.Open();
        SqlDataAdapter pc_adpt = new(pc_query, myConnection);
        DataTable pc_table = new();
        pc_adpt.Fill(pc_table);
        string msg = "";
        if (pc_table.Rows.Count > 0)
        {
            string row = "";
            for (int i = 0; i < pc_table.Rows.Count; i++)
            {
                row += $"Заявка №{pc_table.Rows[i][0]} \nТекст: {pc_table.Rows[i][1]} \nСтатус: {pc_table.Rows[i][2]} \nПрошло {pc_table.Rows[i][3]} из {pc_table.Rows[i][4]} часов \n \n";
            }

            string acc_query = $"select account from users where username = '{message.Chat.Username}'";
            SqlDataAdapter acc_adpt = new(acc_query, myConnection);
            DataTable acc_table = new();
            acc_adpt.Fill(acc_table);
            myConnection.Close();
            msg = $"Личный кабинет пользователя @{message.Chat.Username}: \nКоличество средств на счёте - {acc_table.Rows[0][0]} \n \nВсе заявки \n - - - - - - - -\n{row} - - - - - - - -\n";
        }
        else
        {
            msg = "Нет заявок";
        }
        Console.WriteLine($"{msg} - - - - - - - -\n");
        await SentCabinet(message, chatId, cancellationToken, msg);
    }
    catch (Exception ex)
    {
        Console.WriteLine(" - - - - - - - - - - \n ОШИБКА: " + ex.Message + "\n - - - - - - - - - - ");
    }


}

async Task ShowAllAvailableAdds(Message message, long chatId, CancellationToken cancellationToken)
{
    try
    {
        string msg = "";
        string query = "";
        string row = "";
        query = $"select id_add, text, active_hours, hours, status from adds where publisher is NULL and status = '1'";
        myConnection.Open();
        SqlDataAdapter adds_adpt = new(query, myConnection);
        DataTable adds_table = new();
        adds_adpt.Fill(adds_table);
        myConnection.Close();
        if (adds_table.Rows.Count > 0)
        {
            for (int i = 0; i < adds_table.Rows.Count; i++)
            {
                row += $"Заявка №{adds_table.Rows[i][0]} \nТекст: {adds_table.Rows[i][1]} \nПрошло {adds_table.Rows[i][2]} из {adds_table.Rows[i][3]} часов \n \n";
            }
            msg = $"Все доступные заявки \n - - - - - - - -\n{row} ";
        }
        else
        {            
            msg = "Нет заявок";
        }
        Console.WriteLine($"{msg} - - - - - - - -\n");
        Message sentAdds = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"{msg}",
            replyMarkup: PrkmAdds,
            cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine(" - - - - - - - - - - \n ОШИБКА: " + ex.Message + "\n - - - - - - - - - - ");
    }
}
    

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    try
    {
        
        if (update.Message is not { } message)
            return;

        var chatId = message.Chat.Id;

        Console.WriteLine($"Received a '{message.Text}' message in chat {chatId} with @{message.Chat.Username}");
        if (role == 0)
        {
            if (message.Text == "/start")
            {
                string query = $"select role from users where username = '{message.Chat.Username}'";
                myConnection.Open();
                SqlDataAdapter adpt = new(query, myConnection);
                DataTable table = new();
                adpt.Fill(table);
                myConnection.Close();

                if (table.Rows.Count > 0)
                {
                    string greetings = $"Добро пожаловать, {message.Chat.FirstName}!";
                    if (table.Rows[0][0].ToString() == "1")
                    {
                        role = 1;
                        await SentMenu(message, chatId, cancellationToken, greetings);
                    }
                    else if (table.Rows[0][0].ToString() == "2")
                    {
                        role = 2;
                        await SentMenu(message, chatId, cancellationToken, greetings);
                    }
                    else
                    {
                        await ChooseRole(message, chatId, cancellationToken);
                    }
                }
                else
                {
                    await ChooseRole(message, chatId, cancellationToken);
                }
            }
            else if (message.Text == "Рекламодатель" || message.Text == "Паблишер")
            {
                string selectquery = $"select role from users where username = '{message.Chat.Username}'";
                myConnection.Open();
                SqlDataAdapter adpt = new(selectquery, myConnection);
                DataTable table = new();
                adpt.Fill(table);
                myConnection.Close();
                string query = "";
                if (message.Text == "Рекламодатель")
                {
                    role = 1;
                }
                else
                {
                    role = 2;
                }
                if (table.Rows.Count > 0)
                {
                    if (table.Rows[0][0].ToString().Trim() == "")
                    {
                        query = $"update users set role = '{role}' where username = '{message.Chat.Username}'";
                    }
                }
                else
                {
                    query = $"insert into Users (username, surname, name, role, account, chatID) values ('{message.Chat.Username}', '{message.Chat.LastName}', '{message.Chat.FirstName}', '{role}', '10000', '{message.Chat.Id}')";
                }
                ConnectToSQL(query);
                string greetings = $"Добро пожаловать, {message.Chat.FirstName}!";
                await SentMenu(message, chatId, cancellationToken, greetings);
            }
            else
            {
                string msg = "Для начала работы с ботом напишите \"/start\"";
                await WrongCommand(chatId, msg, cancellationToken);
            }
        }
        
        else if (role == 1)
        {
            if (message.Text == "Создать новое объявление" || message.Text == "Просмотреть личный кабинет")
            {
                if (message.Text == "Создать новое объявление")
                {
                    Message sentmsg = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Введите текст до 70 символов для вашего объявления",
                    replyMarkup: new ForceReplyMarkup(),
                    cancellationToken: cancellationToken);
                }
                else
                {
                    await PersonalCabinet(message, chatId, cancellationToken);
                }
            }

            else if (message.Text == "Вернуться в меню")
            {
                string msg = "Меню";
                await SentMenu(message, chatId, cancellationToken, msg);
            }

            else if (message.Text == "Активировать заявку" || message.Text == "Приостановить заявку" || message.Text == "Удалить заявку")
            {
                string action = "";
                if (message.Text == "Активировать заявку")
                {
                    action = "активировать";
                }
                else if (message.Text == "Приостановить заявку")
                {
                    action = "приостановить";
                }
                else
                {
                    action = "удалить";
                }

                Message sentmsg = await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Введите номер заявки, которую надо {action}",
                        replyMarkup: new ForceReplyMarkup(),
                        cancellationToken: cancellationToken);
            }

            else if (message.ReplyToMessage != null)
            {
                if (message.ReplyToMessage.Text == "Введите текст до 70 символов для вашего объявления")
                {
                    if (message.Text.Length <= 70)
                    {
                        addtxt = message.Text;

                        Message sentmsg = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: addhours,
                            replyMarkup: new ForceReplyMarkup(),
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Message sentMenu = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"Слишком длинное сообщение {menuanswer}",
                            replyMarkup: ArkmMenu,
                            cancellationToken: cancellationToken);
                    }

                }
                else if (message.ReplyToMessage.Text == addhours || message.ReplyToMessage.Text == activateadd || message.ReplyToMessage.Text == stopadd || message.ReplyToMessage.Text == deleteadd)
                {

                    if (int.TryParse(message.Text, out int x))
                    {
                        if (message.ReplyToMessage.Text == addhours)
                        {
                            if (Int32.Parse(message.Text) > 1 && Int32.Parse(message.Text) % 2 == 0)
                            {
                                string query = $"insert into Adds (advertiser, text, status, hours, active_hours) values ((select id_user from users where username = '{message.Chat.Username}'), '{addtxt}', '2', '{Int32.Parse(message.Text)}', '0')";
                                ConnectToSQL(query);

                                Message sentmsg = await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Объявление успешно создано! {menuanswer}",
                                replyMarkup: ArkmMenu,
                                cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await WrongData(message, chatId, cancellationToken);
                            }
                        }
                        else
                        {
                            string query = $"select status from adds where id_add = '{message.Text}'";
                            myConnection.Open();
                            SqlDataAdapter adpt = new(query, myConnection);
                            DataTable table = new();
                            adpt.Fill(table);
                            myConnection.Close();
                            if (table.Rows.Count != 0)
                            {
                                if (table.Rows[0][0].ToString() != "3")
                                {
                                    string secquery = "";
                                    if (message.ReplyToMessage.Text == activateadd)
                                    {
                                        secquery = $"update adds set status = '1' where id_add = '{message.Text}'";
                                    }
                                    else if (message.ReplyToMessage.Text == stopadd)
                                    {
                                        secquery = $"update adds set status = '2' where id_add = '{message.Text}'";
                                    }
                                    else
                                    {
                                        secquery = $"update adds set status = '3' where id_add = '{message.Text}'";
                                    }
                                    ConnectToSQL(secquery);
                                    Message sentMenu = await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: $"Статус заявки успешно обновлён!",
                                        replyMarkup: ArkmCabinet,
                                        cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    Message sentMenu = await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: $"Эта заявка удалена",
                                        cancellationToken: cancellationToken);
                                }
                            }
                            else
                            {
                                Message sentMenu = await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: $"Такой заявки не существует",
                                        cancellationToken: cancellationToken);
                            }
                            await PersonalCabinet(message, chatId, cancellationToken);
                        }
                    }
                    else
                    {
                        await WrongData(message, chatId, cancellationToken);
                    }

                }
            }

            else
            {
                string msg = "К сожалению, такой команды нет \n Нажмите \"/start\" для перехода в меню";
                await WrongCommand(chatId, msg, cancellationToken);
            }
        }
        
        else if(role == 2) {

            if (message.Text == "Просмотреть личный кабинет")
            {
                await PersonalCabinet(message, chatId, cancellationToken);
            }

            else if (message.Text == "Отказаться от заявки")
            {
                string update_query = $"update adds set publisher = NULL where publisher = (select id_user from users where username = '{message.Chat.Username}')";
                ConnectToSQL(update_query);
                await SentMenu(message, chatId, cancellationToken, "Вы успешно отказались от заявки");
            } 

            else if (message.Text == "Вернуться в меню")
            {
                string msg = "Меню";
                await SentMenu(message, chatId, cancellationToken, msg);
            }

            else if (message.Text == "Просмотреть доступные заявки")
            {
                await ShowAllAvailableAdds(message, chatId, cancellationToken);
            }

            else if (message.Text == "Выбрать заявку для рекламы")
            {
                string query = $"select * from adds where publisher = (select id_user from users where username = '{message.Chat.Username}')";
                myConnection.Open();
                SqlDataAdapter adpt = new(query, myConnection);
                DataTable table = new();
                adpt.Fill(table);
                myConnection.Close();
                if (table.Rows.Count == 0)
                {
                    Message sentmsg = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Напишите номер заявки, которую хотите выбрать",
                    replyMarkup: new ForceReplyMarkup(),
                    cancellationToken: cancellationToken);
                }
                else
                {
                    Message sentMenu = await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Вы не можете выбрать новую заявку, так как уже выбрали другую",
                        cancellationToken: cancellationToken);
                    await SentMenu(message, chatId, cancellationToken, "");
                }
                
            }

            else if (message.ReplyToMessage != null)
            {
                if (message.ReplyToMessage.Text == "Напишите номер заявки, которую хотите выбрать")
                {
                    if (Int32.TryParse(message.Text, out int x))
                    {
                        string query = $"select * from adds where id_add = '{message.Text}' and status = 1";
                        myConnection.Open();
                        SqlDataAdapter adpt = new(query, myConnection);
                        DataTable table = new();
                        adpt.Fill(table);
                        myConnection.Close();
                        if (table.Rows.Count != 0)
                        {
                            string update_query = $"update adds set publisher = (select id_user from users where username = '{message.Chat.Username}') where id_add = '{message.Text}'";
                            ConnectToSQL(update_query);
                            await SentMenu(message, chatId, cancellationToken, $"Вы успешно выбрали заявку №{message.Text}");
                        }
                        else
                        {
                            Message sentMenu = await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Такой заявки не существует",
                                cancellationToken: cancellationToken);
                            await SentMenu(message, chatId, cancellationToken, "");
                        }
                    }
                    else
                    {
                        await WrongData(message, chatId, cancellationToken);
                    }                               
                }
            }

            else
            {
                string msg = "К сожалению, такой команды нет \nНажмите \"/start\" для перехода в меню";
                await WrongCommand(chatId, msg, cancellationToken);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(" - - - - - - - - - - \n ОШИБКА: " + ex + "\n - - - - - - - - - - ");
    }
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
