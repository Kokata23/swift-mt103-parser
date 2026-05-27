using System.Text.RegularExpressions;
using SwiftMT103Parser.Models;

namespace SwiftMT103Parser.Services;

public class Mt103ParserService
{
    private readonly ILogger<Mt103ParserService> _logger;

    public Mt103ParserService(ILogger<Mt103ParserService> logger)
    {
        _logger = logger;
    }

    public Mt103Message Parse(string raw)
    {
        _logger.LogInformation("Parsing MT103 message ({Length} chars)", raw.Length);
        var msg = new Mt103Message
        {
            RawContent = raw,
            ParsedAt = DateTime.UtcNow
        };

        // The SWIFT message may wrap multiple envelopes; the last {1:F01...} block
        // is the actual message header. We identify distinct top-level blocks by
        // scanning for balanced-brace block markers at the root level.
        var blocks = ExtractTopLevelBlocks(raw);

        // There may be two {1:...} blocks (the FIN envelope and the message itself).
        // Block 2 helps us identify the real message: it appears right after the
        // {1:F01...} application header. We process in order.
        string? block1 = null, block2 = null, block3 = null, block4 = null, block5 = null;

        foreach (var (tag, content) in blocks)
        {
            switch (tag)
            {
                case "1":
                    // Keep only the F01 variant (actual message header), overwriting F21
                    if (content.StartsWith("F01", StringComparison.OrdinalIgnoreCase) || block1 == null)
                        block1 = content;
                    break;
                case "2": block2 = content; break;
                case "3": block3 = content; break;
                case "4": block4 = content; break;
                case "5": block5 = content; break;
            }
        }

        if (block1 != null) ParseBlock1(block1, msg);
        if (block2 != null) ParseBlock2(block2, msg);
        if (block3 != null) ParseBlock3(block3, msg);
        if (block4 != null) ParseBlock4(block4, msg);
        if (block5 != null) ParseBlock5(block5, msg);

        return msg;
    }

    // Extracts top-level {tag:content} blocks, handling nested braces inside block 4.
    private static List<(string tag, string content)> ExtractTopLevelBlocks(string raw)
    {
        var result = new List<(string, string)>();
        int i = 0;
        while (i < raw.Length)
        {
            int open = raw.IndexOf('{', i);
            if (open < 0) break;

            // Find matching close brace
            int depth = 0;
            int close = -1;
            for (int j = open; j < raw.Length; j++)
            {
                if (raw[j] == '{') depth++;
                else if (raw[j] == '}')
                {
                    depth--;
                    if (depth == 0) { close = j; break; }
                }
            }
            if (close < 0) break;

            string blockContent = raw.Substring(open + 1, close - open - 1);
            int colon = blockContent.IndexOf(':');
            if (colon > 0)
            {
                string tag = blockContent[..colon];
                string content = blockContent[(colon + 1)..];
                result.Add((tag, content));
            }
            i = close + 1;
        }
        return result;
    }

    // {1:F01PRCBBGSFAXXX2082167565}
    // Format: F<svcId><logicalTerminal(12)><session(4)><seq(6)>
    private static void ParseBlock1(string content, Mt103Message msg)
    {
        if (content.Length < 1) return;
        msg.Block1ApplicationId = content[..1];          // "F"
        if (content.Length >= 3)
            msg.Block1ServiceId = content[1..3];          // "01"
        if (content.Length >= 15)
            msg.Block1LogicalTerminal = content[3..15];   // "PRCBBGSFAXXX"
        if (content.Length >= 19)
            msg.Block1SessionNumber = content[15..19];    // "2082"
        if (content.Length >= 25)
            msg.Block1SequenceNumber = content[19..25];   // "167565"
    }

    // {2:I103COBADEFFXXXXN}
    // Input  => I<type><receiver><priority>
    // Output => O<type><time><mirtag><date><priority>
    private static void ParseBlock2(string content, Mt103Message msg)
    {
        if (content.Length < 1) return;
        msg.Block2Direction = content[..1];   // "I" or "O"

        if (content.Length >= 4)
            msg.Block2MessageType = content[1..4];   // "103"

        if (content.Length >= 16)
            msg.Block2ReceiverAddress = content[4..16].TrimEnd('X'); // "COBADEFF"

        if (content.Length >= 17)
            msg.Block2Priority = content[^1..];      // last char "N"
    }

    // {3:{119:STP}}
    private static void ParseBlock3(string content, Mt103Message msg)
    {
        var m = Regex.Match(content, @"\{119:([^}]+)\}");
        if (m.Success) msg.Block3ValidationFlag = m.Groups[1].Value.Trim();
    }

