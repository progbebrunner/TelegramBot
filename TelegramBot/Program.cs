using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data.SqlClient;
using System.Data;
using TelegramBot;
using Telegram.Bot.Requests;

TelegramBotClient botClient = new("6762325774:AAHXTbacyLzyYmYh8VYZf7SZuh-Ozh_NxG4");

using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

BotUser[] users = Array.Empty<BotUser>();
string addtxt = "";
string menuanswer = " \n \nЧто вы хотите сделать?";
string addhours = "Сколько часов будет активно объявление? \nКоличество часов должно быть целым числом";
string activateadd = "Введите номер заявки, которую надо активировать";
string stopadd = "Введите номер заявки, которую надо приостановить";
string deleteadd = "Введите номер заявки, которую надо удалить";
SqlConnection myConnection = new("Server=localhost\\SQLEXPRESS03;Database=TgBot;Trusted_Connection=True;");
DateTime dt_start = DateTime.Now;


ReplyKeyboardMarkup ArkmMenu = new(new[]
{
    new KeyboardButton("Создать новое объявление"),
    new KeyboardButton("Просмотреть личный кабинет"),
})
{ ResizeKeyboard = true };
ReplyKeyboardMarkup ArkmCabinet = new(new[]
{
    new[]{
        new KeyboardButton("Активировать"),
        new KeyboardButton("Приостановить"),
        new KeyboardButton("Удалить")},
    new[]{
        new KeyboardButton("Выйти"),
        new KeyboardButton("Вернуться в меню")}
})
{ ResizeKeyboard = true };

ReplyKeyboardMarkup PrkmMenu = new(new[]
{
    new KeyboardButton("Просмотреть доступные заявки"),
    new KeyboardButton("Просмотреть личный кабинет"),
})
{ ResizeKeyboard = true };
ReplyKeyboardMarkup PrkmAdds = new(new[]
{
    new KeyboardButton("Выбрать заявку для рекламы"),
    new KeyboardButton("Вернуться в меню"),
})
{ ResizeKeyboard = true };

ReplyKeyboardMarkup PrkmCabinet = new(new[]
{
    new[]{
        new KeyboardButton("Отказаться от заявки"),
        new KeyboardButton("Вернуться в меню")},
    new[]{
        new KeyboardButton("Выйти")}
})
{ ResizeKeyboard = true };

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);
//botClient.SetWebhookAsync("https://f211-188-143-222-213.ngrok-free.app", dropPendingUpdates: true).Wait();
await botClient.DeleteWebhookAsync();
User me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
await CheckAdds();
while (await timer.WaitForNextTickAsync(cts.Token))
{
    await CheckAdds();
}
Console.ReadLine();

//Send cancellation request to stop bot
cts.Cancel();

