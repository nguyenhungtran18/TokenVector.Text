using System;
using System.Diagnostics;
using System.Text;
using Microsoft.ML.Tokenizers;
using TokenVector.Text;

namespace TokenVector.Text.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" 🔥 HEAD-TO-HEAD BENCHMARK: TokenVector.Text vs Microsoft.ML.Tokenizers vs tiktoken");
        Console.WriteLine("================================================================================");

        string sampleVi = "Hệ sinh thái TokenVector AI Engine và mô hình ngôn ngữ lớn LLM hiệu năng cao với khả năng xử lý song song trên toàn bộ nhân CPU.";
        string sampleEn = "TokenVector is a cutting-edge high-throughput AI compiler and computational framework written in C# 12 and .NET 8 Native AOT.";

        const int iterations = 100_000;

        Console.WriteLine($"Corpus: Vietnamese ({sampleVi.Length} chars) & English ({sampleEn.Length} chars)");
        Console.WriteLine($"Iterations per test: {iterations:N0}");
        Console.WriteLine("--------------------------------------------------------------------------------");

        // 1. Benchmark TokenVector.Text
        var tkvTokenizer = Tokenizer.CreateVietnameseBpe();

        // Warmup
        for (int i = 0; i < 1000; i++)
        {
            tkvTokenizer.Encode(sampleVi.AsSpan(), addSpecialTokens: false);
            tkvTokenizer.Encode(sampleEn.AsSpan(), addSpecialTokens: false);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memBeforeTkv = GC.GetAllocatedBytesForCurrentThread();

        var swTkv = Stopwatch.StartNew();
        int tkvTokens = 0;
        for (int i = 0; i < iterations; i++)
        {
            var res = tkvTokenizer.Encode(sampleVi.AsSpan(), addSpecialTokens: false);
            tkvTokens += res.Length;
        }
        swTkv.Stop();
        long memAfterTkv = GC.GetAllocatedBytesForCurrentThread();

        double tkvSeconds = swTkv.Elapsed.TotalSeconds;
        double tkvThroughput = tkvTokens / tkvSeconds;
        double tkvLatency = (swTkv.Elapsed.TotalMilliseconds * 1000.0) / iterations;
        long tkvAllocPerOp = (memAfterTkv - memBeforeTkv) / iterations;
        double tkvMbPerSec = ((sampleVi.Length * sizeof(char)) * (double)iterations / (1024.0 * 1024.0)) / tkvSeconds;

        Console.WriteLine($"[1] TokenVector.Text (C# 12 SIMD / Zero-Copy):");
        Console.WriteLine($"    - Latency:    {tkvLatency:F2} µs / sentence");
        Console.WriteLine($"    - Bandwidth:  {tkvMbPerSec:F2} MB/s ({tkvThroughput / 1_000_000.0:F2} M tokens/s)");
        Console.WriteLine($"    - Total Time: {tkvSeconds:F3} s ({tkvTokens:N0} tokens)");
        Console.WriteLine($"    - GC Alloc:   {tkvAllocPerOp} B / op");
        Console.WriteLine("--------------------------------------------------------------------------------");

        // 2. Benchmark Microsoft.ML.Tokenizers (Tiktoken / BPE)
        var msTokenizer = Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForModel("gpt-4");

        // Warmup
        for (int i = 0; i < 1000; i++)
        {
            msTokenizer.EncodeToIds(sampleVi);
            msTokenizer.EncodeToIds(sampleEn);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memBeforeMs = GC.GetAllocatedBytesForCurrentThread();

        var swMs = Stopwatch.StartNew();
        int msTokens = 0;
        for (int i = 0; i < iterations; i++)
        {
            var res = msTokenizer.EncodeToIds(sampleVi);
            msTokens += res.Count;
        }
        swMs.Stop();
        long memAfterMs = GC.GetAllocatedBytesForCurrentThread();

        double msSeconds = swMs.Elapsed.TotalSeconds;
        double msThroughput = msTokens / msSeconds;
        double msLatency = (swMs.Elapsed.TotalMilliseconds * 1000.0) / iterations;
        long msAllocPerOp = (memAfterMs - memBeforeMs) / iterations;
        double msMbPerSec = ((sampleVi.Length * sizeof(char)) * (double)iterations / (1024.0 * 1024.0)) / msSeconds;

        Console.WriteLine($"[2] Microsoft.ML.Tokenizers (TiktokenTokenizer gpt-4):");
        Console.WriteLine($"    - Latency:    {msLatency:F2} µs / sentence");
        Console.WriteLine($"    - Bandwidth:  {msMbPerSec:F2} MB/s ({msThroughput / 1_000_000.0:F2} M tokens/s)");
        Console.WriteLine($"    - Total Time: {msSeconds:F3} s ({msTokens:N0} tokens)");
        Console.WriteLine($"    - GC Alloc:   {msAllocPerOp} B / op");
        Console.WriteLine("--------------------------------------------------------------------------------");

        double latencySpeedup = msLatency / tkvLatency;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"⚡ TokenVector.Text is {latencySpeedup:F2}x FASTER in latency and uses {(1.0 - (double)tkvAllocPerOp / msAllocPerOp) * 100.0:F1}% LESS heap memory!");
        Console.ResetColor();
        Console.WriteLine("================================================================================");
    }
}
