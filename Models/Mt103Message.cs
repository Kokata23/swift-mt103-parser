namespace SwiftMT103Parser.Models;

public class Mt103Message
{
    public int Id { get; set; }
    public DateTime ParsedAt { get; set; }
    public string RawContent { get; set; } = string.Empty;

    // Block 1
    public string? Block1ApplicationId { get; set; }
    public string? Block1ServiceId { get; set; }
    public string? Block1LogicalTerminal { get; set; }
    public string? Block1SessionNumber { get; set; }
    public string? Block1SequenceNumber { get; set; }

    // Block 2
    public string? Block2Direction { get; set; }
    public string? Block2MessageType { get; set; }
    public string? Block2ReceiverAddress { get; set; }
    public string? Block2Priority { get; set; }

    // Block 3
    public string? Block3ValidationFlag { get; set; }

    // Block 4 fields
    public string? Field20TransactionRef { get; set; }
    public string? Field23BankOperationCode { get; set; }
    public string? Field32AValueDate { get; set; }
    public string? Field32ACurrency { get; set; }
    public string? Field32AAmount { get; set; }
    public string? Field33BCurrency { get; set; }
    public string? Field33BAmount { get; set; }
    public string? Field50KOrderingCustomerIban { get; set; }
    public string? Field50KOrderingCustomerName { get; set; }
    public string? Field50KOrderingCustomerAddress { get; set; }
    public string? Field57AAccountWithInstitution { get; set; }
    public string? Field59BeneficiaryIban { get; set; }
    public string? Field59BeneficiaryName { get; set; }
    public string? Field59BeneficiaryAddress { get; set; }
    public string? Field70RemittanceInfo { get; set; }
    public string? Field71AChargesCode { get; set; }

    // Block 5
    public string? Block5Mac { get; set; }
    public string? Block5Chk { get; set; }
}
