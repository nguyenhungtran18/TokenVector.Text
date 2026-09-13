using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using TokenVector.Text.Models;

namespace TokenVector.Text.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class TokenizerBenchmarks
{
    private ITokenizer _vietnameseBpe = null!;
    private string _englishSample = null!;
    private string _vietnameseSample = null!;
    private byte[] _utf8Sample = null!;
    private string[] _batchSamples = null!;

    [GlobalSetup]
    public void Setup()
    {
        _vietnameseBpe = Tokenizer.CreateVietnameseBpe();

        _englishSample = "TokenVector is a cutting-edge, high-throughput AI compiler and computational framework written in C# 12 and .NET 8 Native AOT.";
        _vietnameseSample = "Hệ sinh thái TokenVector AI Engine và mô hình ngôn ngữ lớn LLM hiệu năng cao với khả năng xử lý song song trên toàn bộ nhân CPU.";
        _utf8Sample = Encoding.UTF8.GetBytes(_vietnameseSample);

        _batchSamples = new string[64];
        for (int i = 0; i < 64; i++)
        {
            _batchSamples[i] = _vietnameseSample;
        }
    }

    [Benchmark(Baseline = true)]
    public EncodingResult Encode_SingleSequence_Vietnamese()
    {
        return _vietnameseBpe.Encode(_vietnameseSample.AsSpan(), addSpecialTokens: false);
    }

    [Benchmark]
    public EncodingResult Encode_SingleSequence_English()
    {
        return _vietnameseBpe.Encode(_englishSample.AsSpan(), addSpecialTokens: false);
    }

    [Benchmark]
    public EncodingResult EncodeUtf8_DirectBytes()
    {
        return _vietnameseBpe.EncodeUtf8(_utf8Sample.AsSpan(), addSpecialTokens: false);
    }

    [Benchmark]
    public BatchEncodingResult EncodeBatch_64Sequences()
    {
        return _vietnameseBpe.EncodeBatch(_batchSamples, maxLength: 64, padding: PaddingStrategy.MaxFixed);
    }
}
