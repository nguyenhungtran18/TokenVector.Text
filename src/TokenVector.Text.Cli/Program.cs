using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using TokenVector.Text;
using TokenVector.Text.Training;

namespace TokenVector.Text.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            PrintUsage();
            return 0;
        }

        string command = args[0].ToLowerInvariant();
        try
        {
            switch (command)
            {
                case "encode":
                    return HandleEncode(args);
                case "decode":
                    return HandleDecode(args);
                case "train":
                    return HandleTrain(args);
                case "info":
                    return HandleInfo(args);
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Unknown command: {command}");
                    Console.ResetColor();
                    PrintUsage();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("==================================================================");
        Console.WriteLine(" TokenVector.Text CLI (tkv-text) - Zero-Allocation NLP Engine     ");
        Console.WriteLine("==================================================================");
        Console.WriteLine("Usage:");
        Console.WriteLine("  tkv-text encode <input_file> [-m <model_path_or_preset>] [-o <output_file>]");
        Console.WriteLine("  tkv-text decode <token_ids_file> [-m <model_path_or_preset>] [-o <output_file>]");
        Console.WriteLine("  tkv-text train <corpus_txt_file> [-v <vocab_size>] [-o <output_tokenizer.json>]");
        Console.WriteLine("  tkv-text info [-m <model_path_or_preset>]");
        Console.WriteLine();
        Console.WriteLine("Models / Presets:");
        Console.WriteLine("  vietnamese (default) | gpt4 | llama3 | bert | <path/to/tokenizer.json>");
        Console.WriteLine("==================================================================");
    }

    private static ITokenizer ResolveTokenizer(string modelName)
    {
        return modelName.ToLowerInvariant() switch
        {
            "vietnamese" or "vi" => Tokenizer.CreateVietnameseBpe(),
            "gpt4" or "cl100k" => Tokenizer.CreateGpt4(),
            "llama3" or "llama" => Tokenizer.CreateLlama3(),
            "bert" => Tokenizer.CreateBert(),
            _ => Tokenizer.FromPretrained(modelName)
        };
    }

    private static int HandleEncode(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: tkv-text encode <input_file> [-m <model>] [-o <output_file>]");
            return 1;
        }

        string inputFile = args[1];
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Input file not found: {inputFile}");
            return 1;
        }

        string model = "vietnamese";
        string? outputFile = null;

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "-m" && i + 1 < args.Length) model = args[++i];
            else if (args[i] == "-o" && i + 1 < args.Length) outputFile = args[++i];
        }

        var tokenizer = ResolveTokenizer(model);
        string text = File.ReadAllText(inputFile);

        var sw = Stopwatch.StartNew();
        var result = tokenizer.Encode(text.AsSpan());
        sw.Stop();

        Console.WriteLine($"Encoded {text.Length:N0} characters into {result.Length:N0} tokens in {sw.ElapsedMilliseconds} ms ({result.Length / Math.Max(0.001, sw.Elapsed.TotalSeconds):N0} tokens/sec)");

        if (!string.IsNullOrEmpty(outputFile))
        {
            using var fs = File.Create(outputFile);
            using var bw = new BinaryWriter(fs);
            for (int i = 0; i < result.Length; i++)
            {
                bw.Write(result.Ids[i]);
            }
            Console.WriteLine($"Saved binary tokens to: {outputFile}");
        }
        else
        {
            int previewCount = Math.Min(20, result.Length);
            Console.WriteLine($"Preview IDs: [{string.Join(", ", result.Ids.AsSpan(0, previewCount).ToArray())}{(result.Length > 20 ? ", ..." : "")}]");
        }

        return 0;
    }

    private static int HandleDecode(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: tkv-text decode <token_ids_file> [-m <model>] [-o <output_file>]");
            return 1;
        }

        string inputFile = args[1];
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Input file not found: {inputFile}");
            return 1;
        }

        string model = "vietnamese";
        string? outputFile = null;

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "-m" && i + 1 < args.Length) model = args[++i];
            else if (args[i] == "-o" && i + 1 < args.Length) outputFile = args[++i];
        }

        var tokenizer = ResolveTokenizer(model);

        byte[] bytes = File.ReadAllBytes(inputFile);
        int[] ids = new int[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, ids, 0, bytes.Length);

        string text = tokenizer.Decode(ids);

        if (!string.IsNullOrEmpty(outputFile))
        {
            File.WriteAllText(outputFile, text);
            Console.WriteLine($"Decoded {ids.Length:N0} tokens to: {outputFile}");
        }
        else
        {
            Console.WriteLine("Decoded Output:");
            Console.WriteLine(text);
        }

        return 0;
    }

    private static int HandleTrain(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: tkv-text train <corpus_txt_file> [-v <vocab_size>] [-o <output_tokenizer.json>]");
            return 1;
        }

        string inputFile = args[1];
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Corpus file not found: {inputFile}");
            return 1;
        }

        int vocabSize = 32_000;
        string outputFile = "tokenizer.json";

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "-v" && i + 1 < args.Length) int.TryParse(args[++i], out vocabSize);
            else if (args[i] == "-o" && i + 1 < args.Length) outputFile = args[++i];
        }

        Console.WriteLine($"Training BPE Tokenizer from '{inputFile}' to target vocab size: {vocabSize:N0}...");
        var lines = File.ReadLines(inputFile);

        var sw = Stopwatch.StartNew();
        var trainer = new BpeTrainer(targetVocabSize: vocabSize);
        var trained = trainer.Train(lines);
        sw.Stop();

        Console.WriteLine($"Trained {trained.Vocab.Count:N0} vocabulary subwords in {sw.Elapsed.TotalSeconds:F2} s!");

        BpeTrainer.ExportToJson(trained, outputFile);
        Console.WriteLine($"Saved Hugging Face tokenizer to: {outputFile}");

        return 0;
    }

    private static int HandleInfo(string[] args)
    {
        string model = "vietnamese";
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "-m" && i + 1 < args.Length) model = args[++i];
        }

        var tokenizer = ResolveTokenizer(model);
        Console.WriteLine($"Tokenizer Type: {tokenizer.GetType().Name}");
        Console.WriteLine($"Vocabulary Size: {tokenizer.Vocab.Count:N0} tokens");
        Console.WriteLine($"Special Tokens: {tokenizer.SpecialTokens.Count:N0}");
        foreach (var (k, v) in tokenizer.SpecialTokens.AsDictionary())
        {
            Console.WriteLine($"  - {k} => ID {v}");
        }

        return 0;
    }
}
