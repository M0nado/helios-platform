using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HELIOS.Platform.BackendServices.AI.TokenOptimization
{
    /// <summary>
    /// Advanced prompt compression using semantic and syntactic techniques
    /// </summary>
    public interface IPromptCompressor
    {
        Task<CompressionResult> CompressAsync(string prompt, double targetCompressionRatio);
        Task<SemanticSimilarityResult> FindSimilarChunksAsync(string[] chunks);
        Task<string[]> ExtractEntitiesAsync(string text);
        string RemoveStopwords(string text);
        string ApplySentencePieceCompression(string text);
    }

    public class PromptCompressor : IPromptCompressor
    {
        private readonly HashSet<string> _stopwords;
        private readonly int _minChunkSize;

        public PromptCompressor(int minChunkSize = 3)
        {
            _minChunkSize = minChunkSize;
            _stopwords = LoadStopwords();
        }

        public async Task<CompressionResult> CompressAsync(string prompt, double targetCompressionRatio)
        {
            var originalLength = prompt.Length;
            var targetLength = (int)(originalLength * targetCompressionRatio);

            var compressed = prompt;
            var techniques = new List<string>();

            // Step 1: Remove stopwords
            var afterStopwordRemoval = RemoveStopwords(compressed);
            techniques.Add("stopword_removal");
            compressed = afterStopwordRemoval;

            // Step 2: Apply sentence-piece compression
            if (compressed.Length > targetLength)
            {
                compressed = ApplySentencePieceCompression(compressed);
                techniques.Add("sentence_piece");
            }

            // Step 3: Entity extraction and replacement
            if (compressed.Length > targetLength)
            {
                var entities = await ExtractEntitiesAsync(compressed);
                compressed = ReplaceEntitiesWithReferences(compressed, entities);
                techniques.Add("entity_replacement");
            }

            // Step 4: Semantic similarity grouping
            if (compressed.Length > targetLength)
            {
                var sentences = Regex.Split(compressed, @"(?<=[.!?])\s+");
                if (sentences.Length > _minChunkSize)
                {
                    var similarityResult = await FindSimilarChunksAsync(sentences);
                    compressed = MergeReduceSimilarChunks(similarityResult);
                    techniques.Add("semantic_grouping");
                }
            }

            var compressionRatio = (double)compressed.Length / originalLength;
            var tokensEstimate = originalLength / 4; // Rough estimate

            return await Task.FromResult(new CompressionResult
            {
                Original = prompt,
                Compressed = compressed,
                OriginalLength = originalLength,
                CompressedLength = compressed.Length,
                TargetCompressionRatio = targetCompressionRatio,
                AchievedCompressionRatio = compressionRatio,
                TechniquesApplied = techniques,
                CompressionPercentage = (1 - compressionRatio) * 100,
                EstimatedTokensSaved = (int)((1 - compressionRatio) * tokensEstimate),
                CompressedAt = DateTime.UtcNow
            });
        }

        public async Task<SemanticSimilarityResult> FindSimilarChunksAsync(string[] chunks)
        {
            var result = new SemanticSimilarityResult { Chunks = chunks };
            var similarityMatrix = new double[chunks.Length][];

            for (int i = 0; i < chunks.Length; i++)
            {
                similarityMatrix[i] = new double[chunks.Length];
                for (int j = i; j < chunks.Length; j++)
                {
                    var similarity = CalculateSemanticSimilarity(chunks[i], chunks[j]);
                    similarityMatrix[i][j] = similarity;
                    similarityMatrix[j][i] = similarity;
                }
            }

            result.SimilarityMatrix = similarityMatrix;
            result.SimilarPairs = FindSimilarPairs(similarityMatrix, 0.7);

            return await Task.FromResult(result);
        }

        public async Task<string[]> ExtractEntitiesAsync(string text)
        {
            var entities = new List<string>();

            // Proper nouns (capitalized words)
            var words = text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                if (char.IsUpper(word[0]) && word.Length > 2)
                {
                    entities.Add(word);
                }
            }

            // Numbers
            var numberMatches = Regex.Matches(text, @"\b\d+(?:\.\d+)?\b");
            foreach (Match match in numberMatches)
            {
                entities.Add(match.Value);
            }

            return await Task.FromResult(entities.Distinct().ToArray());
        }

        public string RemoveStopwords(string text)
        {
            var words = text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var filtered = words.Where(w => !_stopwords.Contains(w.ToLower())).ToList();
            return string.Join(" ", filtered);
        }

        public string ApplySentencePieceCompression(string text)
        {
            // Merge short sentences
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            var compressed = new List<string>();

            foreach (var sentence in sentences)
            {
                if (compressed.Count > 0 && sentence.Length < 30 && compressed.Last().Length < 100)
                {
                    compressed[compressed.Count - 1] += " " + sentence;
                }
                else
                {
                    compressed.Add(sentence);
                }
            }

            return string.Join(" ", compressed);
        }

        private double CalculateSemanticSimilarity(string chunk1, string chunk2)
        {
            if (string.Equals(chunk1, chunk2, StringComparison.OrdinalIgnoreCase))
                return 1.0;

            var words1 = chunk1.ToLower().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var words2 = chunk2.ToLower().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union > 0 ? (double)intersection / union : 0;
        }

        private List<(int, int, double)> FindSimilarPairs(double[][] matrix, double threshold)
        {
            var pairs = new List<(int, int, double)>();

            for (int i = 0; i < matrix.Length; i++)
            {
                for (int j = i + 1; j < matrix[i].Length; j++)
                {
                    if (matrix[i][j] >= threshold)
                    {
                        pairs.Add((i, j, matrix[i][j]));
                    }
                }
            }

            return pairs.OrderByDescending(p => p.Item3).ToList();
        }

        private string ReplaceEntitiesWithReferences(string text, string[] entities)
        {
            var result = text;
            var entityMap = new Dictionary<string, string>();

            foreach (var entity in entities.OrderByDescending(e => e.Length))
            {
                var reference = $"[{entityMap.Count + 1}]";
                if (!entityMap.ContainsKey(entity))
                {
                    entityMap.Add(entity, reference);
                    result = Regex.Replace(result, Regex.Escape(entity), reference, RegexOptions.IgnoreCase);
                }
            }

            return result;
        }

        private string MergeReduceSimilarChunks(SemanticSimilarityResult result)
        {
            var chunks = result.Chunks.ToList();
            var merged = new List<string>();

            for (int i = 0; i < chunks.Count; i++)
            {
                if (merged.Count == 0)
                {
                    merged.Add(chunks[i]);
                }
                else
                {
                    var lastSimilarity = CalculateSemanticSimilarity(merged.Last(), chunks[i]);
                    if (lastSimilarity > 0.6)
                    {
                        merged[merged.Count - 1] += " " + chunks[i];
                    }
                    else
                    {
                        merged.Add(chunks[i]);
                    }
                }
            }

            return string.Join(" ", merged);
        }

        private HashSet<string> LoadStopwords()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
                "of", "with", "by", "from", "as", "is", "was", "are", "were", "been",
                "be", "have", "has", "had", "do", "does", "did", "will", "would",
                "could", "should", "may", "might", "must", "can", "if", "while", "because",
                "that", "this", "these", "those", "i", "you", "he", "she", "it", "we", "they"
            };
        }
    }

    public class CompressionResult
    {
        public string Original { get; set; }
        public string Compressed { get; set; }
        public int OriginalLength { get; set; }
        public int CompressedLength { get; set; }
        public double TargetCompressionRatio { get; set; }
        public double AchievedCompressionRatio { get; set; }
        public List<string> TechniquesApplied { get; set; }
        public double CompressionPercentage { get; set; }
        public int EstimatedTokensSaved { get; set; }
        public DateTime CompressedAt { get; set; }
    }

    public class SemanticSimilarityResult
    {
        public string[] Chunks { get; set; }
        public double[][] SimilarityMatrix { get; set; }
        public List<(int, int, double)> SimilarPairs { get; set; }
    }
}
