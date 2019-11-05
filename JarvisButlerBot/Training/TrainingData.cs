using JarvisModuleCore.ML;
using Newtonsoft.Json;
using System.IO;

namespace JarvisButlerBot.Training
{
    public class TrainingData
    {
        private static readonly string trainingDataDirectory = "Training";

        public static TaskPredictionInput[] Ping
        {
            get
            {
                return JsonConvert.DeserializeObject<TaskPredictionInput[]>(File.ReadAllText(Path.Combine(trainingDataDirectory, "Ping.json")));
            }
        }
    }
}
