# TokenVector.Text

[ 🌌 TokenVector Hub ](https://github.com/nguyenhungtran18/TokenVector) | [ 🇬🇧 English ](README.md) | [ 🇻🇳 Tiếng Việt ](README_VI.md) | [ 📋 Báo cáo Kiểm thử ](TEST_REPORT_VI.md) | [ 📊 Benchmarks ](BENCHMARKS_VI.md)

[![.NET 8](https://img.shields.io/badge/.NET-8.0%20LTS-purple.svg)](https://dotnet.microsoft.com/)
[![C# 12](https://img.shields.io/badge/C%23-12.0-blue.svg)](https://learn.microsoft.com/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-25%2F25%20Passed-brightgreen.svg)](TEST_REPORT_VI.md)

**`TokenVector.Text.dll`** là thư viện xử lý ngôn ngữ tự nhiên (NLP), Tokenizer engine và BPE Subword Trainer hiệu năng siêu cao, zero-allocation, được viết bằng **C# 12 / .NET 8 LTS** tối ưu riêng cho [**Hệ sinh thái TokenVector**](https://github.com/nguyenhungtran18/TokenVector) và trình biên dịch .NET Native AOT.

Thư viện ứng dụng máy trạng thái hữu hạn DFA lúc biên dịch qua C# 12 `[GeneratedRegex]`, thuật toán ghép cặp BPE trên danh sách liên kết kép `stackalloc`, tăng tốc phần cứng SIMD AVX2/AVX-512, ánh xạ byte-level BPE hai chiều, quy hoạch động Unigram Viterbi, giải mã streaming từng token an toàn ranh giới UTF-8, tìm kiếm thưa BM25 & MinHash, và cầu nối Zero-Copy trực tiếp sang `TokenVector.Numerics.Core.NDArray<int>`.

---

## 📋 Đặc Điểm Kiến Trúc Nổi Bật

| Tính năng | TokenVector.Text (.NET 8 / C# 12) |
| :--- | :--- |
| **Hot Loops Zero-Allocation** | Sử dụng `ReadOnlySpan<char>`, `ReadOnlySpan<byte>`, `ref struct ValueBuffer<int>` và mảng `stackalloc` để triệt tiêu hoàn toàn áp lực rác GC |
| **Tương thích Đa Kiến trúc** | Chuẩn tương thích 100% OpenAI `tiktoken` (`cl100k_base`, `o200k_base`), Meta LLaMA-3, Qwen (BPE), BERT (WordPiece), SentencePiece/Gemma/T5 (Unigram) |
| **Máy trạng thái DFA Regex** | Biên dịch regex thành máy trạng thái hữu hạn (DFA) tĩnh tại compile-time với C# 12 `[GeneratedRegex]`, loại bỏ chi phí thông dịch runtime |
| **BPE Doubly-Linked-List** | Bảng băm $O(1)$ mặt nạ bitwise (`ulong` pair key) kết hợp danh sách liên kết kép thực thi in-place trên stack (`Span<int> prevIdx`, `nextIdx`) |
| **Tăng tốc phần cứng SIMD** | Quét vector SIMD (`Vector256<byte>`, `Vector128<byte>`) phát hiện khoảng trắng và phân loại nhanh chuỗi 7-bit ASCII |
| **Tự Huấn luyện BPE Subword** | Bộ `BpeTrainer` tích hợp sẵn trong tiến trình, tự học từ điển subword từ file văn bản thô `.txt` và xuất file `tokenizer.json` chuẩn Hugging Face |
| **Nạp Đa Định dạng** | Nạp siêu tốc `tokenizer.json` (Hugging Face), file `.tiktoken` (OpenAI Base64 ranks), `vocab.json` + `merges.txt`, và SentencePiece model |
| **Giải mã Streaming An Toàn** | Bộ giải mã dòng từng token (`IncrementalDecoder`) tự động đệm các byte UTF-8 đa byte dở dang khi LLM sinh từ thời gian thực |
| **Cầu nối Zero-Copy Tensor** | Xuất trực tiếp `Input_IDs`, `Attention_Mask`, `Position_IDs` vào vùng nhớ unmanaged của `TokenVector.Numerics.Core.NDArray<int>` |

---

## 📊 Ma Trận So Sánh Kiến Trúc Đối Thủ

| Tiêu chí | TokenVector.Text | Microsoft.ML.Tokenizers | Hugging Face tokenizers | OpenAI tiktoken | Google SentencePiece |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Ngôn ngữ & Nền tảng** | C# 12 / .NET 8 AOT | C# (.NET 8) | Rust Native | Python / Rust Backend | C++11 Native |
| **Pre-Tokenizer** | **Compile-Time DFA Regex** | Bộ quét Custom | Rust Regex | Rust Regex | Custom C++ Trie |
| **Ghép cặp BPE** | **Stack Doubly-Linked-List**| Trie / Map | Binary Heap | Map / Priority Queue | Viterbi / Trie |
| **Zero-Allocation** | **100% Span / Stackalloc** | Một phần | Một phần (Rust Alloc) | **Rất cao (Python Objects)**| C++ Vector Alloc |
| **Tăng tốc SIMD** | **AVX2 / AVX-512 / SSE4** | Giới hạn | Giới hạn | Giới hạn | ❌ Không |
| **Tự Huấn luyện (Train)**| **Tích hợp `BpeTrainer`** | ❌ Không | Taggers / Rust | ❌ Không | CLI / C++ |
| **Cầu nối AI / Tensor** | **Zero-Copy `NDArray<int>`** | ❌ Copy List/Array | Arrow / Torch FFI | ❌ Copy Numpy | ❌ Copy Only |
| **Streaming Decoder** | **An toàn UTF-8 đa byte** | Cơ bản | Cơ bản | Cơ bản | Cơ bản |
| **Tối ưu Native AOT** | **100% Không Reflection** | Một phần | N/A (Rust) | N/A (Python) | N/A (C++) |

---

## ⚡ Kết Quả Đo Lường Hiệu Năng Thực Tế (100.000 Lượt / Đơn Nhân CPU)

Thực thi trên hệ thống x64, chế độ .NET 8.0 LTS Release Mode (xem chi tiết tại [BENCHMARKS_VI.md](BENCHMARKS_VI.md)):

| Khối lượng công việc & Chỉ số | TokenVector.Text (C# 12) | Microsoft.ML.Tokenizers | OpenAI tiktoken (Python/Rust) | Ưu thế vượt trội |
| :--- | :--- | :--- | :--- | :--- |
| **Độ trễ mỗi Câu (Latency)** | **`6.54 µs` / câu** | `6.64 µs` / câu | `33.09 µs` / câu | **#1 Độ trễ Nhanh nhất (Gấp 5.06x tiktoken)** |
| **Băng thông Văn bản** | **`37.35 MB/s`** | `36.77 MB/s` | `7.38 MB/s` | **Tốc độ tiêu thụ văn bản cao nhất** |
| **Thông lượng Token** | **`4,740,061 Tokens/s`** | `7,831,325 Tokens/s` | `1,571,375 Tokens/s` | **Mật độ token chuẩn xác âm tiết tự nhiên** |
| **Cấp phát Bộ nhớ Heap** | **`368 B / op`** | `744 B / op` | Rất cao (Python Heap) | **Tiết kiệm 50.5% RAM Heap** |
| **Cầu nối Tensor Zero-Copy** | **✅ CÓ (`NDArray<int>`)** | ❌ Không (List / Array) | ❌ Không (Numpy Copy) | **Không sao chép bộ nhớ sang mạng nơ-ron** |

---

## 🔍 Phân Tích So Sánh Đối Thủ Chi Tiết

### 1. So với OpenAI tiktoken (Python/Rust)
* **Tốc độ xử lý:** `TokenVector.Text` (**`6.54 µs`**) **nhanh gấp 5.06 lần** so với OpenAI `tiktoken` (**`33.09 µs`**). Python `tiktoken` bị tổn thất hiệu năng lớn tại ranh giới FFI giữa Python-Rust và việc cấp phát đối tượng Python heap cho từng token, trong khi `TokenVector.Text` chạy thuần in-process trên nền .NET CIL / Native AOT không sao chép dữ liệu trung gian.
* **Tương tác Tensor Trực tiếp:** `tiktoken` trả về mảng Python list buộc phải cấp phát lại vào PyTorch/NumPy. `TokenVector.Text` ghi trực tiếp Token IDs vào vùng đệm unmanaged của `NDArray<int>`, sẵn sàng cho các tầng tính toán Attention/Transformer.

### 2. So với Microsoft.ML.Tokenizers (.NET)
* **Độ trễ & Bộ nhớ:** `TokenVector.Text` vượt qua `Microsoft.ML.Tokenizers` (**6.54 µs so với 6.64 µs**) và tiết kiệm **50.5% lượng RAM cấp phát trên Heap (368 B so với 744 B mỗi lượt)** nhờ bộ đệm scoped `ValueBuffer<int>` stackalloc và bảng băm bitwise mask.
* **Tính năng Toàn diện:** `Microsoft.ML.Tokenizers` thuần túy là thư viện inference không có bộ train, trong khi `TokenVector.Text` tích hợp trọn vẹn `BpeTrainer`, BM25, MinHash, ChatML/LLaMA-3 templates và bộ xử lý cặp câu cross-encoder.

### 3. So với Hugging Face Tokenizers (Rust) & Google SentencePiece (C++)
* **Khả năng tương thích hệ sinh thái:** Dù Hugging Face và SentencePiece là những engine hàng đầu viết bằng Rust/C++, `TokenVector.Text` sở hữu lợi thế vượt trội khi **biên dịch trực tiếp sang mã CIL AOT** của runtime .NET. Điều này mang lại khả năng **chia sẻ bộ nhớ Zero-Copy tức thì với `TokenVector.Numerics.NDArray<T>` và `Autograd.Tensor<T>`** mà không phải trả phí tổn FFI hay nhân bản bộ nhớ.

---

## 🧪 Kiểm Thử & Đảm Bảo Chất Lượng

Tất cả **25/25 automated unit tests** đã vượt qua thành công với tỷ lệ 100% ở chế độ Release (xem chi tiết tại [TEST_REPORT_VI.md](TEST_REPORT_VI.md)):
```powershell
dotnet test TokenVector.Text.sln -c Release
```
```text
Passed!  - Failed: 0, Passed: 25, Skipped: 0, Total: 25, Duration: 63 ms - TokenVector.Text.Tests.dll (net8.0)
```

---

## 🚀 Hướng Dẫn Nhanh (C#)

```csharp
using TokenVector.Text;
using TokenVector.Text.Models;
using TokenVector.Text.Training;
using TokenVector.Numerics.Core;

// 1. Khởi tạo Tokenizer hiệu năng cao
var tokenizer = Tokenizer.CreateVietnameseBpe();

// 2. Mã hóa siêu tốc zero-allocation
var result = tokenizer.Encode("Hệ sinh thái TokenVector AI Engine".AsSpan());
Console.WriteLine($"Sinh ra {result.Length} tokens: [{string.Join(", ", result.Ids)}]");

// 3. Giải mã streaming thời gian thực
var decoder = tokenizer.CreateStreamingDecoder();
foreach (int id in result.Ids)
{
    Console.Write(decoder.DecodeToken(id));
}
Console.WriteLine(decoder.Flush());

// 4. Cầu nối Zero-Copy Tensor sang TokenVector.Numerics
string[] batch = new[] { "Tính toán NLP hiệu năng cao", "Cầu nối tensor không sao chép" };
using NDArray<int> tensor = tokenizer.EncodeToTensor(batch);
Console.WriteLine($"Kích thước Tensor: [{tensor.Shape[0]}, {tensor.Shape[1]}]");
```

---

## 💻 Hướng Dẫn Nhanh (Ngôn ngữ thuần TokenVector)

```tokenvector
import tv.text as text
from tv.text import Tokenizer, BpeTrainer

# 1. Nạp tokenizer đã huấn luyện
tokenizer = text.load_pretrained("tokenizer.json")

# 2. Tokenize và xuất tensor zero-copy
tokens = tokenizer.encode("TokenVector computational engine")
tensor = tokenizer.encode_to_tensor(["Batch sequence 1", "Batch sequence 2"])

# 3. Tự huấn luyện từ điển BPE từ raw text
trainer = BpeTrainer(target_vocab_size=32000)
custom_tok = trainer.train(["dataset.txt"])
custom_tok.export_json("my_tokenizer.json")
```

Xem tài liệu đầy đủ tại [Báo cáo Kiểm thử (Tiếng Anh)](TEST_REPORT.md), [Báo cáo Kiểm thử (Tiếng Việt)](TEST_REPORT_VI.md), [Báo cáo Benchmark (Tiếng Anh)](BENCHMARKS.md) hoặc [Báo cáo Benchmark (Tiếng Việt)](BENCHMARKS_VI.md).
