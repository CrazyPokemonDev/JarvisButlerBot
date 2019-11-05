using JarvisButlerBot.Training;
using JarvisModuleCore.Classes;
using JarvisModuleCore.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using JarvisModuleCore.Attributes;

namespace JarvisButlerBot.DefaultModules
{
    [JarvisModule]
    public class PingModule : JarvisModule
    {
        public override string Id => "jarvis.default.ping";
        public override string Name => "Ping module";
        public override Version Version => Version.Parse("1.0.0");
        public override TaskPredictionInput[] MLTrainingData => TrainingData.Ping;

        [JarvisTask("jarvis.default.ping.ping", Command = "/ping", PossibleMessageTypes = PossibleMessageTypes.All ^ PossibleMessageTypes.Poll)]
        public async void Ping(Message message, Jarvis jarvis)
        {
            await jarvis.ReplyAsync(message, "Yes, I am here.");
        }
    }
}
