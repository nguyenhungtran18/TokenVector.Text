using System;
using System.Collections.Generic;
using System.Text;

namespace TokenVector.Text.Templates;

/// <summary>
/// Pre-configured chat templates for formatting conversational messages into LLM input prompts.
/// </summary>
public sealed class ChatTemplate
{
    public delegate string TemplateFormatter(IEnumerable<ChatMessage> messages, bool addGenerationPrompt);

    private readonly TemplateFormatter _formatter;

    public ChatTemplate(TemplateFormatter formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>
    /// Applies template to conversational messages and renders prompt string.
    /// </summary>
    public string Render(IEnumerable<ChatMessage> messages, bool addGenerationPrompt = true)
    {
        return _formatter(messages, addGenerationPrompt);
    }

    /// <summary>
    /// ChatML format (&lt;|im_start|&gt;role\ncontent&lt;|im_end|&gt;) used by Qwen, Yi, DeepSeek, and OpenAI fine-tunes.
    /// </summary>
    public static ChatTemplate ChatML => new((messages, addGenerationPrompt) =>
    {
        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            sb.Append("<|im_start|>").Append(msg.Role).Append('\n');
            sb.Append(msg.Content);
            sb.Append("<|im_end|>\n");
        }
        if (addGenerationPrompt)
        {
            sb.Append("<|im_start|>assistant\n");
        }
        return sb.ToString();
    });

    /// <summary>
    /// Meta LLaMA-3 Instruct chat template.
    /// </summary>
    public static ChatTemplate Llama3 => new((messages, addGenerationPrompt) =>
    {
        var sb = new StringBuilder();
        sb.Append("<|begin_of_text|>");
        foreach (var msg in messages)
        {
            sb.Append("<|start_header_id|>").Append(msg.Role).Append("<|end_header_id|>\n\n");
            sb.Append(msg.Content);
            sb.Append("<|eot_id|>");
        }
        if (addGenerationPrompt)
        {
            sb.Append("<|start_header_id|>assistant<|end_header_id|>\n\n");
        }
        return sb.ToString();
    });
}
