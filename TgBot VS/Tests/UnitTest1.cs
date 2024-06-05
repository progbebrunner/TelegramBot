using TelegramBot;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        Testing tst = new Testing();
        
        [TestMethod]
        public void ChooseRole()
        {            
            Assert.IsTrue(tst.CheckRole("Паблишер"));
        }

        [TestMethod]
        public void CorrectDays()
        {
            Assert.IsTrue(tst.CheckDays("7"));
        }

        [TestMethod]
        public void EnoughMoney()
        {
            Assert.IsFalse(tst.CheckMoney(111));
        }

        [TestMethod]
        public void ExistingAdd()
        {
            Assert.IsTrue(tst.CheckAdd(69));
        }

        [TestMethod]
        public void RightDesc()
        {
            Assert.IsTrue(tst.CheckDesc("реклама"));
        }

        [TestMethod]
        public void Refill()
        {
            Assert.IsFalse(tst.CheckRefill(-100));
        }
    }
}