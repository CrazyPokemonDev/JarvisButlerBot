using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JarvisModuleCore.Classes;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace JarvisCoreTests
{
    [TestClass]
    public class JarvisTests
    {
        private Jarvis jarvis;
        private int[] globalAdmins;
        private int testUserId;
        private Random rnd;
        [TestInitialize]
        public void Init()
        {
            rnd = new Random();
            TestConfig testConfig = JsonConvert.DeserializeObject<TestConfig>(File.ReadAllText("test_config.json"));
            globalAdmins = testConfig.GlobalAdmins;
            testUserId = testConfig.TestUser;
            jarvis = new Jarvis(testConfig.BotToken, testConfig.GlobalAdmins);
            jarvis.StartReceiving();
        }

        [TestMethod]
        public void IsGlobalAdminTest()
        {
            if (globalAdmins.Length > 0) Assert.IsTrue(jarvis.IsGlobalAdmin(globalAdmins.GetRandomElement(rnd)));
            for (int i = 0; i < 5; i++)
            {
                int randomId = rnd.Next();
                Assert.AreEqual(globalAdmins.Contains(randomId), jarvis.IsGlobalAdmin(randomId), 
                    $"Recognized user ID as global admin that should not have been recognized: {randomId}");
            }
        }

        [TestMethod]
        public async void SendMessageAndWaitForReplyTest()
        {
            const string messageText = "Please reply to this message!";
            var messageReplied = await jarvis.SendTextMessageAndWaitForReplyAsync(testUserId, messageText);

            Assert.IsNotNull(messageReplied.ReplyToMessage);
            Assert.AreEqual(messageReplied.ReplyToMessage.Text, messageText);
        }

        [TestMethod]
        public async void ReplyMessageTest()
        {
            var message1 = await jarvis.SendTextMessageAsync(testUserId, "This is a test message.");
            var message2 = await jarvis.ReplyAsync(message1, "This is a test reply.");

            Assert.IsNotNull(message2.ReplyToMessage);
            Assert.AreEqual(message1.MessageId, message2.ReplyToMessage.MessageId);
            Assert.AreEqual(message1.Chat.Id, message2.ReplyToMessage.Chat.Id);
            Assert.AreEqual(message1.Text, message2.ReplyToMessage.Text);
        }

        [TestCleanup]
        public void Cleanup()
        {
            jarvis.StopReceiving();
        }
    }
}
