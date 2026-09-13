# TokenVector.Text (Tiếng Việt)

[ 🇬🇧 English ](README.md) | [ 🇻🇳 Tiếng Việt ](README_VI.md) | [ 📋 Báo cáo Kiểm thử (25/25 Đạt) ](TEST_REPORT_VI.md) | [ 📊 Benchmarks ](BENCHMARKS_VI.md)

**TokenVector.Text** là thư viện NLP, Tokenizer engine và BPE Subword Trainer hiệu năng cao, zero-allocation chính thức dành cho **Hệ sinh thái TokenVector**. Thư viện được phát triển bằng C# 12 / .NET 8 Native AOT, tăng tốc SIMD (AVX2/AVX-512) và C# 12 `[GeneratedRegex]`, đạt băng thông văn bản vượt trội (**37.35 MB/s**, **6.54 µs/câu**) và tích hợp tensor zero-copy trực tiếp với `TokenVector.Numerics.Core.NDArray<int>`.

---

## ⚡ Các Tính năng Nổi bật & Kiến trúc

1. **Zero Heap Allocation trên Hot Loops**:
   - Sử dụng triệt để `ReadOnlySpan<T>`, `ref struct`, và bộ nhớ stack/unmanaged để quá trình phân đoạn từ và mã hóa không phát sinh rác GC.
2. **Hỗ trợ đầy đủ 3 Thuật toán Phổ biến nhất**:
   - **BPE (Byte-Pair Encoding)**: Chuẩn tương thích 100% OpenAI `tiktoken` (`cl100k_base`, `o200k_base`), Meta LLaMA-3, Qwen.
   - **WordPiece**: Thuật toán khớp tiền tố `##` tương thích mô hình BERT và DistilBERT.
   - **Unigram**: Thuật toán quy hoạch động Viterbi tối ưu xác suất tương thích SentencePiece, T5, Gemma.
3. **Tự Huấn luyện Subword BPE (`BpeTrainer`)**:
   - Cho phép tự học và tạo từ điển subwords tối ưu từ bất kỳ file văn bản `.txt` thô nào và xuất ra file `tokenizer.json` chuẩn.
4. **Bộ Nạp Đa Định dạng (`Loaders`)**:
   - Nạp file `tokenizer.json` (Hugging Face), file `.tiktoken` (OpenAI base64), file `vocab.json` + `merges.txt`, và SentencePiece model.
5. **Bộ giải mã Streaming Token an toàn (`IncrementalDecoder`)**:
   - Tự động ghép các byte UTF-8 dở dang khi LLM sinh từng token, bảo đảm không bị lỗi ký tự đa byte (tiếng Việt có dấu, Emoji).
6. **Chat Templates & Mã hóa Cặp câu (Sequence-Pair)**:
   - Hỗ trợ ChatML (`<|im_start|>role...`) và LLaMA-3 (`<|start_header_id|>...`).
   - Hỗ trợ mã hóa 2 câu đồng thời `[CLS] A [SEP] B [SEP]` kèm `TokenTypeIds` (Segment IDs) cho Cross-Encoder và NLI.
7. **Thuật toán Tìm kiếm & Xử lý RAG (BM25 & MinHash)**:
   - **BM25 Vectorizer**: Xếp hạng tài liệu và trích xuất vector thưa cho Hybrid Search.
   - **MinHash (LSH)**: Băm ngữ nghĩa ước lượng độ tương đồng Jaccard và lọc trùng lặp tập dữ liệu lớn.
8. **Cầu nối Zero-Copy Tensor sang `TokenVector.Numerics`**:
   - Xuất trực tiếp `Input_IDs`, `Attention_Mask`, `Position_IDs` thành `NDArray<int>`.
9. **Ứng dụng CLI độc lập (`tkv-text`)**:
   - Mã hóa, giải mã và train tokenizer từ terminal.

---

## 🚀 Hướng dẫn Sử dụng Nhanh

### 1. Tự Huấn luyện (Train) Tokenizer từ Văn bản Thô

```csharp
using TokenVector.Text.Training;

var corpus = new[] {
    "Hệ sinh thái TokenVector AI Engine và mô hình ngôn ngữ lớn LLM.",
    "TokenVector là nền tảng tensor và toán học hiệu năng cao."
};

var trainer = new BpeTrainer(targetVocabSize: 32_000);
var tokenizer = trainer.Train(corpus);

// Xuất ra file tokenizer.json chuẩn Hugging Face
BpeTrainer.ExportToJson(tokenizer, "my_tokenizer.json");
```

### 2. Nạp File `.tiktoken` của OpenAI

```csharp
using TokenVector.Text.Vocab.Loaders;

var (vocab, merges) = TiktokenBpeLoader.Load("cl100k_base.tiktoken");
var tokenizer = Tokenizer.CreateBpe(vocab, merges);
```

### 3. Xuất trực tiếp sang Tensor `TokenVector.Numerics`

```csharp
using TokenVector.Text;
using TokenVector.Numerics.Core;

var tokenizer = Tokenizer.CreateVietnameseBpe();
string[] batch = new[] { "Tính toán hiệu năng cao TokenVector" };

using NDArray<int> tensor = tokenizer.EncodeToTensor(batch);
Console.WriteLine($"Kích thước Tensor: [{tensor.Shape[0]}, {tensor.Shape[1]}]");
```

---

## 📊 Kết quả Benchmark Hiệu năng
 
Đo lường trên nền tảng x64 .NET 8 (Đơn nhân CPU / 100,000 lượt lặp):

| Chỉ số | TokenVector.Text | Microsoft.ML.Tokenizers | OpenAI tiktoken (Python/Rust) |
| :--- | :--- | :--- | :--- |
| **Độ trễ mỗi Câu** | **`6.54 µs`** *(Nhanh nhất)* | `6.64 µs` | `33.09 µs` (Chậm hơn 5.06x) |
| **Băng thông Văn bản** | **`37.35 MB/s`** | `36.77 MB/s` | `7.38 MB/s` |
| **Cấp phát RAM Heap** | **`368 B / op`** *(Tiết kiệm 50.5%)* | `744 B / op` | Rất cao (Python Heap) |
| **Xuất Tensor Zero-Copy** | **✅ CÓ (`NDArray<int>`)** | ❌ Không | ❌ Không |
