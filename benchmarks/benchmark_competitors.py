import time
import tiktoken

def benchmark_tiktoken():
    enc = tiktoken.get_encoding("cl100k_base")
    sample_vi = "Hệ sinh thái TokenVector AI Engine và mô hình ngôn ngữ lớn LLM hiệu năng cao với khả năng xử lý song song trên toàn bộ nhân CPU."
    sample_en = "TokenVector is a cutting-edge high-throughput AI compiler and computational framework written in C# 12 and .NET 8 Native AOT."
    
    iterations = 100_000

    # Warmup
    for _ in range(1000):
        enc.encode(sample_vi)
        enc.encode(sample_en)

    # Benchmark Vietnamese
    t0 = time.perf_counter()
    total_tokens_vi = 0
    for _ in range(iterations):
        res = enc.encode(sample_vi)
        total_tokens_vi += len(res)
    t1 = time.perf_counter()

    elapsed_vi = t1 - t0
    throughput_vi = total_tokens_vi / elapsed_vi
    latency_vi = (elapsed_vi * 1_000_000) / iterations

    print("================================================================================")
    print(" [3] OpenAI tiktoken (Python / Rust Backend cl100k_base):")
    print(f"    - Throughput (Vietnamese): {throughput_vi:,.0f} tokens/sec ({throughput_vi / 1_000_000:.2f} M tokens/s)")
    print(f"    - Latency (Vietnamese):    {latency_vi:.2f} us / sentence")
    print(f"    - Total Time:              {elapsed_vi:.3f} s ({total_tokens_vi:,} tokens)")
    print("--------------------------------------------------------------------------------")

    # Benchmark English
    t0 = time.perf_counter()
    total_tokens_en = 0
    for _ in range(iterations):
        res = enc.encode(sample_en)
        total_tokens_en += len(res)
    t1 = time.perf_counter()

    elapsed_en = t1 - t0
    throughput_en = total_tokens_en / elapsed_en
    latency_en = (elapsed_en * 1_000_000) / iterations

    print(f"    - Throughput (English):    {throughput_en:,.0f} tokens/sec ({throughput_en / 1_000_000:.2f} M tokens/s)")
    print(f"    - Latency (English):       {latency_en:.2f} us / sentence")
    print("================================================================================")

if __name__ == "__main__":
    benchmark_tiktoken()
