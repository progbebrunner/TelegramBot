using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace TelegramBot
{
    public class Testing
    {        
        public bool CheckRole(string msg)
        {
            string role1 = "Паблишер";
            string role2 = "Рекламодатель";
            if (msg == role1 || msg == role2)
            {
                return true;
            } else return false;
        }

        public bool CheckDays(string msg) 
        {
            if(int.TryParse(msg, out int x))
            {
                return true;
            } else return false;
        }

        public bool CheckMoney(int x)
        {
            int account = 100;
            if (account >= x)
            {
                return true;
            }
            else return false;
        }

        public bool CheckAdd(int n)
        {
            int adds = 100;
            if (adds >= n)
            {
                return true;
            }
            else return false;
        }

        public bool CheckDesc(string s)
        {
            string desc = "реклама";
            if (desc == s)
            {
                return true;
            }
            else return false;
        }

        public bool CheckRefill(int m)
        {
            if (m >= 0)
            {
                return true;
            }
            else return false;
        }
    }
}
