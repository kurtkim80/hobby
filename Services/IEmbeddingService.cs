namespace HobbyCollector.Services;

public interface IEmbeddingService : IDisposable
{
    int Dimension { get; }
    string ModelName { get; }
    float[] GenerateEmbedding(string text);
}