async Task CheckAdds()
{
    try
    {
        if (myConnection.State == ConnectionState.Open || myConnection.State == ConnectionState.Connecting)
        {
            await myConnection.CloseAsync();
        }
        await myConnection.OpenAsync(cts.Token);
        string CheckAdds_q = "select id_add, text, hours, active_hours, last_check, chatId from adds inner join users on adds.publisher = users.id_user where status = 1 and publisher > 0";
        SqlDataAdapter CheckAdds_adpt = new(CheckAdds_q, myConnection);
        DataTable CheckAdds_tbl = new();
        CheckAdds_adpt.Fill(CheckAdds_tbl);
        if (CheckAdds_tbl.Rows.Count > 0)
        {
            for (int i = 0; i < CheckAdds_tbl.Rows.Count; i++)
            {
                if (DateTime.Now.Subtract(DateTime.Parse(CheckAdds_tbl.Rows[i][4].ToString().Trim())) >= TimeSpan.Parse("00:30:00"))
                {
                    var user_info = await botClient.GetChatAsync(int.Parse(CheckAdds_tbl.Rows[i][5].ToString().Trim()), cts.Token);
                    if (user_info.Bio.Trim().ToLower().Contains(CheckAdds_tbl.Rows[i][1].ToString().Trim().ToLower()))
                    {
                        var dt_now = DateTime.Now;
                        var last_check = new DateTime(dt_now.Year, dt_now.Month, dt_now.Day, dt_now.Hour, dt_now.Minute, 0);

                        string upd_q = $"update adds set active_hours = active_hours + 1, last_check = '{last_check}' where id_add = {int.Parse(CheckAdds_tbl.Rows[i][0].ToString().Trim())}";
                        await ConnectToSQL(upd_q);
                        string upd_publ_q = $"update users set account = account + 90";
                        await ConnectToSQL(upd_publ_q);
                    }
                    else
                    {
                        string upd_publ = $"update adds set publisher = NULL where id_add = {int.Parse(CheckAdds_tbl.Rows[i][0].ToString().Trim())}";
                        await ConnectToSQL(upd_publ);
                        await CommandAndTxt(int.Parse(CheckAdds_tbl.Rows[i][5].ToString().Trim()), $"Так вы убрали текст заявки №{CheckAdds_tbl.Rows[i][0].ToString().Trim()}, то вы больше не привязаны к ней", cts.Token);
                        string SendNote_q = $"select id_add, chatId from adds inner join users on adds.advertiser = users.id_user where id_add = {int.Parse(CheckAdds_tbl.Rows[i][0].ToString().Trim())}";
                        SqlDataAdapter SendNote_adpt = new(SendNote_q, myConnection);
                        DataTable SendNote_tbl = new();
                        CheckAdds_adpt.Fill(CheckAdds_tbl);
                        await CommandAndTxt(int.Parse(SendNote_tbl.Rows[0][1].ToString().Trim()), $"Заявка №{CheckAdds_tbl.Rows[i][0].ToString().Trim()} теперь снова доступна для выбора, так как Паблишер не выполнил условия продления", cts.Token);
                    }
                }
            }
        }

        string CompletedAdds_q = "select id_add, hours, active_hours, chatId from adds inner join users on adds.advertiser = users.id_user where status = '1' and publisher > 0 and hours = active_hours";
        SqlDataAdapter CompletedAdds_adpt = new(CompletedAdds_q, myConnection);
        DataTable CompletedAdds_tbl = new();
        CompletedAdds_adpt.Fill(CompletedAdds_tbl);
        if (CompletedAdds_tbl.Rows.Count > 0)
        {
            for (int i = 0; i < CompletedAdds_tbl.Rows.Count; i++)
            {
                string upd_publ = $"update adds set status = 3 where id_add = {int.Parse(CompletedAdds_tbl.Rows[i][0].ToString().Trim())}";
                await ConnectToSQL(upd_publ);
                await CommandAndTxt(int.Parse(CompletedAdds_tbl.Rows[i][3].ToString().Trim()), $"Заявка №{CompletedAdds_tbl.Rows[i][0].ToString().Trim()} закончила свою работу", cts.Token);
            }
        }
        myConnection.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine(" - - - - - - - - - - \n ОШИБКА: " + ex + "\n - - - - - - - - - - ");
    }
    
}

