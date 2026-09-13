using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TokenVector.Text.Models;
using TokenVector.Text.Tokenizers.Bpe;
using TokenVector.Text.Tokenizers.Unigram;
using TokenVector.Text.Tokenizers.WordPiece;
using TokenVector.Text.Vocab;
using TokenVector.Text.Vocab.Loaders;

namespace TokenVector.Text;

/// <summary>
/// Main factory and entry point for creating and loading tokenizers in TokenVector.Text.
/// </summary>
public static class Tokenizer
{
    /// <summary>
    /// Loads a tokenizer from a Hugging Face tokenizer.json file or JSON string.
    /// </summary>
    public static ITokenizer FromPretrained(string modelPathOrJson)
    {
        ArgumentNullException.ThrowIfNull(modelPathOrJson);

        HuggingFaceJsonLoader.LoadedTokenizerConfig config;
        if (File.Exists(modelPathOrJson))
        {
            config = HuggingFaceJsonLoader.LoadFromFile(modelPathOrJson);
        }
        else
        {
            config = HuggingFaceJsonLoader.LoadFromJson(modelPathOrJson);
        }

        var specialTokenSet = new SpecialTokenSet(config.AddedTokens);
        var vocabTable = new VocabTable(config.Vocab);

        if (config.ModelType.Equals("BPE", StringComparison.OrdinalIgnoreCase))
        {
            var mergesTable = new MergesTable(config.Merges, vocabTable);
            var options = new BpeOptions
            {
                UnkToken = config.UnkToken,
                BosToken = config.BosToken,
                EosToken = config.EosToken,
                PadToken = config.PadToken,
                Pattern = config.Pattern,
                ByteLevelFallback = config.ByteLevel
            };
            return new BpeTokenizer(vocabTable, mergesTable, options, specialTokenSet);
        }
        else if (config.ModelType.Equals("WordPiece", StringComparison.OrdinalIgnoreCase))
        {
            var options = new WordPieceOptions
            {
                UnkToken = config.UnkToken ?? "[UNK]",
                PadToken = config.PadToken ?? "[PAD]"
            };
            return new WordPieceTokenizer(vocabTable, options, specialTokenSet);
        }
        else if (config.ModelType.Equals("Unigram", StringComparison.OrdinalIgnoreCase))
        {
            var unigramModel = new UnigramModel(config.UnigramPieces);
            var options = new UnigramOptions
            {
                UnkToken = config.UnkToken ?? "<unk>",
                BosToken = config.BosToken,
                EosToken = config.EosToken,
                PadToken = config.PadToken
            };
            return new UnigramTokenizer(unigramModel, options, specialTokenSet);
        }

        throw new InvalidOperationException($"Unsupported tokenizer model type: {config.ModelType}");
    }

    /// <summary>
    /// Creates a custom BPE tokenizer with given vocab and merges.
    /// </summary>
    public static ITokenizer CreateBpe(
        VocabTable vocab,
        MergesTable merges,
        BpeOptions? options = null,
        SpecialTokenSet? specialTokens = null)
    {
        return new BpeTokenizer(vocab, merges, options, specialTokens);
    }

    /// <summary>
    /// Creates a custom WordPiece tokenizer.
    /// </summary>
    public static ITokenizer CreateWordPiece(
        VocabTable vocab,
        WordPieceOptions? options = null,
        SpecialTokenSet? specialTokens = null)
    {
        return new WordPieceTokenizer(vocab, options, specialTokens);
    }

    /// <summary>
    /// Creates a custom Unigram tokenizer.
    /// </summary>
    public static ITokenizer CreateUnigram(
        UnigramModel model,
        UnigramOptions? options = null,
        SpecialTokenSet? specialTokens = null)
    {
        return new UnigramTokenizer(model, options, specialTokens);
    }

    /// <summary>
    /// Creates a GPT-4 / GPT-3.5 (cl100k_base) compatible BPE tokenizer preset.
    /// </summary>
    public static ITokenizer CreateGpt4() => Presets.ModelPresets.CreateGpt4();

    /// <summary>
    /// Creates a Meta LLaMA-3 (128k) compatible BPE tokenizer preset.
    /// </summary>
    public static ITokenizer CreateLlama3() => Presets.ModelPresets.CreateLlama3();

    /// <summary>
    /// Creates a BERT (bert-base-uncased) compatible WordPiece tokenizer preset.
    /// </summary>
    public static ITokenizer CreateBert() => Presets.ModelPresets.CreateBert();

