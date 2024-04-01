using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    internal class BotUser
    {
        private string? username {  get; set; }
        private int? role { get; set; }
        private long? chatId { get; set; }

        public void SetUsername(string? username)
        {
            this.username = username;
        }

        public string? GetUsername() 
        {
            return username;
        }

        public void SetRole(int? role)
        {
            this.role = role;
        }

        public int? GetRole()
        {
            return role;
        }

        public void SetChatId(long? chatId)
        {
            this.chatId = chatId;
        }

        public long? GetChatId()
        {
            return chatId;
        }

        public void PrintData()
        {
            Console.WriteLine($"UN: \"{username}\" | Role: {role} | CID: {chatId}");
        }
    }
}