    // Block 4 contains tagged fields separated by CRLF or LF, starting with :<tag>:
    private void ParseBlock4(string content, Mt103Message msg)
    {
        // Normalize line endings and split into field segments.
        // Each field starts at a line beginning with :<tag(2-3 chars)>:
        content = content.Replace("\r\n", "\n").Replace("\r", "\n");

        // Split on field tag boundaries; keep delimiter with the segment.
        var segments = Regex.Split(content, @"(?=\n:(?:[0-9]{2}[A-Z]?|[0-9]{2}):)");

        foreach (var seg in segments)
        {
            var line = seg.TrimStart('\n');
            var tagMatch = Regex.Match(line, @"^:([0-9]{2}[A-Z]?):(.*)$", RegexOptions.Singleline);
            if (!tagMatch.Success) continue;

            string tag = tagMatch.Groups[1].Value;
            string value = tagMatch.Groups[2].Value.Trim('\n', '\r', '-');

            _logger.LogDebug("Block4 field :{tag}: = {value}", tag, value);

            switch (tag)
            {
                case "20":
                    msg.Field20TransactionRef = value.Trim();
                    break;

                case "23B":
                    msg.Field23BankOperationCode = value.Trim();
                    break;

                case "32A":
                    ParseField32A(value.Trim(), msg);
                    break;

                case "33B":
                    ParseField33B(value.Trim(), msg);
                    break;

                case "50K":
                    ParsePartyField50K(value, msg);
                    break;

                case "57A":
                    msg.Field57AAccountWithInstitution = value.Trim();
                    break;

                case "59":
                    ParsePartyField59(value, msg);
                    break;

                case "70":
                    msg.Field70RemittanceInfo = value.Trim();
                    break;

                case "71A":
                    msg.Field71AChargesCode = value.Trim();
                    break;
            }
        }
    }

    // 32A: 160217EUR540,00  => date=160217  ccy=EUR  amount=540,00
    private static void ParseField32A(string value, Mt103Message msg)
    {
        // Format: YYMMDD + 3-char currency + amount
        if (value.Length < 10) { msg.Field32AValueDate = value; return; }
        msg.Field32AValueDate = value[..6];
        msg.Field32ACurrency = value[6..9];
        msg.Field32AAmount = value[9..];
    }

    // 33B: EUR540,00 => ccy=EUR  amount=540,00
    private static void ParseField33B(string value, Mt103Message msg)
    {
        if (value.Length < 4) { msg.Field33BCurrency = value; return; }
        msg.Field33BCurrency = value[..3];
        msg.Field33BAmount = value[3..];
    }

    // 50K: /BG95RZBB91556261794271\nOKO 1000 OOD\nTZAR IVAN SHISHMAN ? 11\nSOFIA, BULGARIA
    private static void ParsePartyField50K(string value, Mt103Message msg)
    {
        var lines = value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int nameStart = 0;
        if (lines.Length > 0 && lines[0].StartsWith('/'))
        {
            msg.Field50KOrderingCustomerIban = lines[0][1..].Trim();
            nameStart = 1;
        }
        if (nameStart < lines.Length)
            msg.Field50KOrderingCustomerName = lines[nameStart].Trim();
        if (nameStart + 1 < lines.Length)
            msg.Field50KOrderingCustomerAddress = string.Join(", ", lines[(nameStart + 1)..].Select(l => l.Trim()));
    }

    // 59: /DE83500105172667785918\nFRANCA CEVALES\nMUNCHENER STR. 35, GERMANY
    private static void ParsePartyField59(string value, Mt103Message msg)
    {
        var lines = value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int nameStart = 0;
        if (lines.Length > 0 && lines[0].StartsWith('/'))
        {
            msg.Field59BeneficiaryIban = lines[0][1..].Trim();
            nameStart = 1;
        }
        if (nameStart < lines.Length)
            msg.Field59BeneficiaryName = lines[nameStart].Trim();
        if (nameStart + 1 < lines.Length)
            msg.Field59BeneficiaryAddress = string.Join(", ", lines[(nameStart + 1)..].Select(l => l.Trim()));
    }

    // {5:{MAC:00000000}{CHK:6BC2D5BE9937}}
    private static void ParseBlock5(string content, Mt103Message msg)
    {
        var mac = Regex.Match(content, @"\{MAC:([^}]+)\}");
        if (mac.Success) msg.Block5Mac = mac.Groups[1].Value.Trim();

        var chk = Regex.Match(content, @"\{CHK:([^}]+)\}");
        if (chk.Success) msg.Block5Chk = chk.Groups[1].Value.Trim();
    }
}
