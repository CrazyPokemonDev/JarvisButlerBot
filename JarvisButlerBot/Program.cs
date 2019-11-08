using JarvisButlerBot.DefaultModules;
using JarvisButlerBot.Helpers;
using JarvisModuleCore.Attributes;
using JarvisModuleCore.Classes;
using JarvisModuleCore.ML;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace JarvisButlerBot
{
    class Program
    {
        private static readonly string baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Crazypokemondev\\JarvisButlerBot\\");
        private static readonly string moduleDirectory = Path.Combine(baseDirectory, "modules");
        private static readonly string libraryDirectory = Path.Combine(baseDirectory, "lib");
        private static readonly string botTokenPath = Path.Combine(baseDirectory, "bot.token");
        private static readonly string globalAdminsPath = Path.Combine(baseDirectory, "globalAdmins.txt");
        private static Jarvis jarvis;
        private static readonly ManualResetEvent stopHandle = new ManualResetEvent(false);
        internal static readonly List<JarvisModule> Modules = new List<JarvisModule>();
        private static readonly Dictionary<string, (ExecuteTask Delegate, JarvisTaskAttribute Attributes)> Tasks = new Dictionary<string, (ExecuteTask task, JarvisTaskAttribute attributes)>();
        private static readonly MLContext mlContext = new MLContext(seed: 0);
        private static ITransformer trainedModel;
        private static PredictionEngine<TaskPredictionInput, TaskPrediction> predictionEngine;

        #region Startup
        static void Main()
        {
            Console.WriteLine("Setting up base directory");
            SetupBaseDirectory();

            Console.WriteLine("Loading Modules");
            LoadDefaultModules();
            LoadModules(Directory.CreateDirectory(moduleDirectory));

            Console.WriteLine("Training model");
            TrainModel();

            Console.WriteLine("Waking up JARVIS");
            jarvis = new Jarvis(token: File.ReadAllText(botTokenPath), File.ReadAllLines(globalAdminsPath).Select(x => int.Parse(x)).ToArray());
            jarvis.OnMessage += Jarvis_OnMessage;
            jarvis.StartReceiving();

            Console.WriteLine("Startup finished");
            stopHandle.WaitOne();
            Console.WriteLine("Shutting down");
            jarvis.StopReceiving();
        }

        private static void SetupBaseDirectory()
        {
            Directory.CreateDirectory(baseDirectory);
            Directory.CreateDirectory(libraryDirectory);
            if (!File.Exists(botTokenPath))
            {
                Console.WriteLine("No bot token found, please enter:");
                var token = Console.ReadLine();
                File.WriteAllText(botTokenPath, token);
            }
            if (!File.Exists(globalAdminsPath))
            {
                File.WriteAllText(globalAdminsPath, "267376056");
            }
        }

        private static void LoadDefaultModules()
        {
            LoadModule(new PingModule());
            LoadModule(new ReflectionModule());
        }

        private static void LoadModules(DirectoryInfo moduleDirectory)
        {
            foreach (var file in moduleDirectory.EnumerateFiles())
            {
                if (file.Extension != ".dll") continue;
                var assembly = Assembly.LoadFile(file.FullName);
                foreach (var type in assembly.GetExportedTypes())
                {
                    var typeAttribute = type.GetCustomAttribute<JarvisModuleAttribute>();
                    if (typeAttribute == null) continue;
                    JarvisModule module = (JarvisModule)type.GetConstructor(new Type[0]).Invoke(new object[0]);
                    if (Modules.Any(x => x.Id == module.Id && x.Version > module.Version)) continue;
                    else if (Modules.Any(x => x.Id == module.Id && x.Version == module.Version)) throw new Exception($"Two instances of module {module.Name}({module.Id}) with identical version found");
                    else Modules.RemoveAll(x => x.Id == module.Id && x.Version < module.Version);
                    foreach (var dependency in typeAttribute.Dependencies)
                    {
                        var fileToGet = Path.Combine(libraryDirectory, dependency);
                        File.Copy(fileToGet, Path.GetFileName(fileToGet));
                    }
                    LoadModule(module);
                }
            }
            foreach (var dir in moduleDirectory.EnumerateDirectories())
            {
                LoadModules(dir);
            }
        }

        private static void LoadModule(JarvisModule module)
        {
            Modules.Add(module);
            foreach (var method in module.GetType().GetMethods())
            {
                var methodAttribute = method.GetCustomAttribute<JarvisTaskAttribute>();
                if (methodAttribute == null) continue;
                var task = (ExecuteTask)Delegate.CreateDelegate(typeof(ExecuteTask), module, method);
                Tasks.Add(methodAttribute.TaskId, (task, methodAttribute));
            }
        }

        private static void TrainModel()
        {
            List<TaskPredictionInput> inputs = new List<TaskPredictionInput>();
            /* Dummy data
            for (int i = 0; i < 50; i++) inputs.Add(new TaskPredictionInput { ChatType = "Private", MessageType = "Text", MessageText = "JARVIS, entferne diese Person.", TaskId = "kick" });
            for (int i = 0; i < 50; i++) inputs.Add(new TaskPredictionInput { ChatType = "Private", MessageType = "Text", MessageText = "Geh", TaskId = "leave" });
            for (int i = 0; i < 50; i++) inputs.Add(new TaskPredictionInput { ChatType = "Private", MessageType = "Text", MessageText = "Baum", TaskId = "scream" });
            for (int i = 0; i < 50; i++) inputs.Add(new TaskPredictionInput { ChatType = "Private", MessageType = "Text", MessageText = "Eine rote Nase lacht selten blöd.", TaskId = "think" });
            for (int i = 0; i < 50; i++) inputs.Add(new TaskPredictionInput { ChatType = "Private", MessageType = "Text", MessageText = "HIER FLIEGT GLEICH ALLES IN DIE LUFT!", TaskId = "explode" });*/

            foreach (var module in Modules) inputs.AddRange(module.MLTrainingData);

            foreach (var notEnoughtData in inputs.GroupBy(x => x.TaskId).Where(x => x.Count() < 50))
            {
                Console.WriteLine($"Warning: Only {notEnoughtData.Count()} lines of training data for task {notEnoughtData.Key}, recommended amount is 50.");
            }

            inputs.Shuffle(seed: 0);

            var trainingData = mlContext.Data.LoadFromEnumerable(inputs);
            var pipeline = ProcessData();
            BuildAndTrainModel(trainingData, pipeline);
        }

        private static IEstimator<ITransformer> ProcessData()
        {
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: nameof(TaskPredictionInput.TaskId), outputColumnName: "Label")
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(inputColumnName: nameof(TaskPredictionInput.MessageType), outputColumnName: "MessageTypeEncoded"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(inputColumnName: nameof(TaskPredictionInput.ChatType), outputColumnName: "ChatTypeEncoded"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(inputColumnName: nameof(TaskPredictionInput.HasReplyToMessage), outputColumnName: "HasReplyToMessageEncoded"))
                .Append(mlContext.Transforms.Text.FeaturizeText(inputColumnName: nameof(TaskPredictionInput.MessageText), outputColumnName: "MessageTextFeaturized"))
                .Append(mlContext.Transforms.NormalizeLpNorm("MessageTextFeaturized"))
                .Append(mlContext.Transforms.Concatenate("Features", "MessageTextFeaturized", "MessageTypeEncoded", "ChatTypeEncoded", "HasReplyToMessageEncoded"))
                .AppendCacheCheckpoint(mlContext);
            return pipeline;
        }

        private static IEstimator<ITransformer> BuildAndTrainModel(IDataView data, IEstimator<ITransformer> pipeline)
        {
            //var trainingPipeline = pipeline.Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
            var averagedPerceptronBinaryTrainer = mlContext.BinaryClassification.Trainers.AveragedPerceptron("Label", "Features", numberOfIterations: 10);
            // Compose an OVA (One-Versus-All) trainer with the BinaryTrainer.
            // In this strategy, a binary classification algorithm is used to train one classifier for each class, "
            // which distinguishes that class from all other classes. Prediction is then performed by running these binary classifiers, "
            // and choosing the prediction with the highest confidence score.
            var trainer = mlContext.MulticlassClassification.Trainers.OneVersusAll(averagedPerceptronBinaryTrainer);
            var trainingPipeline = pipeline.Append(trainer)
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
            trainedModel = trainingPipeline.Fit(data);
            predictionEngine = mlContext.Model.CreatePredictionEngine<TaskPredictionInput, TaskPrediction>(trainedModel);
            return trainingPipeline;
        }
        #endregion

        #region Message Handling
        private static async void Jarvis_OnMessage(object sender, MessageEventArgs e)
        {
            if (jarvis.IsGlobalAdmin(e.Message.From.Id) && e.Message.Type == MessageType.Text && (e.Message.Text == "/stop" || e.Message.Text == $"/stop@{jarvis.Username}"))
            {
                await jarvis.ReplyAsync(e.Message, "Stopping the bot!");
                stopHandle.Set();
            }

            string text = GetText(e.Message);
            if (string.IsNullOrWhiteSpace(text)) return;
            MessageEntity[] entities = GetEntities(e.Message);

            bool botCommandAtStart(MessageEntity x) => x.Type == MessageEntityType.BotCommand && x.Offset == 0;
            PossibleMessageTypes msgType = GetMessageType(e.Message);
            PossibleChatTypes chatType = GetChatType(e.Message.Chat);

            if (text.ToLower().Contains($"@{jarvis.Username}".ToLower()) || e.Message.Chat.Type == ChatType.Private || e.Message.ReplyToMessage?.From?.Id == jarvis.BotId)
            {
                string hasReplyToMessage = (e.Message.ReplyToMessage != null).ToString();
                var input = new TaskPredictionInput
                {
                    ChatType = chatType.ToString(),
                    HasReplyToMessage = hasReplyToMessage,
                    MessageText = PrepareForPrediction(text, entities),
                    MessageType = msgType.ToString()
                };
                var prediction = predictionEngine.Predict(input);
                var taskId = prediction.TaskId;
                /*VBuffer<ReadOnlyMemory<char>> names = default;
                predictionEngine.OutputSchema["Score"].Annotations.GetValue("SlotNames", ref names);
                for (int i = 0; i < prediction.Score.Length; i++) Console.WriteLine("{0}: {1}", names.GetItemOrDefault(i), prediction.Score[i]);*/

                if (!Tasks.ContainsKey(taskId))
                {
                    Parallel.ForEach(jarvis.GlobalAdmins, async x => await jarvis.SendTextMessageAsync(x, $"I wanted to execute the task with ID {taskId}, but I couldn't find it!"));
                    return;
                }
                var task = Tasks[taskId];
                if (!task.Attributes.PossibleMessageTypes.HasFlag(msgType)) return;
                if (!task.Attributes.PossibleChatTypes.HasFlag(chatType))
                {
                    await jarvis.ReplyAsync(e.Message, "I believe this isn't possible in here, sorry!");
                    return;
                }
                task.Delegate.Invoke(e.Message, jarvis);
            }
            else if (entities.Any(botCommandAtStart))
            {
                var entity = entities.First(botCommandAtStart);
                string cmd = text.Substring(entity.Offset, entity.Length);
                if (cmd.Contains("@")) return;

                foreach (var task in Tasks.Where(x => x.Value.Attributes.Commands.Contains(cmd)
                    && x.Value.Attributes.PossibleMessageTypes.HasFlag(msgType)
                    && x.Value.Attributes.PossibleChatTypes.HasFlag(chatType))
                .Select(x => x.Value))
                {
                    task.Delegate.Invoke(e.Message, jarvis);
                }
            }
        }

        private static string PrepareForPrediction(string text, MessageEntity[] entities)
        {
            foreach (var entity in entities)
            {
                switch (entity.Type)
                {
                    case MessageEntityType.Mention:
                        if (text.Substring(entity.Offset, entity.Length).ToLower() == jarvis.Username.ToLower()) continue;
                        text = text.Substring(0, entity.Offset) + "@User" + text.Substring(entity.Offset + entity.Length);
                        foreach (var e in entities.Except(new MessageEntity[] { entity }))
                        {
                            if (e.Offset >= entity.Offset + entity.Length) e.Offset -= entity.Length - 5;
                        }
                        break;
                    case MessageEntityType.TextMention:
                        text = text.Substring(0, entity.Offset) + "@Mention" + text.Substring(entity.Offset + entity.Length);
                        foreach (var e in entities.Except(new MessageEntity[] { entity }))
                        {
                            if (e.Offset >= entity.Offset + entity.Length) e.Offset -= entity.Length - 8;
                        }
                        break;
                }
            }
            text = Regex.Replace(text, Regex.Escape($"@{jarvis.Username}".ToLower()), "@Username");
            return text;
        }

        private static string GetText(Message message)
        {
            switch (message.Type)
            {
                case MessageType.Audio:
                case MessageType.Voice:
                case MessageType.Photo:
                case MessageType.Video:
                case MessageType.Document:
                    return message.Caption;
                case MessageType.Poll:
                    return message.Poll.Question;
                case MessageType.Text:
                    return message.Text;
                default:
                    return null;
            }
        }

        private static MessageEntity[] GetEntities(Message message)
        {
            switch (message.Type)
            {
                case MessageType.Audio:
                case MessageType.Voice:
                case MessageType.Photo:
                case MessageType.Video:
                case MessageType.Document:
                    return message.CaptionEntities ?? new MessageEntity[0];
                case MessageType.Text:
                    return message.Entities ?? new MessageEntity[0];
                default:
                    return new MessageEntity[0];
            }
        }

        private static PossibleMessageTypes GetMessageType(Message message)
        {
            switch (message.Type)
            {
                case MessageType.Audio:
                    return PossibleMessageTypes.Audio;
                case MessageType.Voice:
                    return PossibleMessageTypes.Voice;
                case MessageType.Photo:
                    return PossibleMessageTypes.Photo;
                case MessageType.Video:
                    return PossibleMessageTypes.Video;
                case MessageType.Document:
                    if (message.Animation != null) return PossibleMessageTypes.Animation;
                    else return PossibleMessageTypes.Document;
                case MessageType.Poll:
                    return PossibleMessageTypes.Poll;
                case MessageType.Text:
                    return PossibleMessageTypes.Text;
                default:
                    return PossibleMessageTypes.All;
            }
        }

        private static PossibleChatTypes GetChatType(Chat chat)
        {
            switch (chat.Type)
            {
                case ChatType.Private:
                    return PossibleChatTypes.Private;
                case ChatType.Group:
                    return PossibleChatTypes.Group;
                case ChatType.Channel:
                    return PossibleChatTypes.Channel;
                case ChatType.Supergroup:
                    return PossibleChatTypes.Supergroup;
                default:
                    return PossibleChatTypes.All;
            }
        }
        #endregion
    }
}
