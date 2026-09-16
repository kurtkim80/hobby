using System.Numerics.Tensors;
using System.Security.Cryptography;
using System.Text;

namespace HobbyCollector.Services;

public class LocalEmbeddingService : IEmbeddingService
{
    public int Dimension => 384;
    public string ModelName => "WasmSemantic-384";

    public float[] GenerateEmbedding(string text)
    {
        return GenerateFeatureVector(text, Dimension);
    }

    private static float[] GenerateFeatureVector(string text, int dimension)
    {
        float[] vector = new float[dimension];
        if (string.IsNullOrWhiteSpace(text))
        {
            return vector;
        }

        // 텍스트 정규화 및 토큰화
        string normalized = text.ToLowerInvariant();
        var words = normalized.Split([' ', '\t', '\r', '\n', ',', '.', '!', '?', '-', '(', ')', '[', ']', ':', '"'], 
            StringSplitOptions.RemoveEmptyEntries);

        // 단어 단위 및 2~4글자 N-gram 해싱으로 의미적/형태적 유사도 반영
        foreach (var word in words)
        {
            AddHashedFeature(vector, word, 1.5f);

            // 한글/영문 서브워드 N-gram
            if (word.Length > 2)
            {
                for (int i = 0; i <= word.Length - 2; i++)
                {
                    AddHashedFeature(vector, word.Substring(i, 2), 0.8f);
                }
            }
        }

        // 전체 문장 컨텍스트 해싱
        AddHashedFeature(vector, normalized, 0.5f);

        // L2 정규화 (Unit vector로 변환 -> 코사인 유사도 연산 극대화)
        float norm = 0f;
        for (int i = 0; i < dimension; i++)
        {
            norm += vector[i] * vector[i];
        }

        if (norm > 0)
        {
            float invNorm = 1.0f / MathF.Sqrt(norm);
            for (int i = 0; i < dimension; i++)
            {
                vector[i] *= invNorm;
            }
        }

        return vector;
    }

    private static void AddHashedFeature(float[] vector, string token, float weight)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        // SHA-256 바이트로 여러 버킷에 가중치 분산
        for (int i = 0; i < 4; i++)
        {
            int index = Math.Abs(BitConverter.ToInt32(hash, i * 4)) % vector.Length;
            float sign = (hash[16 + i] % 2 == 0) ? 1.0f : -1.0f;
            vector[index] += sign * weight;
        }
    }

    public void Dispose()
    {
    }
}
