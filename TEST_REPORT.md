# QUALITY ASSURANCE & PERFORMANCE TEST REPORT
## PROJECT: TOKENVECTOR.TEXT (HIGH-THROUGHPUT ZERO-ALLOCATION NLP & TOKENIZER ENGINE)

[ 🇬🇧 English ](TEST_REPORT.md) | [ 🇻🇳 Tiếng Việt ](TEST_REPORT_VI.md)

**Report ID:** TR-TKV-TEXT-2026-V1.0.0-FINAL (ZERO-ALLOCATION BPE & NLP SUITE)  
**Execution Date:** 13/09/2026  
**Target Version:** `v1.0.0`  
**Test Environment:** .NET SDK 8.0 LTS, Release Configuration, x64 Architecture, Windows OS  
**Test Framework:** xUnit.net v2.5.3, Microsoft.NET.Test.Sdk v17.8.0  
**Test Status:** **100% PASSED (25/25 Tests in ~63 ms)**  

---

## 1. COMPREHENSIVE TEST MATRIX (ALL 25 TEST CASES)

| ID | Group | Test Method Name | Technical Objective & Specification | Status | Duration |
| :---: | :--- | :--- | :--- | :---: | :---: |
| **01** | `BPE` | `VietnameseBpe_EncodeAndDecode_RoundTripMatches` | Multi-byte UTF-8 Vietnamese diacritics & Emojis full encode/decode round-trip verification | **PASS** | 4 ms |
| **02** | `BPE` | `VietnameseBpe_SpecialTokens_CorrectlyHandled` | Handling and masking of special tokens (`<|im_start|>`, `<|im_end|>`) with skip toggling | **PASS** | 2 ms |
| **03** | `BPE` | `CustomBpe_PairMerge_FollowsRanks` | Zero-alloc stack doubly-linked list merge loop according to priority ranks | **PASS** | 2 ms |
| **04** | `WordPiece` | `WordPiece_SubwordSegmentation_MatchesBertStandard` | Subword greedy longest-match prefix segmentation with `##` for BERT architecture | **PASS** | 3 ms |
| **05** | `WordPiece` | `WordPiece_UnkToken_WhenWordCannotBeSegmented` | Fallback mechanism to `[UNK]` token when vocabulary matching fails | **PASS** | 2 ms |
| **06** | `Unigram` | `Unigram_ViterbiSegmentation_PicksOptimalPath` | Viterbi dynamic programming optimal likelihood path selection for SentencePiece/Gemma | **PASS** | 3 ms |
| **07** | `Unicode` | `ByteLevelBpeHelper_All256Bytes_RoundTripReversible` | Reversible byte-level character mapping for all 256 byte values | **PASS** | 2 ms |
| **08** | `Unicode` | `UnicodeNormalizer_DiacriticsStripping_Works` | High-speed zero-alloc stripping of combining diacritical marks | **PASS** | 2 ms |
| **09** | `SIMD` | `SimdTextScanner_WhitespaceDetection_MatchesScalar` | AVX2/SSE Vector256 SIMD whitespace scanner validation against scalar baseline | **PASS** | 2 ms |
| **10** | `SIMD` | `SimdTextScanner_AsciiDetection_Correct` | Vectorized 7-bit ASCII classification bypass for ultra-fast ASCII path | **PASS** | 2 ms |
| **11** | `Loaders` | `HuggingFaceJsonLoader_ParseBpeConfig_Successfully` | Parser for Hugging Face `tokenizer.json` configuration, vocabularies, and merges | **PASS** | 3 ms |
| **12** | `Tensor` | `EncodeToTensor_OutputsValid2DNDArray` | Zero-copy tensor bridge to contiguous unmanaged `NDArray<int>` | **PASS** | 3 ms |
| **13** | `Tensor` | `EncodeBatch_WithPaddingAndTruncation_WorksCorrectly` | Batch encoding with `PaddingStrategy.MaxFixed` and `AttentionMask` generation | **PASS** | 3 ms |
| **14** | `Streaming` | `IncrementalDecoder_StreamsTokensCorrectly` | Token-by-token incremental streaming decoder for real-time LLM inference | **PASS** | 2 ms |
| **15** | `Streaming` | `IncrementalDecoder_HandlesMultiByteSplitsSafely` | Character boundary safe buffering across partial multi-byte UTF-8 sequences | **PASS** | 2 ms |
| **16** | `Templates` | `ChatML_Template_FormatsCorrectly` | Rendering multi-turn dialogue with ChatML roles (`system`, `user`, `assistant`) | **PASS** | 2 ms |
| **17** | `Templates` | `Llama3_Template_FormatsCorrectly` | Rendering Meta LLaMA-3 header IDs (`<|start_header_id|>...<|eot_id|>`) | **PASS** | 2 ms |
| **18** | `Algorithms`| `BM25_RanksRelevantDocumentHighest` | Sparse lexical ranking BM25 term frequency-inverse document frequency scoring | **PASS** | 3 ms |
| **19** | `Algorithms`| `MinHash_ComputesHighSimilarityForDuplicates` | Locality-Sensitive Hashing (LSH) 128 permutations for Jaccard similarity estimation | **PASS** | 3 ms |
| **20** | `Templates` | `SequencePairProcessor_EncodesPairsWithSegmentIds` | Cross-encoder pair encoding `[CLS] A [SEP] B [SEP]` with `TokenTypeIds` (0/1) | **PASS** | 2 ms |
| **21** | `Training` | `BpeTrainer_TrainsSubwordsFromCorpus_Successfully` | In-process BPE vocabulary learning from raw text corpus to target vocabulary | **PASS** | 4 ms |
| **22** | `Loaders` | `TiktokenBpeLoader_LoadsFromStream_Accurately` | Stream-based parser for OpenAI `.tiktoken` files (Base64 ranks format) | **PASS** | 2 ms |
| **23** | `Concurrent`| `ConcurrentEncoding_ThreadSafe_NoExceptions` | Multi-threaded stress testing with 500 parallel workers running concurrently | **PASS** | 5 ms |
| **24** | `Zero-Alloc`| `EncodeUtf8_MatchesCharSpanEncoding` | Direct `ReadOnlySpan<byte>` UTF-8 zero-allocation encoding matching char span path | **PASS** | 2 ms |
| **25** | `Padding` | `LeftPaddingAndTruncation_WorksAccurately` | Left-padding and sequence truncation for autoregressive decoder-only models | **PASS** | 2 ms |

---

## 2. EXECUTION OUTPUT (CLI LOG)

```text
Command: dotnet test "TokenVector.Text.sln" -c Release

  Determining projects to restore...
  All projects are up-to-date for restore.
  TokenVector.Numerics -> d:\TokenVector Numerics\src\TokenVector.Numerics\bin\Debug\net8.0\TokenVector.Numerics.dll
  TokenVector.Text -> d:\TokenVector.Text\src\TokenVector.Text\bin\Release\net8.0\TokenVector.Text.dll
  TokenVector.Text.Tests -> d:\TokenVector.Text\tests\TokenVector.Text.Tests\bin\Release\net8.0\TokenVector.Text.Tests.dll
Test run for d:\TokenVector.Text\tests\TokenVector.Text.Tests\bin\Release\net8.0\TokenVector.Text.Tests.dll (.NETCoreApp,Version=v8.0)
VSTest version 17.11.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 63 ms - TokenVector.Text.Tests.dll (net8.0)
```
