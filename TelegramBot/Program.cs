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
using System;
using System.Threading;
using Microsoft.VisualBasic;

TelegramBotClient botClient = new("6762325774:AAHXTbacyLzyYmYh8VYZf7SZuh-Ozh_NxG4");

using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

BotUser[] users = Array.Empty<BotUser>();
string addtxt = "";
int payrateid = 0;
string menuanswer = " \n \nЧто вы хотите сделать?";
string entertxt = "Ответом на это сообщение, введите текст до 70 символов для вашего объявления";
string payratetxt = "Выберите тариф для вашего объявления: \n \n   1. Базовый: до 5 паблишеров за 500 ед/день  \n   2. Средний: до 15 паблишеров за 750 ед/день \n   3. Расширенный: до 30 паблишеров за 1250 ед/день \n   4. Максимум: до 50 паблишеро за 2000 ед/день";
string adddays = "Сколько дней будет активно объявление? \nКоличество дней должно быть целым числом";
string activateadd = "Введите номер заявки, которую надо активировать";
string stopadd = "Введите номер заявки, которую надо приостановить";
string deleteadd = "Введите номер заявки, которую надо удалить";
string addmoney = "Введите сумму на которую хотите пополнить счёт";
string chooseadd = "Ответом на это сообщение, напишите номер заявки, которую хотите выбрать";
SqlConnection myConnection = new("Server=localhost;Database=TgBot;Trusted_Connection=True;");
DateTime dt_start = DateTime.Now;
DataTable payrate_table = new();
DataTable managers_table = new();
string payrate_query = $"select * from PaymentRates";
SqlDataAdapter payrate_adpt = new(payrate_query, myConnection);
payrate_adpt.Fill(payrate_table);
string managers_query = $"select chatID from users where role = '3'";
SqlDataAdapter managers_adpt = new(managers_query, myConnection);
managers_adpt.Fill(managers_table);