async Task TgBotProgramm(Update? update, int? role, long chatId, CancellationToken cancellationToken)
{
    try
    {        
        if (update.Message is not { } message)
        { return; }
        if (role == 0)
        {
            if (message.Text == "/start")
            {
                await ChooseRole(message, chatId, cancellationToken);
            }
            else if (message.Text == "Рекламодатель" || message.Text == "Паблишер")
            {
                string chooserole_query = $"select role from users where username = '{message.Chat.Username}'";
                 
                SqlDataAdapter chooserole_adpt = new(chooserole_query, myConnection);
                DataTable chooserole_table = new();
                chooserole_adpt.Fill(chooserole_table);
                 
                string query = "";
                if (message.Text == "Рекламодатель")
                {
                    role = 1;
                }
                else
                {
                    role = 2;
                }
                if (chooserole_table.Rows.Count > 0)
                {
                    if (chooserole_table.Rows[0][0].ToString().Trim() == "")
                    {
                        query = $"update users set role = '{role}', account = '10000' where username = '{message.Chat.Username}'";
                        await ConnectToSQL(query);
                        int index;
                        foreach(BotUser u in users)
                        {
                            if (u.GetUsername() == message.Chat.Username)
                            {
                                
                                index = Array.IndexOf(users, u);
                                users[index].SetRole(role);
                            }
                        }
                        
                    }
                }
                else
                {
                    query = $"insert into Users (username, surname, name, role, account, chatID) values ('{message.Chat.Username}', '{message.Chat.LastName}', '{message.Chat.FirstName}', '{role}', '10000', '{message.Chat.Id}')";
                    await ConnectToSQL(query);
                    Array.Resize(ref users, users.Length + 1);
                    BotUser new_user = new();
                    new_user.SetUsername(message.Chat.Username);
                    new_user.SetRole(role);
                    new_user.SetChatId(message.Chat.Id);
                    users[^1] = new_user;                    
                }
                string greetings = $"Добро пожаловать, {message.Chat.FirstName}!";
                await SentMenu(role, chatId, cancellationToken, greetings);
            }
            else
            {
                string msg = "Для начала работы с ботом напишите \"/start\"";
                await CommandAndTxt(chatId, msg, cancellationToken);
            }
        }

        else if (role == 1)
        {
            if (message.Text == "/start")
            {
                string greetings = $"Добро пожаловать, {message.Chat.FirstName}!";
                await SentMenu(role, chatId, cancellationToken, greetings);
            }

            else if (message.Text == "Создать новое объявление" || message.Text == "Просмотреть личный кабинет")
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
                    await PersonalCabinet(message, role, chatId, cancellationToken);
                }
            }

            else if (message.Text == "Вернуться в меню")
            {
                string msg = "Меню";
                await SentMenu(role, chatId, cancellationToken, msg);
            }

            else if (message.Text == "Активировать" || message.Text == "Приостановить" || message.Text == "Удалить")
            {
                string action = "";
                if (message.Text == "Активировать")
                {
                    action = "активировать";
                }
                else if (message.Text == "Приостановить")
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
                        await SentMenu(role, chatId, cancellationToken, $"Слишком длинное сообщение");
                    }

                }
                else if (message.ReplyToMessage.Text == addhours || message.ReplyToMessage.Text == activateadd || message.ReplyToMessage.Text == stopadd || message.ReplyToMessage.Text == deleteadd)
                {

                    if (int.TryParse(message.Text, out int x))
                    {
                        if (message.ReplyToMessage.Text == addhours)
                        {
                            if (int.Parse(message.Text) > 1)
                            {
                                string msg = "";
                                string accCheck_query = $"select account from users where username = '{message.Chat.Username}'";
                                SqlDataAdapter accCheck_adpt = new(accCheck_query, myConnection);
                                DataTable accCheck_table = new();
                                accCheck_adpt.Fill(accCheck_table);
                                if (int.Parse(accCheck_table.Rows[0][0].ToString().Trim()) >= int.Parse(message.Text.Trim()) * 100)
                                {
                                    string query = $"insert into Adds (advertiser, text, status, hours, active_hours) values ((select id_user from users where username = '{message.Chat.Username}'), '{addtxt}', '2', '{int.Parse(message.Text)}', '0')";
                                    await ConnectToSQL(query);
                                    query = $"update users set account = '{int.Parse(accCheck_table.Rows[0][0].ToString().Trim()) - (int.Parse(message.Text.Trim()) * 100)}' where username = '{message.Chat.Username}'";
                                    await ConnectToSQL(query);
                                    msg = $"Объявление успешно создано";
                                }
                                else
                                {
                                    msg = "У вас недостаточно средств \n \nДля пополнения счёта перейдите в личный кабинет";
                                }

                                await SentMenu(role, chatId, cancellationToken, msg);
                            }
                            else
                            {
                                await WrongData(message, role, chatId, cancellationToken);
                            }
                        }
                        else
                        {
                            string setstatus_query = $"select status from adds where id_add = '{message.Text}'";
                            SqlDataAdapter setstatus_adpt = new(setstatus_query, myConnection);
                            DataTable setstatus_table = new();
                            setstatus_adpt.Fill(setstatus_table);
                            if (setstatus_table.Rows.Count != 0)
                            {
                                if (setstatus_table.Rows[0][0].ToString() != "3")
                                {
                                    string secquery = "";
                                    if (message.ReplyToMessage.Text == activateadd)
                                    {
                                        secquery = $"update adds set status = '1' where id_add = '{message.Text}'";
                                        await SentCabinet(role, chatId, cancellationToken, "Статус заявки успешно обновлён!");
                                    }
                                    else if (message.ReplyToMessage.Text == stopadd)
                                    {
                                        secquery = $"update adds set status = '2' where id_add = '{message.Text}'";
                                        await SentCabinet(role, chatId, cancellationToken, "Статус заявки успешно обновлён!");
                                    }
                                    else
                                    {
                                        secquery = $"update adds set status = '3' where id_add = '{message.Text}'";
                                        await SentCabinet(role, chatId, cancellationToken, "Заявка была успешно удалена!");
                                        string SendNoteToP_q = $"select chatId from adds inner join users on adds.publisher = users.id_user where id_add = {message.Text}";
                                        SqlDataAdapter SendNoteToP_adpt = new(SendNoteToP_q, myConnection);
                                        DataTable SendNoteToP_table = new();
                                        SendNoteToP_adpt.Fill(SendNoteToP_table);
                                        if (SendNoteToP_table.Rows.Count != 0)
                                        {
                                            await CommandAndTxt(int.Parse(SendNoteToP_table.Rows[0][0].ToString().Trim()), $"К сожалению заявка №{message.Text}, к которой вы были привязаны, была удалена \n \n Для выбора новой заявки перейдите в меню по команде \"/start\"", cancellationToken)
                                        }
                                    }
                                    await ConnectToSQL(secquery);
                                }
                                else
                                {
                                    await CommandAndTxt(chatId, "Эта заявка была удалена", cancellationToken);
                                }
                            }
                            else
                            {
                                await CommandAndTxt(chatId, "Такой заявки не существует", cancellationToken);
                            }
                            await PersonalCabinet(message, role, chatId, cancellationToken);
                        }
                    }
                    else
                    {
                        await WrongData(message, role, chatId, cancellationToken);
                    }

                }
            }

            else if (message.Text == "Выйти")
            {
                await SentConfirm(chatId, cancellationToken, "Вы уверены, что хотите выйти? \n \n Все ваши заявки будут удалены", message.MessageId);
            }

            else
            {
                string msg = "К сожалению, такой команды нет \n Нажмите \"/start\" для перехода в меню";
                await CommandAndTxt(chatId, msg, cancellationToken);
            }
        }

        else if (role == 2)
        {
            if (message.Text == "/start")
            {
                string greetings = $"Добро пожаловать, {message.Chat.FirstName}!";
                await SentMenu(role, chatId, cancellationToken, greetings);
            }

            else if (message.Text == "Просмотреть личный кабинет")
            {
                await PersonalCabinet(message, role, chatId, cancellationToken);
            }

            else if (message.Text == "Отказаться от заявки")
            {
                string removeadd_query = $"select * from adds where publisher = (select id_user from users where username = '{message.Chat.Username}')";
                 
                SqlDataAdapter removeadd_adpt = new(removeadd_query, myConnection);
                DataTable removeadd_table = new();
                removeadd_adpt.Fill(removeadd_table);
                 
                if (removeadd_table.Rows.Count != 0)
                {
                    string update_query = $"update adds set publisher = NULL where publisher = (select id_user from users where username = '{message.Chat.Username}')";
                    await ConnectToSQL(update_query);
                    await SentMenu(role, chatId, cancellationToken, "Вы успешно отказались от заявки");
                }
                else
                {
                    await CommandAndTxt(chatId, "Вы не можете отказаться от заявки, так как таковой нет", cancellationToken);
                    await PersonalCabinet(message, role, chatId, cancellationToken);
                }
            }

            else if (message.Text == "Вернуться в меню")
            {
                string msg = "Меню";
                await SentMenu(role, chatId, cancellationToken, msg);
            }

            else if (message.Text == "Просмотреть доступные заявки")
            {
                await ShowAllAvailableAdds(message, chatId, cancellationToken);
            }

            else if (message.Text == "Выбрать заявку для рекламы")
            {
                string chooseadd_query = $"select * from adds where publisher = (select id_user from users where username = '{message.Chat.Username}') and status <> 3";
                SqlDataAdapter chooseadd_adpt = new(chooseadd_query, myConnection);
                DataTable chooseadd_table = new();
                chooseadd_adpt.Fill(chooseadd_table);
                if (chooseadd_table.Rows.Count == 0)
                {
                    Message sentmsg = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Напишите номер заявки, которую хотите выбрать",
                    replyMarkup: new ForceReplyMarkup(),
                    cancellationToken: cancellationToken);
                }
                else
                {
                    await CommandAndTxt(chatId, "Вы не можете выбрать новую заявку, так как уже выбрали другую", cancellationToken);
                    await SentMenu(role, chatId, cancellationToken, "");
                }

            }

            else if (message.ReplyToMessage != null)
            {
                if (message.ReplyToMessage.Text == "Напишите номер заявки, которую хотите выбрать")
                {
                    if (int.TryParse(message.Text, out int x))
                    {
                        string setadd_query = $"select text from adds where id_add = '{message.Text}' and status = 1";
                        SqlDataAdapter setadd_adpt = new(setadd_query, myConnection);
                        DataTable setadd_table = new();
                        setadd_adpt.Fill(setadd_table);
                        if (setadd_table.Rows.Count != 0)
                        {
                            Message sentConfirmAdd = await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Вы выбрали заявку №{message.Text} \n \nДля активации внесите текст заявки к себе в описание профиля: \n   \"{setadd_table.Rows[0][0]}\" \n \nПосле этого нажмите \"Подтвердить заявку\"",
                                replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Подтвердить заявку", $"Подтверждение заявки_{message.Text}_{message.MessageId}")),
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await CommandAndTxt(chatId, "Такой заявки не существует", cancellationToken);
                            await SentMenu(role, chatId, cancellationToken, "");
                        }
                    }
                    else
                    {
                        await WrongData(message, role, chatId, cancellationToken);
                    }
                }
            }

            else if (message.Text == "Выйти")
            {
                await SentConfirm(chatId, cancellationToken, "Вы уверены, что хотите выйти?", message.MessageId);
            }

            else
            {
                string msg = "К сожалению, такой команды нет \nНажмите \"/start\" для перехода в меню";
                await CommandAndTxt(chatId, msg, cancellationToken);
            }
        }        
    }
    catch (Exception ex)
    {
        Console.WriteLine(" - - - - - - - - - - \n ОШИБКА: " + ex + "\n - - - - - - - - - - ");
    }    
}

