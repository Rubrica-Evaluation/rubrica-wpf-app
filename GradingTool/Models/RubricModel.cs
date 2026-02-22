using System.Text.Json;
using System.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace GradingTool.Models
{
    public class ObservableCollectionConverter : System.Text.Json.Serialization.JsonConverter<ObservableCollection<string>>
    {
        public override ObservableCollection<string> Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? new List<string>();
            return new ObservableCollection<string>(list);
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, ObservableCollection<string> value, System.Text.Json.JsonSerializerOptions options)
        {
            System.Text.Json.JsonSerializer.Serialize(writer, value.ToList(), options);
        }
    }

    public class RubricModel
    {
        [JsonPropertyName("meta")]
        public RubricMeta Meta { get; set; } = new();

        [JsonPropertyName("penalties")]
        public List<PenaltyItemModel> Penalties { get; set; } = new();

        [JsonPropertyName("criteria")]
        public List<CriterionModel> Criteria { get; set; } = new();

        [JsonPropertyName("computed")]
        public ComputedModel Computed { get; set; } = new();
    }

    public class RubricMeta
    {
        [JsonPropertyName("tp")]
        public string Tp { get; set; } = string.Empty;

        [JsonPropertyName("student")]
        public StudentModel Student { get; set; } = new();
    }

    public class StudentModel
    {
        [JsonPropertyName("da")]
        public string Da { get; set; } = string.Empty;

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("group")]
        public string Group { get; set; } = string.Empty;

        [JsonPropertyName("groupCode")]
        public string GroupCode { get; set; } = string.Empty;

        [JsonPropertyName("team")]
        public int Team { get; set; }
    }

    public partial class PenaltyItemModel : ObservableObject
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ComputedPenalty))]
        [JsonPropertyName("count")]
        private int _count;

        [JsonPropertyName("factor")]
        public double Factor { get; set; }

        [ObservableProperty]
        [JsonPropertyName("reason")]
        private string _reason = string.Empty;

        [JsonPropertyName("min")]
        public double Min { get; set; }

        [JsonIgnore]
        public double ComputedPenalty => Count * Factor;
    }

    public class ScaleItemModel
    {
        [JsonPropertyName("qualitative")]
        public string Qualitative { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("points")]
        public int Points { get; set; }
    }

    public partial class CriterionModel : ObservableObject
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("scale")]
        public List<ScaleItemModel> Scale { get; set; } = new();

        [JsonPropertyName("weight")]
        public int Weight { get; set; }

        [ObservableProperty]
        [JsonPropertyName("result")]
        private string _result = string.Empty;

        [JsonPropertyName("feedback")]
        [System.Text.Json.Serialization.JsonConverter(typeof(ObservableCollectionConverter))]
        public ObservableCollection<string> Feedback { get; set; } = new();

        [ObservableProperty]
        [JsonPropertyName("points")]
        private double? _points;

        [JsonIgnore]
        public ObservableCollection<string> SuggestedComments { get; } = new();

        [JsonIgnore]
        public bool IsEditingFeedback
        {
            get => _isEditingFeedback;
            set => SetProperty(ref _isEditingFeedback, value);
        }
        private bool _isEditingFeedback;

        [JsonIgnore]
        public int EditingFeedbackIndex
        {
            get => _editingFeedbackIndex;
            set => SetProperty(ref _editingFeedbackIndex, value);
        }
        private int _editingFeedbackIndex = -1;

        [JsonIgnore]
        public string FeedbackInput
        {
            get => _feedbackInput;
            set => SetProperty(ref _feedbackInput, value);
        }
        private string _feedbackInput = string.Empty;

        partial void OnResultChanged(string value)
        {
            RecalculatePoints();
        }

        private void RecalculatePoints()
        {
            if (!string.IsNullOrEmpty(Result))
            {
                var selectedScale = Scale.FirstOrDefault(s => s.Qualitative == Result);
                if (selectedScale != null)
                {
                    // Calcul des points : points du niveau * poids / 100
                    Points = (double)selectedScale.Points * Weight / 100.0;
                }
            }
            else
            {
                Points = null;
            }
        }
    }

    public class ComputedModel
    {
        [JsonPropertyName("total")]
        public double? Total { get; set; }
    }
}