ReplyKeyboardMarkup ArkmMenu = new(new[]
{
    new KeyboardButton("Создать новое объявление"),
    new KeyboardButton("Просмотреть личный кабинет"),
})
{ ResizeKeyboard = true };
ReplyKeyboardMarkup ArkmPayRate = new(new[]
{
    new[] {
        new KeyboardButton("Базовый"),
        new KeyboardButton("Средний") },
    new[] {
        new KeyboardButton("Расширенный"),
        new KeyboardButton("Максимум") }
})
{ ResizeKeyboard = true, Selective = true };
ReplyKeyboardMarkup ArkmCabinet = new(new[]
{
    new[]{
        new KeyboardButton("Активировать"),
        new KeyboardButton("Приостановить"),
        new KeyboardButton("Удалить")},
    new[]{
        new KeyboardButton("Вернуться в меню"),
        new KeyboardButton("Пополнить счёт"),
        new KeyboardButton("Выйти")}
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
await botClient.DeleteWebhookAsync();
User me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
var timer = new PeriodicTimer(TimeSpan.FromHours(1));
await CheckAdds();
while (await timer.WaitForNextTickAsync(cts.Token))
{
    await CheckAdds();
}
Console.ReadLine();

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

        var last_check = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);

        string CheckAdds_q = "select publishertoadd.id_add, publishertoadd.id_publisher, text,  last_check, chatId from publishertoadd inner join adds on publishertoadd.id_add = adds.id_add inner join users on publishertoadd.id_publisher = users.id_user where publishertoadd.status = 1";
        SqlDataAdapter CheckAdds_adpt = new(CheckAdds_q, myConnection);
        DataTable CheckAdds_tbl = new();
        CheckAdds_adpt.Fill(CheckAdds_tbl);
        if (CheckAdds_tbl.Rows.Count > 0)
        {
            for (int i = 0; i < CheckAdds_tbl.Rows.Count; i++)
            {
                if (DateTime.Now.Subtract(DateTime.Parse(CheckAdds_tbl.Rows[i][3].ToString().Trim())) >= TimeSpan.Parse("01:00:00"))
                {
                    var user_info = await botClient.GetChatAsync(long.Parse(CheckAdds_tbl.Rows[i][4].ToString().Trim()), cts.Token);
                    if (user_info.Bio is not null)
                    {
                        if (user_info.Bio.Trim().ToLower().Contains(CheckAdds_tbl.Rows[i][2].ToString().Trim().ToLower()))
                        {
                            string upd_last_check_q = $"update publishertoadd set last_check = '{last_check}', active_hours = active_hours + 1 where id_publisher = {int.Parse(CheckAdds_tbl.Rows[i][1].ToString().Trim())} and status = 1";
                            await ConnectToSQL(upd_last_check_q);
                            string upd_publ_acc_q = $"update users set account = account + 90 where id_user = {int.Parse(CheckAdds_tbl.Rows[i][1].ToString().Trim())}";
                            await ConnectToSQL(upd_publ_acc_q);
                        }
                    }
                    else
                    {
                        string upd_publ = $"update publishertoadd set status = 3, last_check = '{last_check}' where id_publisher = {int.Parse(CheckAdds_tbl.Rows[i][1].ToString().Trim())} and status = '1'";
                        await ConnectToSQL(upd_publ);
                        await CommandAndTxt(long.Parse(CheckAdds_tbl.Rows[i][4].ToString().Trim()), $"Так вы убрали текст заявки №{CheckAdds_tbl.Rows[i][0].ToString().Trim()}, то вы больше не привязаны к ней", cts.Token);
                        string SendNote_q = $"select id_add, chatId from adds inner join users on adds.advertiser = users.id_user where id_add = {int.Parse(CheckAdds_tbl.Rows[i][0].ToString().Trim())}";
                        SqlDataAdapter SendNote_adpt = new(SendNote_q, myConnection);
                        DataTable SendNote_tbl = new();
                        SendNote_adpt.Fill(SendNote_tbl);
                        await CommandAndTxt(long.Parse(SendNote_tbl.Rows[0][1].ToString().Trim()), $"Заявка №{SendNote_tbl.Rows[0][0].ToString().Trim()} потеряла 1-го Паблишера, так как он не выполнил условия продления", cts.Token);
                    }
                }
            }
        }

        string AddHours_q = "select id_add, last_hours_add from adds where status = '1'";
        SqlDataAdapter AddHours_adpt = new(AddHours_q, myConnection);
        DataTable AddHours_tbl = new();
        AddHours_adpt.Fill(AddHours_tbl);
        if (AddHours_tbl.Rows.Count > 0)
        {
            for (int i = 0; i < AddHours_tbl.Rows.Count; i++)
            {
                if (DateTime.Now.Subtract(DateTime.Parse(AddHours_tbl.Rows[i][1].ToString().Trim())) >= TimeSpan.Parse("01:00:00"))
                {
                    string upd_q = $"update adds set active_hours = active_hours + 1, last_hours_add = '{last_check}' where id_add = '{int.Parse(AddHours_tbl.Rows[i][0].ToString().Trim())}'";
                    await ConnectToSQL(upd_q);
                }
            }
        }
        

        string CompletedAdds_q = "select id_add, chatId from adds inner join users on adds.advertiser = users.id_user where status = '1' and hours = active_hours";
        SqlDataAdapter CompletedAdds_adpt = new(CompletedAdds_q, myConnection);
        DataTable CompletedAdds_tbl = new();
        CompletedAdds_adpt.Fill(CompletedAdds_tbl);
        if (CompletedAdds_tbl.Rows.Count > 0)
        {
            for (int i = 0; i < CompletedAdds_tbl.Rows.Count; i++)
            {
                string upd_adds_status = $"update adds set status = 3 where id_add = {int.Parse(CompletedAdds_tbl.Rows[i][0].ToString().Trim())}";
                await ConnectToSQL(upd_adds_status);
                await CommandAndTxt(long.Parse(CompletedAdds_tbl.Rows[i][1].ToString().Trim()), $"У заявки №{CompletedAdds_tbl.Rows[i][0].ToString().Trim()} вышло время работы", cts.Token);

                string CompletedAddsToPubl_q = $"select chatId from publishertoadd inner join users on publishertoadd.id_publisher = users.id_user where id_add = {int.Parse(CompletedAdds_tbl.Rows[i][0].ToString().Trim())} and status = '1'";
                SqlDataAdapter CompletedAddsToPubl_adpt = new(CompletedAddsToPubl_q, myConnection);
                DataTable CompletedAddsToPubl_tbl = new();
                CompletedAddsToPubl_adpt.Fill(CompletedAddsToPubl_tbl);
                if (CompletedAddsToPubl_tbl.Rows.Count > 0)
                {
                    for (int j = 0; j < CompletedAddsToPubl_tbl.Rows.Count; j++)
                    {
                        await CommandAndTxt(long.Parse(CompletedAddsToPubl_tbl.Rows[i][0].ToString().Trim()), $"У заявки №{CompletedAdds_tbl.Rows[i][0].ToString().Trim()} вышло время работы", cts.Token);
                    }
                }
                string upd_publtoadd_status = $"update publishertoadd set status = 3 where id_add = {int.Parse(CompletedAdds_tbl.Rows[i][0].ToString().Trim())}";
                await ConnectToSQL(upd_publtoadd_status);
            }
        }
        myConnection.Close();
        Console.WriteLine($" - - - - - - - - - - \n Была проведена проверка всех активных заявок в {DateTime.Now}\n - - - - - - - - - - ");
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
                string msg = "Вы не выбрали роль \nДля начала работы с ботом напишите \"/start\"";
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

            else if (message.Text == "Создать новое объявление")
            {
                Message sentmsg = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: entertxt,
                replyMarkup: new ForceReplyMarkup(),
                cancellationToken: cancellationToken);
            }

            else if(message.Text == "Просмотреть личный кабинет")
            {
                await PersonalCabinet(message, role, chatId, cancellationToken);
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

            else if (message.Text == "Пополнить счёт")
            {
                Message sentmsg = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: addmoney,
                replyMarkup: new ForceReplyMarkup(),
                cancellationToken: cancellationToken);
            }

            else if (message.ReplyToMessage != null)
            {
                if (message.ReplyToMessage.Text == entertxt)
                {
                    if (message.Text.Length <= 70)
                    {
                        addtxt = message.Text;

                        await ChoosePayRent(chatId, cancellationToken, message.MessageId);
                    }
                    else
                    {
                        await SentMenu(role, chatId, cancellationToken, $"Слишком длинное сообщение");
                    }
                }

                else if (message.ReplyToMessage.Text == adddays || message.ReplyToMessage.Text == activateadd || message.ReplyToMessage.Text == stopadd || message.ReplyToMessage.Text == deleteadd || message.ReplyToMessage.Text == addmoney)
                {

                    if (int.TryParse(message.Text, out int x))
                    {
                        if (message.ReplyToMessage.Text == adddays)
                        {
                            if (int.Parse(message.Text) >= 1)
                            {
                                string msg = "";
                                string accCheck_query = $"select account from users where username = '{message.Chat.Username}'";
                                SqlDataAdapter accCheck_adpt = new(accCheck_query, myConnection);
                                DataTable accCheck_table = new();
                                accCheck_adpt.Fill(accCheck_table);
                                int payrate_price = 0;
                                for (int i = 0; i < payrate_table.Rows.Count; i++)
                                {
                                    if (int.Parse(payrate_table.Rows[i][0].ToString().Trim()) == payrateid)
                                    {
                                        payrate_price = int.Parse(payrate_table.Rows[i][3].ToString().Trim());
                                    }
                                }
                                int add_price = int.Parse(message.Text.Trim()) * payrate_price;
                                if (int.Parse(accCheck_table.Rows[0][0].ToString().Trim()) >= add_price)
                                {
                                    var dt_now= new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);

                                    string query = $"insert into Adds (advertiser, text, id_payrate, status, hours, active_hours) values ((select id_user from users where username = '{message.Chat.Username}'), '{addtxt}', {payrateid}, '4', '{int.Parse(message.Text) * 24}', '0', {dt_now})";
                                    await ConnectToSQL(query);
                                    query = $"update users set account = '{int.Parse(accCheck_table.Rows[0][0].ToString().Trim()) - add_price}' where username = '{message.Chat.Username}'";
                                    await ConnectToSQL(query);
                                    msg = $"Объявление успешно создано и отправлено на проверку";

                                    SqlCommand cmd = new SqlCommand();
                                    cmd.CommandText = @"select top 1 id_add from Adds order by id_add desc";
                                    cmd.Connection = myConnection;
                                    int addID = (int)cmd.ExecuteScalar();
                                    if (managers_table.Rows.Count > 0)
                                    {
                                        for (int i = 0; i < managers_table.Rows.Count; i++)
                                        {
                                            await SentToManager(int.Parse(managers_table.Rows[i][0].ToString().Trim()), cancellationToken, addID, message.Chat.Username, addtxt);
                                        }
                                    }
                                    
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

                        else if (message.ReplyToMessage.Text == addmoney)
                        {
                            if (int.Parse(message.Text) >= 0)
                            {

                                string addmoney_q = $"update users set account = account + {int.Parse(message.Text)} where username = '{message.Chat.Username}'";
                                await ConnectToSQL(addmoney_q);
                                await SentCabinet(role, chatId, cancellationToken, "Средства успешно добавлены на счёт");
                                await PersonalCabinet(message, role, chatId, cancellationToken);
                            }
                            else
                            {
                                await CommandAndTxt(chatId, "Количество средств, на которое вы хотите пополнить счёт, должно быть не меньше 0", cancellationToken);
                                await PersonalCabinet(message, role, chatId, cancellationToken);
                            }
                        }

                        else
                        {
                            string setstatus_query = $"select status from adds where id_add = '{message.Text}' and advertiser = (select id_user from users where username = '{message.Chat.Username}')";
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
                                    else
                                    {
                                        string action = "";
                                        if (message.ReplyToMessage.Text == stopadd)
                                        {
                                            action = "пристановлена";
                                            secquery = $"update adds set status = '2' where id_add = '{message.Text}'";
                                            await SentCabinet(role, chatId, cancellationToken, "Статус заявки успешно обновлён!");
                                            
                                        }
                                        else
                                        {
                                            action = "удалена";
                                            secquery = $"update adds set status = '3' where id_add = '{message.Text}'";
                                            await SentCabinet(role, chatId, cancellationToken, "Заявка была успешно удалена!");
                                        }
                                        string upd_p = $"update publishertoadd set status = '3' where id_add = {message.Text}";
                                        await ConnectToSQL(upd_p);
                                        string SendNoteToP_q = $"select chatId from publishertoadd inner join users on publishertoadd.id_publisher = users.id_user where id_add = {message.Text}";
                                        SqlDataAdapter SendNoteToP_adpt = new(SendNoteToP_q, myConnection);
                                        DataTable SendNoteToP_table = new();
                                        SendNoteToP_adpt.Fill(SendNoteToP_table);
                                        if (SendNoteToP_table.Rows.Count != 0)
                                        {
                                            for (int i = 0; i < SendNoteToP_table.Rows.Count; i++)
                                            {
                                                await CommandAndTxt(long.Parse(SendNoteToP_table.Rows[i][0].ToString().Trim()), $"К сожалению заявка №{message.Text}, к которой вы были привязаны, была {action} \n \n Для выбора новой заявки перейдите в меню по команде \"/start\"", cancellationToken);
                                            }
                                        }
                                    }
                                    await ConnectToSQL(secquery);
                                }
                                else
                                {
                                    await CommandAndTxt(chatId, "Такой заявки не существуе", cancellationToken);
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
                await SentConfirm(chatId, cancellationToken, "Вы уверены, что хотите выйти? \n \nВсе ваши заявки будут удалены", message.MessageId);
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
                string findpubladd_query = $"select id_add from publishertoadd where id_publisher = (select id_user from users where username = '{message.Chat.Username}') and status = '1'";                 
                SqlDataAdapter findpubladd_adpt = new(findpubladd_query, myConnection);
                DataTable findpubladd_table = new();
                findpubladd_adpt.Fill(findpubladd_table);
                 
                if (findpubladd_table.Rows.Count != 0)
                {
                    await PublDeleteOrReject(message.Chat.Username.Trim(), findpubladd_table.Rows[0][0].ToString().Trim(), role, chatId, cancellationToken);
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
                string chooseadd_query = $"select * from publishertoadd where id_publisher = (select id_user from users where username = '{message.Chat.Username}') and status <> 3";
                SqlDataAdapter chooseadd_adpt = new(chooseadd_query, myConnection);
                DataTable chooseadd_table = new();
                chooseadd_adpt.Fill(chooseadd_table);
                if (chooseadd_table.Rows.Count == 0)    
                {
                    Message sentmsg = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: chooseadd,
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
                if (message.ReplyToMessage.Text == chooseadd)
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
                                parseMode: ParseMode.Html, 
                                text: $"Вы выбрали заявку №{message.Text} \n \nДля активации внесите текст заявки к себе в описание профиля: \n   \"<code>{setadd_table.Rows[0][0]}</code> \" \n \nПосле этого нажмите \"Подтвердить заявку\"",
                                replyMarkup: null,
                                cancellationToken: cancellationToken);
                            await botClient.EditMessageReplyMarkupAsync(chatId, sentConfirmAdd.MessageId, new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Подтвердить заявку", $"Подтверждение заявки_{message.Text}_{sentConfirmAdd.MessageId}")));
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
                await SentConfirm(chatId, cancellationToken, "Вы уверены, что хотите выйти? \nВы будете удалены с выбранной заявки, если таковая есть", message.MessageId);
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

    Message sentMenu = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: txt,
        replyMarkup: null,
        cancellationToken: cancellationToken);
    InlineKeyboardMarkup ikmSentConfim = new(new[]
    {
        InlineKeyboardButton.WithCallbackData("Да", $"Подтверждено_{sentMenu.MessageId}"),
        InlineKeyboardButton.WithCallbackData("Нет", $"Отмененено_{sentMenu.MessageId}")
    });
    await botClient.EditMessageReplyMarkupAsync(chatId, sentMenu.MessageId, ikmSentConfim);
}

async Task ChoosePayRent(long chatId, CancellationToken cancellationToken, int msgID)
{

    Message choosePayRentmsg = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: payratetxt,
        replyMarkup: null,
        cancellationToken: cancellationToken);
    InlineKeyboardMarkup ikmChoosePayRent = new(new[]
    {
        InlineKeyboardButton.WithCallbackData("Базовый", $"Тариф_1_{choosePayRentmsg.MessageId}"),
        InlineKeyboardButton.WithCallbackData("Средний", $"Тариф_2_{choosePayRentmsg.MessageId}"),
        InlineKeyboardButton.WithCallbackData("Расширенный", $"Тариф_3_{choosePayRentmsg.MessageId}"),
        InlineKeyboardButton.WithCallbackData("Максимум", $"Тариф_4_{choosePayRentmsg.MessageId}")
    });
    await botClient.EditMessageReplyMarkupAsync(chatId, choosePayRentmsg.MessageId, ikmChoosePayRent);

}

async Task SentToManager(long chatId, CancellationToken cancellationToken, int addID, string? username, string txt)
{
    Message checkNewAddMsg = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: $"Новая заявка {addID} от @{username} \n \nТекст: {txt}",
        replyMarkup: null,
        cancellationToken: cancellationToken);

    InlineKeyboardMarkup checkNewAddIkm = new(new[]
    {
        InlineKeyboardButton.WithCallbackData("Подтвердить", $"Менеджер подтвердил_{addID}_{checkNewAddMsg.MessageId}"),
        InlineKeyboardButton.WithCallbackData("Отклонить", $"Менеджер отклонил_{addID}_{checkNewAddMsg.MessageId}")
    });
    await botClient.EditMessageReplyMarkupAsync(chatId, checkNewAddMsg.MessageId, checkNewAddIkm, cancellationToken);
}

async Task ConnectToSQL(string query)
{
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
        string pc_query = "";
        if (role == 1)
        {
            pc_query = $"select adds.id_add, adds.text, addstatuses.status, paymentrates.name, (select COUNT(*) from PublisherToAdd where PublisherToAdd.id_add = adds.id_add and status = '1') as 'active_publs', paymentrates.number_of_publs, paymentrates.price, adds.active_hours, adds.hours from adds inner join addstatuses on adds.status = addstatuses.id_status inner join paymentrates on adds.id_payrate = paymentrates.id_payrate where adds.advertiser = (select id_user from users where username = '{message.Chat.Username}') and adds.status <> '3'";
        }
        else if (role == 2)
        {
            pc_query = $"select publishertoadd.id_add, adds.text, adds.hours, adds.active_hours, publishertoadd.active_hours from publishertoadd inner join adds on publishertoadd.id_add = adds.id_add inner join addstatuses on adds.status = addstatuses.id_status inner join users on adds.advertiser = users.id_user inner join paymentrates on adds.id_payrate = paymentrates.id_payrate where publishertoadd.id_publisher = (select id_user from users where username = '{message.Chat.Username}') and publishertoadd.status <> 3";
        }

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
            if (role == 1)
            {
                for (int i = 0; i < pc_table.Rows.Count; i++)
                {                    
                    row += $"Заявка №{pc_table.Rows[i][0]} \nТекст: {pc_table.Rows[i][1]} \nСтатус: {pc_table.Rows[i][2]} \nТариф: {pc_table.Rows[i][3]} - {pc_table.Rows[i][5]} паблишеров за {pc_table.Rows[i][6]} ед/день \nКол-во активных паблишеров: {pc_table.Rows[i][4]} из {pc_table.Rows[i][5]} \nПрошло {pc_table.Rows[i][4]} из {pc_table.Rows[i][5]} часов \n \n";
                }
            }
            else
            {
                msg = $"Заявка №{pc_table.Rows[0][0]} \nТекст: {pc_table.Rows[0][1]} \nБудет активна ещё {int.Parse(pc_table.Rows[0][2].ToString().Trim()) - int.Parse(pc_table.Rows[0][3].ToString().Trim())} часов \nВы рекламировали {pc_table.Rows[0][4]} часов \nЗаработано {int.Parse(pc_table.Rows[0][3].ToString().Trim()) * 90} \n \n";                
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
        string row = "";
        string query = "select id_add, text, active_hours, hours, status from adds inner join paymentrates on adds.id_payrate = paymentrates.id_payrate where status = '1' and (select count(*) from publishertoadd where publishertoadd.id_add = adds.id_add and status = '1') < paymentrates.number_of_publs";
                 
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

async Task PublDeleteOrReject(string username, string id_add, int? role, long chatId, CancellationToken cancellationToken)
{
    string update_query = $"update publishertoadd set status = 3 where id_publisher = (select id_user from users where username = '{username}')";
    await ConnectToSQL(update_query);
    await SentMenu(role, chatId, cancellationToken, "Вы успешно отказались от заявки");
    string SendNoteToAdv_q = $"select chatId from adds inner join users on adds.advertiser = users.id_user where id_add = {id_add}";
    SqlDataAdapter SendNoteToAdv_adpt = new(SendNoteToAdv_q, myConnection);
    DataTable SendNoteToAdv_table = new();
    SendNoteToAdv_adpt.Fill(SendNoteToAdv_table);
    if (SendNoteToAdv_table.Rows.Count != 0)
    {
        await CommandAndTxt(long.Parse(SendNoteToAdv_table.Rows[0][0].ToString().Trim()), $"К сожалению, паблишер отказался заявки №{id_add} \nТеперь она снова доступна для выбора", cancellationToken);
    }
}

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    try
    {
        long chatId = 0;
        if (myConnection.State == ConnectionState.Open || myConnection.State == ConnectionState.Connecting)
        {
            await myConnection.CloseAsync();
        }
        await myConnection.OpenAsync(cancellationToken);

        DataTable start_table = new();
        if (users.Length == 0)
        {            
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
                        if (user != null && user.GetUsername() == update.CallbackQuery.From.Username)
                        {
                            curr_user = user;
                        }
                    }

                    if (update.CallbackQuery.Data.Contains("Подтверждено"))
                    {
                        string[] cbqData = update.CallbackQuery.Data.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        await botClient.EditMessageReplyMarkupAsync(update.CallbackQuery.From.Id, int.Parse(cbqData[1]), null, cancellationToken);
                        DataTable AddsForDel_table = new();
                        if (curr_user.GetRole() == 1)
                        {
                            string AddsForDel_query = $"select id_publisher from publishertoadd inner join adds on publishertoadd.id_add = adds.id_add inner join users on adds.advertiser = users.id_user where advertiser = (select id_user from users where username = '{update.CallbackQuery.From.Username}') and adds.status = '1' and publishertoadd.status = '1'";
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
                                    await CommandAndTxt(long.Parse(msgToPubl_table.Rows[i][0].ToString().Trim()), "К сожалению, заявка, к который вы были привязаны, была удалена \n \nДля выбора новой заявки перейдите в меню по команде \"/start\"", cancellationToken);
                                }
                            }
                            
                            string findadvadds_query = $"select id_add from publishertoadd where id_publisher = (select id_user from users where username = '{update.CallbackQuery.From.Username.Trim()}') and status = '1'";
                            SqlDataAdapter findadvadds_adpt = new(findadvadds_query, myConnection);
                            DataTable findadvadds_table = new();
                            findadvadds_adpt.Fill(findadvadds_table);
                            if (findadvadds_table.Rows.Count > 0)
                            {
                                for (int i = 0; i < findadvadds_table.Rows.Count; i++)
                                {
                                    string deletePublToAdv1Query = $"update publishertoadd set status = 3 where id_add = '{findadvadds_table.Rows[i][0]}'";
                                    await ConnectToSQL(deletePublToAdv1Query);
                                }
                            }
                            string deleteAdvQuery = $"update adds set status = 3 where advertiser = (select id_user from users where username = '{update.CallbackQuery.From.Username}')";
                            await ConnectToSQL(deleteAdvQuery);

                        }
                        else if (curr_user.GetRole() == 2)
                        {
                            string findpubladd_query = $"select id_add from publishertoadd where id_publisher = (select id_user from users where username = '{update.CallbackQuery.From.Username.Trim()}') and status = '1'";
                            SqlDataAdapter findpubladd_adpt = new(findpubladd_query, myConnection);
                            DataTable findpubladd_table = new();
                            findpubladd_adpt.Fill(findpubladd_table);

                            if (findpubladd_table.Rows.Count != 0)
                            {
                                await PublDeleteOrReject(update.CallbackQuery.From.Username.Trim(), findpubladd_table.Rows[0][0].ToString().Trim(), curr_user.GetRole(), chatId, cancellationToken);
                            }
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
                        await botClient.EditMessageReplyMarkupAsync(update.CallbackQuery.From.Id, int.Parse(cbqData[1]), null, cancellationToken);
                        await SentMenu(curr_user.GetRole(), chatId, cancellationToken, "Команда отменена");
                    }

                    else if (update.CallbackQuery.Data.Contains("Подтверждение заявки"))
                    {
                        string[] cbqData = update.CallbackQuery.Data.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        await botClient.EditMessageReplyMarkupAsync(update.CallbackQuery.From.Id, int.Parse(cbqData[2]), null, cancellationToken);
                        string PublDescCheck_query = $"select text from adds where id_add = '{cbqData[1]}'";
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
                                string update_query = $"insert into publishertoadd (id_add, id_publisher, status, active_hours, last_check) values ({cbqData[1]}, (select id_user from users where username = '{update.CallbackQuery.From.Username}'), '1', '0', '{last_check}')";
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

                    else if (update.CallbackQuery.Data.Contains("Тариф"))
                    {
                        string[] cbqData = update.CallbackQuery.Data.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        await botClient.EditMessageReplyMarkupAsync(update.CallbackQuery.From.Id, int.Parse(cbqData[2]), null, cancellationToken);
                        payrateid = int.Parse(cbqData[1]);
                        Message sentmsg = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: adddays,
                            replyMarkup: new ForceReplyMarkup(),
                            cancellationToken: cancellationToken);
                    }

                    else if (update.CallbackQuery.Data.Contains("Менеджер"))
                    {
                        string[] cbqData = update.CallbackQuery.Data.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        string addID = cbqData[1];
                        string msgID = cbqData[2];
                        await botClient.EditMessageReplyMarkupAsync(update.CallbackQuery.From.Id, int.Parse(msgID), null, cancellationToken);
                        
                        DataTable addinfo_table = new();
                        string addinfo_query = $"select chatid, advertiser, hours, price from adds inner join users on adds.advertiser = users.id_user inner join paymentrates on adds.id_payrate = paymentrates.id_payrate where id_add = '{addID}'";
                        SqlDataAdapter addinfo_adpt = new(addinfo_query, myConnection);
                        addinfo_adpt.Fill(addinfo_table);

                        if (update.CallbackQuery.Data.Contains("подтвердил"))
                        {
                            await CommandAndTxt(update.CallbackQuery.From.Id, $"Завяка №{addID} была подтверждена!", cancellationToken);
                            await CommandAndTxt(long.Parse(addinfo_table.Rows[0][0].ToString().Trim()), $"Завяка №{addID} была подтверждена!", cancellationToken);
                            string upd_add = $"update adds set status = '2' where id_add = '{addID}'";
                            await ConnectToSQL(upd_add);
                        }
                        else
                        {
                            await CommandAndTxt(update.CallbackQuery.From.Id, $"Завяка №{addID} была отклонена!", cancellationToken);
                            await CommandAndTxt(long.Parse(addinfo_table.Rows[0][0].ToString().Trim()), $"Завяка №{addID} была отклонена менеджером!", cancellationToken);
                            string upd_add = $"update adds set status = '3' where id_add = '{addID}'";
                            await ConnectToSQL(upd_add);
                            string upd_user = $"update users set account = account + '{int.Parse(addinfo_table.Rows[0][2].ToString().Trim()) * int.Parse(addinfo_table.Rows[0][3].ToString().Trim()) / 24}' where id_user = '{int.Parse(addinfo_table.Rows[0][1].ToString().Trim())}'";
                            await ConnectToSQL(upd_user);
                        }
                    }
                }
                break;
            case UpdateType.Message:
                var message = update.Message;
                chatId = message.Chat.Id;
                int result = DateTime.Compare(message.Date, dt_start);
                if (result < 0)
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
                        old_user.SetChatId(long.Parse(start_table.Rows[i][2].ToString().Trim()));
                        users[i] = old_user;

                    }
                    int count = 0;
                    foreach (BotUser user in users)
                    {
                        if (user != null && user.GetUsername() == message.Chat.Username)
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