async Task SentConfirm(long chatId, CancellationToken cancellationToken, string txt, int msgID){

    Message? sentMenu = null;
    InlineKeyboardMarkup ikmSentConfim = new(new[]
    {
        InlineKeyboardButton.WithCallbackData("Да", $"Подтверждено_{msgID}"),
        InlineKeyboardButton.WithCallbackData("Нет", $"Отмененено_{msgID}")
    });
    sentMenu = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: $"{txt}",
        replyMarkup: ikmSentConfim,
        cancellationToken: cancellationToken);
}

async Task ConnectToSQL(string query)
{
    //await myConnection.OpenAsync();
    SqlDataAdapter adpt = new(query, myConnection);
    DataTable table = new();
    adpt.Fill(table);
    await Task.CompletedTask;
}

async Task SentMenu(int? role, long chatId, CancellationToken cancellationToken, string txt)
{
    if (role == 1)
    {
        Message sentMenu = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"{txt} {menuanswer}",
            replyMarkup: ArkmMenu,
            cancellationToken: cancellationToken);
    }
    else if (role == 2)
    {
        Message sentMenu = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"{txt} {menuanswer}",
            replyMarkup: PrkmMenu,
            cancellationToken: cancellationToken);
    }
}

async Task SentCabinet(int? role, long chatId, CancellationToken cancellationToken, string txt)
{
    if (role == 1)
    {
        Message sentMenu = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"{txt}",
            replyMarkup: ArkmCabinet,
            cancellationToken: cancellationToken);
    }
    else if (role == 2)
    {
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

async Task CommandAndTxt(long chatId, string txt, CancellationToken cancellationToken)
{
    Message sentMenu = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: txt,
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);
}

