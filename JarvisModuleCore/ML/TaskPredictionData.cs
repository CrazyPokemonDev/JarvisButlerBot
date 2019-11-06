using Microsoft.ML.Data;

namespace JarvisModuleCore.ML
{
    public class TaskPredictionInput
    {
        [LoadColumn(0)]
        public string MessageText { get; set; }
        [LoadColumn(1)]
        public string ChatType { get; set; }
        [LoadColumn(2)]
        public string MessageType { get; set; }
        [LoadColumn(3)]
        public string HasReplyToMessage { get; set; }
        [LoadColumn(4)]
        public string TaskId { get; set; }
    }

    public class TaskPrediction
    {
        [ColumnName("PredictedLabel")]
        public string TaskId;

        public float[] Score;
    }
}
