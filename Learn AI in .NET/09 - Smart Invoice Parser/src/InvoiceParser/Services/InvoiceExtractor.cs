using InvoiceParser.Models;
using Microsoft.Extensions.AI;

namespace InvoiceParser.Services;

/// <summary>
/// Asks the LLM to read raw invoice text and return a structured <see cref="Invoice"/>.
///
/// LESSON — structured output beats "parse the JSON yourself".
/// <c>GetResponseAsync&lt;Invoice&gt;</c> sends the model a JSON schema derived from
/// the <see cref="Invoice"/> type and deserializes the reply straight into the
/// object. No brittle string parsing, no regex — and if the model returns
/// something that doesn't fit the schema, we can detect it.
/// </summary>
public sealed class InvoiceExtractor(IChatClient chat)
{
    public async Task<Invoice?> ExtractAsync(string rawInvoiceText, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, """
                You extract data from raw invoice text into the provided schema.
                Rules:
                - Use numbers (not strings) for all money and quantity fields.
                - Format the date as ISO YYYY-MM-DD.
                - Copy the numbers EXACTLY as they appear; do NOT recalculate or "fix" them.
                - If a field is not present, leave it null or 0.
                """),
            new(ChatRole.User, rawInvoiceText)
        };

        var response = await chat.GetResponseAsync<Invoice>(messages, cancellationToken: ct);

        // TryGetResult is false if the model's reply couldn't be bound to the schema.
        return response.TryGetResult(out var invoice) ? invoice : null;
    }
}
