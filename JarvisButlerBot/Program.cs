using JarvisButlerBot.DefaultModules;
using JarvisButlerBot.Helpers;
using JarvisModuleCore.Attributes;
using JarvisModuleCore.Classes;
using JarvisModuleCore.ML;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static bool update = false;
        private static readonly string gitDirectory = Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\..\\");

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

            Console.WriteLine("Signaling start to modules");
            foreach (var module in Modules) module.Start(jarvis);

            Console.WriteLine("Startup finished");
            stopHandle.WaitOne();
            Console.WriteLine("Shutting down");
            jarvis.StopReceiving();

            if (update)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.CurrentDirectory, "Update\\update.bat"),
                    WorkingDirectory = gitDirectory
                };
                Process.Start(psi);
            }
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
            LoadModule(new MLDataModule());
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

            IEnumerable<IGrouping<string, TaskPredictionInput>> groupings = inputs.GroupBy(x => x.TaskId);
            foreach (var notEnoughtData in groupings.Where(x => x.Count() < 50))
            {
                Console.WriteLine($"Warning: Only {notEnoughtData.Count()} lines of training data for task {notEnoughtData.Key}, recommended amount is 50.");
            }
            foreach (var noData in Tasks.Where(x => !groupings.Any(y => y.Key == x.Key)))
            {
                Console.WriteLine($"Warning: No data found for task {noData.Key}, recommended amount of lines is 50.");
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
            if (jarvis.IsGlobalAdmin(e.Message.From.Id) && e.Message.Type == MessageType.Text)
            {
                if (e.Message.Text == "/stop" || e.Message.Text.ToLower() == $"/stop@{jarvis.Username.ToLower()}")
                {
                    await jarvis.ReplyAsync(e.Message, "Stopping the bot!");
                    stopHandle.Set();
                    return;
                }
                if (e.Message.Text == "/update" || e.Message.Text.ToLower() == $"/update@{jarvis.Username.ToLower()}")
                {
                    await jarvis.ReplyAsync(e.Message, "Updating the bot!");
                    update = true;
                    stopHandle.Set();
                    return;
                }
            }

            string text = e.Message.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;
            MessageEntity[] entities = e.Message.GetEntities();

            bool botCommandAtStart(MessageEntity x) => x.Type == MessageEntityType.BotCommand && x.Offset == 0;
            PossibleMessageTypes msgType = e.Message.GetMessageType();
            PossibleChatTypes chatType = e.Message.Chat.GetChatType();

            if (text.ToLower().Contains($"@{jarvis.Username}".ToLower()) || e.Message.Chat.Type == ChatType.Private || e.Message.ReplyToMessage?.From?.Id == jarvis.BotId)
            {
                string hasReplyToMessage = (e.Message.ReplyToMessage != null).ToString();
                var input = new TaskPredictionInput
                {
                    ChatType = chatType.ToString(),
                    HasReplyToMessage = hasReplyToMessage,
                    MessageText = text.PrepareForPrediction(entities, jarvis.Username),
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
                try
                {
                    task.Delegate.Invoke(e.Message, jarvis);
                }
                catch (Exception ex)
                {
                    await jarvis.ReplyAsync(e.Message, $"An error occurred in task {task.Attributes.TaskId}:\n{ex}");
                }
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
                    try
                    {
                        task.Delegate.Invoke(e.Message, jarvis);
                    }
                    catch (Exception ex)
                    {
                        await jarvis.ReplyAsync(e.Message, $"An error occurred in task {task.Attributes.TaskId}:\n{ex}");
                    }
                }
            }
        }
        #endregion
    }
}