    /// <summary>
    /// Creates a lightweight Vietnamese/Multilingual Byte-Level BPE tokenizer preset.
    /// </summary>
    public static ITokenizer CreateVietnameseBpe()
    {
        var vocabDict = new Dictionary<string, int>(StringComparer.Ordinal);
        int id = 0;

        // Byte fallback tokens (0..255)
        for (int b = 0; b < 256; b++)
        {
            vocabDict[Unicode.ByteLevelBpeHelper.ByteToString((byte)b)] = id++;
        }

        string[] commonWords = new[]
        {
            // Conjunctions, prepositions, pronouns & particles
            "và", "của", "là", "có", "trong", "được", "người", "không", "một", "cho",
            "với", "đã", "các", "này", "khi", "tại", "nhiều", "về", "như", "đến",
            "để", "ra", "vào", "từ", "lại", "thì", "sẽ", "những", "năm", "ngày",
            "đang", "hay", "nên", "nếu", "bởi", "vì", "qua", "sau", "trước", "theo",
            "giữa", "dưới", "trên", "toàn", "bộ", "nhân", "khắp", "cùng", "nhau", "riêng",
            "chung", "khác", "mọi", "từng", "mỗi", "hết", "cả", "ai", "gì", "đâu",
            "nào", "sao", "thế", "đó", "kia", "đây", "rất", "quá", "lắm", "hơn", "nhất",
            "tốt", "xấu", "mới", "cũ", "lớn", "nhỏ", "ít", "đầy", "trống", "nhanh", "chậm",
            "dễ", "khó", "đúng", "sai",

            // Tech, AI, and Science terms
            "Hệ", "thống", "sinh", "thái", "mô", "hình", "ngôn", "ngữ", "hiệu", "năng", "cao",
            "khả", "xử", "lý", "song", "trí", "tuệ", "tạo", "máy", "học", "dữ", "liệu",
            "thuật", "toán", "chương", "trình", "nhớ", "tốc", "độ", "tối", "ưu", "hóa",
            "gian", "thời", "phần", "mềm", "cứng", "mạng", "nơ-ron", "tự", "động", "tập", "hợp",
            "chỉ", "mục", "truy", "vấn", "kết", "quả", "đầu", "cấu", "trúc", "chuyển", "đổi",
            "nhúng", "véc-tơ", "tương", "đồng", "phân", "loại", "dự", "đoán", "huấn", "luyện",
            "suy", "luận", "trọng", "số", "lớp", "hàm", "biến", "giá", "trị", "mảng", "chuỗi",
            "ký", "tự", "mã", "nguồn", "thư", "viện", "nền", "tảng", "giao", "diện", "máy", "chủ",
            "khách", "hàng", "dịch", "vụ", "kết", "nối", "an", "bảo", "mật", "hiện", "đại",
            "tiến", "công", "nghệ", "khoa", "thông", "tin", "tri", "thức", "tài", "nguyên",
            "giải", "pháp", "yêu", "cầu", "hỗ", "trợ", "nghiên", "cứu", "đánh", "giá", "đo", "lường",

            // AI & Computer Science English terms
            "Token", "Vector", "Text", "Numerics", "Core", "Engine", "Model", "LLM", "AI", "GPT",
            "BERT", "System", "CPU", "GPU", "NPU", "RAM", "SIMD", "AVX", "AVX2", "AVX512",
            "ARM", "NEON", "Span", "Memory", "Native", "AOT", "C#", ".NET", "Rust", "Python",
            "C++", "Java", "Linux", "Windows", "MacOS", "is", "a", "an", "the", "and", "or",
            "not", "in", "on", "at", "to", "for", "with", "from", "by", "cutting", "edge",
            "high", "throughput", "compiler", "computational", "framework", "written", "zero",
            "allocation", "fast", "speed", "latency", "benchmark", "accuracy", "training",
            "inference", "pipeline", "dataset", "transformer", "attention", "layer", "encoder",
            "decoder", "tokenizer", "embedding", "loss", "optimizer", "gradient"
        };

        var mergesList = new List<(string First, string Second)>();

        foreach (var word in commonWords)
        {
            foreach (var variant in new[] { word, " " + word })
            {
                byte[] bytes = Encoding.UTF8.GetBytes(variant);
                char[] chars = new char[bytes.Length];
                Unicode.ByteLevelBpeHelper.BytesToChars(bytes, chars);

                string prefix = chars[0].ToString();
                for (int c = 1; c < chars.Length; c++)
                {
                    string nextChar = chars[c].ToString();
                    string merged = prefix + nextChar;

                    if (!vocabDict.ContainsKey(merged))
                    {
                        vocabDict[merged] = id++;
                    }

                    mergesList.Add((prefix, nextChar));
                    prefix = merged;
                }
            }
        }

        var specialTokens = new SpecialTokenSet();
        specialTokens.Add("<|endoftext|>", id);
        vocabDict["<|endoftext|>"] = id++;
        specialTokens.Add("<|im_start|>", id);
        vocabDict["<|im_start|>"] = id++;
        specialTokens.Add("<|im_end|>", id);
        vocabDict["<|im_end|>"] = id++;

        var vocabTable = new VocabTable(vocabDict);
        var mergesTable = new MergesTable(mergesList, vocabTable);
        var options = BpeOptions.Gpt4;

        return new BpeTokenizer(vocabTable, mergesTable, options, specialTokens);
    }
}
