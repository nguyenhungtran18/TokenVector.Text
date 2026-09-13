# TokenVector.Text

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![C# 12](https://img.shields.io/badge/C%23-12-blue.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Native AOT](https://img.shields.io/badge/Native%20AOT-Ready-brightgreen.svg)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

[ 🇬🇧 English ](README.md) | [ 🇻🇳 Tiếng Việt ](README_VI.md) | [ 📋 Test Report (25/25 Passed) ](TEST_REPORT.md) | [ 📊 Benchmarks ](BENCHMARKS.md)

**TokenVector.Text** is the official high-performance, zero-allocation NLP, Tokenizer, and BPE Trainer engine for the **TokenVector Ecosystem**. Designed in C# 12 / .NET 8 Native AOT with SIMD acceleration (AVX2/AVX-512) and C# 12 `[GeneratedRegex]`, it delivers blazing text bandwidth (37.35 MB/s, 6.54 µs/sentence) and seamless zero-copy tensor interop with `TokenVector.Numerics.Core.NDArray<int>`.

---

## ⚡ Core Features & Capabilities

- **Zero-Allocation Hot Loops**: Encoding and slicing operations leverage `ReadOnlySpan<T>`, `ref struct`, and unmanaged buffers to eliminate Garbage Collector overhead.
- **Comprehensive Algorithm Support**:
  - **BPE (Byte-Pair Encoding)**: 100% parity with OpenAI `tiktoken` (`cl100k_base`, `o200k_base`), Meta LLaMA-3, and Qwen.
  - **WordPiece**: Subword greedy longest-match prefix segmentation compatible with BERT and DistilBERT.
  - **Unigram**: Viterbi dynamic programming optimal likelihood segmentation compatible with Google SentencePiece, T5, and Gemma.
- **BPE Subword Trainer (`BpeTrainer`)**: In-process training from raw text files to custom BPE vocabularies with export to Hugging Face `tokenizer.json`.
- **Multi-Format Loaders (`Loaders`)**: Hugging Face `tokenizer.json`, OpenAI `.tiktoken` (base64 ranks), `vocab.json` + `merges.txt`, and SentencePiece models.
- **Incremental Streaming Decoder (`IncrementalDecoder`)**: Character boundary safe streaming decoder that automatically buffers partial multi-byte UTF-8 bytes for real-time LLM token streaming.
- **Chat Templates & Cross-Encoder Pairs**:
  - ChatML (`<|im_start|>role...`) and Meta LLaMA-3 (`<|start_header_id|>...`).
  - Sequence-pair encoding (`[CLS] A [SEP] B [SEP]`) with `TokenTypeIds` (Segment IDs 0/1).
- **Sparse Embeddings for Hybrid RAG & Search**:
  - **BM25 Vectorizer**: Ultra-fast BM25 scoring and top-K document ranking.
  - **MinHash (LSH)**: Locality-Sensitive Hashing for Jaccard similarity estimation and large-scale deduplication.
- **Zero-Copy Tensor Bridge**: Directly exports token IDs, attention masks, and position IDs into contiguous unmanaged memory buffers of `TokenVector.Numerics.Core.NDArray<int>`.
- **Standalone CLI Tool (`tkv-text`)**: High-throughput CLI binary for batch file encoding, decoding, and tokenizer training.

---

## 🚀 Quickstart & Code Examples

### 1. Training a Custom BPE Tokenizer

```csharp
using TokenVector.Text.Training;

var corpus = new[] {
    "TokenVector high-performance computing framework in C# 12 and .NET 8.",
    "Zero-allocation tensor and NLP engine for AI architectures."
};

var trainer = new BpeTrainer(targetVocabSize: 32_000);
var tokenizer = trainer.Train(corpus);

// Export to Hugging Face tokenizer.json format
BpeTrainer.ExportToJson(tokenizer, "tokenizer.json");
```

### 2. Loading OpenAI .tiktoken Files

```csharp
using TokenVector.Text.Vocab.Loaders;

var (vocab, merges) = TiktokenBpeLoader.Load("cl100k_base.tiktoken");
var tokenizer = Tokenizer.CreateBpe(vocab, merges);
```

### 3. Direct Zero-Copy Tensor Export (`TokenVector.Numerics`)

```csharp
using TokenVector.Text;
using TokenVector.Numerics.Core;

var tokenizer = Tokenizer.CreateVietnameseBpe();
string[] batch = new[] { "TokenVector high-performance computing" };

using NDArray<int> tensor = tokenizer.EncodeToTensor(batch);
Console.WriteLine($"Tensor shape: [{tensor.Shape[0]}, {tensor.Shape[1]}]");
```

---

## 📊 Benchmark Results

Measured on x64 .NET 8 (Single CPU Core / 100,000 iterations):

| Metric | TokenVector.Text | Microsoft.ML.Tokenizers | OpenAI tiktoken (Python/Rust) |
| :--- | :--- | :--- | :--- |
| **Sentence Latency** | **`6.54 µs`** *(Fastest)* | `6.64 µs` | `33.09 µs` (5.06x slower) |
| **Text Bandwidth** | **`37.35 MB/s`** | `36.77 MB/s` | `7.38 MB/s` |
| **Heap Memory Alloc** | **`368 B / op`** *(50.5% Less)* | `744 B / op` | High (Python Heap) |
| **Zero-Copy Tensor Export** | **✅ YES (`NDArray<int>`)** | ❌ No | ❌ No |

---

## 🧪 Running Tests

```bash
dotnet test TokenVector.Text.sln
```