async Task WrongData(Message message, int? role, long chatId, CancellationToken cancellationToken)
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

async Task PersonalCabinet(Message message, int? role, long chatId, CancellationToken cancellationToken)
{
    try
    {
        string role_str = "";
        if (role == 1)
        {
            role_str = "advertiser";
        }
        else if (role == 2)
        {
            role_str = "publisher";
        }
        string pc_query = $"select adds.id_add, adds.text, addstatus.status, adds.publisher,adds.active_hours, adds.hours, adds.status, users.account from adds inner join addstatus on adds.status = addstatus.id_status inner join users on adds.{role_str} = users.id_user where adds.{role_str} = (select id_user from users where username = '{message.Chat.Username}') and adds.status <> '3'";
        SqlDataAdapter pc_adpt = new(pc_query, myConnection);
        DataTable pc_table = new();
        pc_adpt.Fill(pc_table);

        string acc_query = $"select roles.role, users.account from users inner join roles on users.role = roles.id_role where users.username = '{message.Chat.Username}'";
        SqlDataAdapter acc_adpt = new(acc_query, myConnection);
        DataTable acc_table = new();
        acc_adpt.Fill(acc_table);

        string msg = $"Личный кабинет пользователя @{message.Chat.Username}:\nРоль - {acc_table.Rows[0][0]} \nКоличество средств на счёте - {acc_table.Rows[0][1]} \n \n   Все заявки \n - - - - - - - - - - - - -\n";
        
        if (pc_table.Rows.Count > 0)
        {            
            string row = "";
            string publisher = "Отсутствует";
            for (int i = 0; i < pc_table.Rows.Count; i++)
            {
                if (pc_table.Rows[i][0] != null)
                {
                    publisher = "Есть";
                }
                row += $"Заявка №{pc_table.Rows[i][0]} \nТекст: {pc_table.Rows[i][1]} \nСтатус: {pc_table.Rows[i][2]} \nПаблишер: {publisher} \nПрошло {pc_table.Rows[i][4]} из {pc_table.Rows[i][5]} часов \n \n";
            }
            msg += row;
        }
        else
        {
            msg += "   Заявок нет \n";
        }
        Console.WriteLine($"{msg} - - - - - - - -\n");
        await SentCabinet(role, chatId, cancellationToken, msg);
    }
    catch (Exception ex)
    {
        Console.WriteLine(" - - - - - - - - - - \n ОШИБКА: " + ex + "\n - - - - - - - - - - ");
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
         
        SqlDataAdapter adds_adpt = new(query, myConnection);
        DataTable adds_table = new();
        adds_adpt.Fill(adds_table);
         
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
            msg = "Нет заявок \n";
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
        long chatId = 0;
        DataTable start_table = new();
        if (users.Length == 0)
        {
            if (myConnection.State == ConnectionState.Open || myConnection.State == ConnectionState.Connecting)
            {
                await myConnection.CloseAsync();
            }
            await myConnection.OpenAsync(cancellationToken);
            string start_query = $"select username, role, chatID from users";
            SqlDataAdapter start_adpt = new(start_query, myConnection);           
            start_adpt.Fill(start_table);
            Array.Resize(ref users, start_table.Rows.Count);
        }
        
        switch (update.Type)
        {
            case UpdateType.CallbackQuery:
                Console.WriteLine($"Received a '{update.CallbackQuery.Data}' CallbackQuery in chat {update.CallbackQuery.From.Id} with @{update.CallbackQuery.From.Username}");
                if (update.CallbackQuery != null)
                {
                    chatId = update.CallbackQuery.From.Id;
                    BotUser curr_user = new();
                    foreach (var user in users)
                    {
                        if (user.GetUsername() == update.CallbackQuery.From.Username)
                        {
                            curr_user = user;
                        }
                    }
                    if (update.CallbackQuery.Data.Contains("Подтверждено"))
                    {
                        string[] cbqData = update.CallbackQuery.Data.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        string msgID = cbqData[1];
                        await botClient.EditMessageReplyMarkupAsync(update.CallbackQuery.From.Id, int.Parse(msgID) + 1, null, cancellationToken);
                        DataTable AddsForDel_table = new();
                        if (curr_user.GetRole() == 1)
                        {
                            string AddsForDel_query = $"select adds.publisher from Adds inner join users on adds.advertiser = users.id_user where (advertiser = (select id_user from users where username = '{update.CallbackQuery.From.Username}')) and (publisher > 0) and (adds.status = 1)";
                            SqlDataAdapter AddsForDel_adpt = new(AddsForDel_query, myConnection);
                            AddsForDel_adpt.Fill(AddsForDel_table);
                            if (AddsForDel_table.Rows.Count > 0)
                            {
                                DataTable msgToPubl_table = new();
                                for (int i = 0; i < AddsForDel_table.Rows.Count; i++)
                                {
                                    string msgToPubl_query = $"select chatID from users where id_user = '{AddsForDel_table.Rows[i][0]}'";
                                    SqlDataAdapter msgToPubl_adpt = new(msgToPubl_query, myConnection);
                                    msgToPubl_adpt.Fill(msgToPubl_table);
                                    await CommandAndTxt(int.Parse(msgToPubl_table.Rows[i][0].ToString().Trim()), "К сожалению, заявка, к который вы были привязаны, была удалена \n \nДля выбора новой заявки перейдите в меню по команде \"/start\"", cancellationToken);                                
                                }
                            }
                            string deleteAdvQuery = $"update adds set status = 3 where advertiser = (select id_user from users where username = '{update.CallbackQuery.From.Username}')";
                            await ConnectToSQL(deleteAdvQuery);                            
                        }
                        else if (curr_user.GetRole() == 2)  
                        {
                            string AddsForDel_query = $"select adds.id_add, adds.status, users.chatID from adds inner join users on adds.advertiser = users.id_user where adds.publisher = (select id_user from users where username = '{update.CallbackQuery.From.Username}') and adds.status = 1";
                            SqlDataAdapter AddsForDel_adpt = new(AddsForDel_query, myConnection);
                            AddsForDel_adpt.Fill(AddsForDel_table);
                            if (AddsForDel_table.Rows.Count > 0)
                            {
                                await CommandAndTxt(long.Parse(AddsForDel_table.Rows[0][2].ToString().Trim()), $"К сожалению, паблишер заявки №{AddsForDel_table.Rows[0][0]} был удалён \n \nТеперь ваша заявка снова доступна для выбора", cancellationToken);
                            }                            
                            string deleteAdvQuery = $"update adds set publisher = NULL where publisher = (select id_user from users where username = '{update.CallbackQuery.From.Username}')";
                            await ConnectToSQL(deleteAdvQuery);
                        }
                        foreach (var user in users)
                        {
                            if (user == curr_user)
                            {
                                user.SetRole(0);
                            }
                        }
                        string query = $"update users set role = NULL, account = '0' where username = '{update.CallbackQuery.From.Username}'";
                        await ConnectToSQL(query);
                        await CommandAndTxt(chatId, "Команда подтверждена \n \nДля активации напишите команду \"/start\"", cancellationToken);
                    }

                    else if (update.CallbackQuery.Data.Contains("Отмененено"))
                    {
                        string[] cbqData = update.CallbackQuery.Data.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        string msgID = cbqData[1];
                        await botClient.EditMessageReplyMarkupAsync(update.CallbackQuery.From.Id, int.Parse(msgID) + 1, null, cancellationToken);
                        await SentMenu(curr_user.GetRole(), chatId, cancellationToken, "Команда отменена");
                    }

                    else if(update.CallbackQuery.Data.Contains("Подтверждение заявки"))
                    {
                        string[] cbqData = update.CallbackQuery.Data.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        string msgID = cbqData[2];
                        await botClient.EditMessageReplyMarkupAsync(update.CallbackQuery.From.Id, int.Parse(msgID) + 1, null, cancellationToken);
                        string PublDescCheck_query = $"select text from adds where id_add = {cbqData[1]}";
                        SqlDataAdapter PublDescCheck_adpt = new(PublDescCheck_query, myConnection);
                        DataTable PublDescCheck_table = new();
                        PublDescCheck_adpt.Fill(PublDescCheck_table);
                        var dt_now = DateTime.Now;
                        var last_check = new DateTime(dt_now.Year, dt_now.Month, dt_now.Day, dt_now.Hour, dt_now.Hour, 0);
                        var user_info = await botClient.GetChatAsync(update.CallbackQuery.From.Id, cancellationToken);
                        if (user_info.Bio != null)
                        {
                            if (user_info.Bio.Trim().ToLower().Contains(PublDescCheck_table.Rows[0][0].ToString().Trim().ToLower()))
                            {
                                string update_query = $"update adds set publisher = (select id_user from users where username = '{update.CallbackQuery.From.Username}'), last_check = '{last_check}' where id_add = '{cbqData[1]}'";
                                await ConnectToSQL(update_query);
                                await SentMenu(2, chatId, cancellationToken, $"Вы успешно выбрали заявку №{cbqData[1]}");
                            }
                            else
                            {
                                await SentMenu(curr_user.GetRole(), chatId, cts.Token, "Вы не внесли текст заявки к себе в описание");
                            }
                        }
                        else
                        {
                            await SentMenu(curr_user.GetRole(), chatId, cts.Token, "У вас пустое описание");
                        }
                    }
                }
                break;
            case UpdateType.Message:
                var message = update.Message;
                chatId = message.Chat.Id;
                int result = DateTime.Compare(message.Date, dt_start);
                if (result < 0  )
                {
                    Console.WriteLine($"Received a '{message.Text}' message in chat {message.Chat.Id} with @{message.Chat.Username}");
                    for (int i = 0; i < start_table.Rows.Count; i++)
                    {
                        BotUser old_user = new();
                        old_user.SetUsername(start_table.Rows[i][0].ToString());
                        if (int.TryParse(start_table.Rows[i][1].ToString().Trim(), out int x))
                        {
                            old_user.SetRole(int.Parse(start_table.Rows[i][1].ToString().Trim()));
                        }
                        else
                        {
                            old_user.SetRole(0);
                        }
                        old_user.SetChatId(int.Parse(start_table.Rows[i][2].ToString().Trim()));
                        users[i] = old_user;

                    }
                    int count = 0;
                    foreach (BotUser user in users)
                    {
                        if (user.GetUsername() == message.Chat.Username)
                        {
                            if (user.GetRole() == 1)
                            {
                                await TgBotProgramm(update, user.GetRole(), chatId, cancellationToken);
                                count++;
                                break;
                            }
                            else if (user.GetRole() == 2)
                            {
                                await TgBotProgramm(update, user.GetRole(), chatId, cancellationToken);
                                count++;
                                break;
                            }
                            else
                            {
                                await TgBotProgramm(update, 0, chatId, cancellationToken);
                                count++;
                                break;
                            }
                        }
                    }
                    if (count == 0)
                    {
                        await TgBotProgramm(update, 0, chatId, cancellationToken);
                    }
                }                
                break;
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