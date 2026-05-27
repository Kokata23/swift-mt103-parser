using Microsoft.Data.Sqlite;
using SwiftMT103Parser.Models;

namespace SwiftMT103Parser.Data;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("Sqlite") ?? "Data Source=swift_messages.db";
        _logger = logger;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Mt103Messages (
                Id                              INTEGER PRIMARY KEY AUTOINCREMENT,
                ParsedAt                        TEXT NOT NULL,
                RawContent                      TEXT NOT NULL,
                Block1ApplicationId             TEXT,
                Block1ServiceId                 TEXT,
                Block1LogicalTerminal           TEXT,
                Block1SessionNumber             TEXT,
                Block1SequenceNumber            TEXT,
                Block2Direction                 TEXT,
                Block2MessageType               TEXT,
                Block2ReceiverAddress           TEXT,
                Block2Priority                  TEXT,
                Block3ValidationFlag            TEXT,
                Field20TransactionRef           TEXT,
                Field23BankOperationCode        TEXT,
                Field32AValueDate               TEXT,
                Field32ACurrency                TEXT,
                Field32AAmount                  TEXT,
                Field33BCurrency                TEXT,
                Field33BAmount                  TEXT,
                Field50KOrderingCustomerIban    TEXT,
                Field50KOrderingCustomerName    TEXT,
                Field50KOrderingCustomerAddress TEXT,
                Field57AAccountWithInstitution  TEXT,
                Field59BeneficiaryIban          TEXT,
                Field59BeneficiaryName          TEXT,
                Field59BeneficiaryAddress       TEXT,
                Field70RemittanceInfo           TEXT,
                Field71AChargesCode             TEXT,
                Block5Mac                       TEXT,
                Block5Chk                       TEXT
            );";
        cmd.ExecuteNonQuery();
        _logger.LogInformation("Database initialized.");
    }

    public int SaveMessage(Mt103Message msg)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Mt103Messages (
                ParsedAt, RawContent,
                Block1ApplicationId, Block1ServiceId, Block1LogicalTerminal, Block1SessionNumber, Block1SequenceNumber,
                Block2Direction, Block2MessageType, Block2ReceiverAddress, Block2Priority,
                Block3ValidationFlag,
                Field20TransactionRef, Field23BankOperationCode,
                Field32AValueDate, Field32ACurrency, Field32AAmount,
                Field33BCurrency, Field33BAmount,
                Field50KOrderingCustomerIban, Field50KOrderingCustomerName, Field50KOrderingCustomerAddress,
                Field57AAccountWithInstitution,
                Field59BeneficiaryIban, Field59BeneficiaryName, Field59BeneficiaryAddress,
                Field70RemittanceInfo, Field71AChargesCode,
                Block5Mac, Block5Chk
            ) VALUES (
                @ParsedAt, @RawContent,
                @B1AppId, @B1SvcId, @B1Lt, @B1Sess, @B1Seq,
                @B2Dir, @B2MsgType, @B2Recv, @B2Pri,
                @B3Val,
                @F20, @F23B,
                @F32ADate, @F32ACcy, @F32AAmt,
                @F33BCcy, @F33BAmt,
                @F50KIban, @F50KName, @F50KAddr,
                @F57A,
                @F59Iban, @F59Name, @F59Addr,
                @F70, @F71A,
                @B5Mac, @B5Chk
            );
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("@ParsedAt", msg.ParsedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@RawContent", msg.RawContent);
        cmd.Parameters.AddWithValue("@B1AppId", (object?)msg.Block1ApplicationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B1SvcId", (object?)msg.Block1ServiceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B1Lt", (object?)msg.Block1LogicalTerminal ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B1Sess", (object?)msg.Block1SessionNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B1Seq", (object?)msg.Block1SequenceNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B2Dir", (object?)msg.Block2Direction ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B2MsgType", (object?)msg.Block2MessageType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B2Recv", (object?)msg.Block2ReceiverAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B2Pri", (object?)msg.Block2Priority ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B3Val", (object?)msg.Block3ValidationFlag ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F20", (object?)msg.Field20TransactionRef ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F23B", (object?)msg.Field23BankOperationCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F32ADate", (object?)msg.Field32AValueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F32ACcy", (object?)msg.Field32ACurrency ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F32AAmt", (object?)msg.Field32AAmount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F33BCcy", (object?)msg.Field33BCurrency ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F33BAmt", (object?)msg.Field33BAmount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F50KIban", (object?)msg.Field50KOrderingCustomerIban ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F50KName", (object?)msg.Field50KOrderingCustomerName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F50KAddr", (object?)msg.Field50KOrderingCustomerAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F57A", (object?)msg.Field57AAccountWithInstitution ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F59Iban", (object?)msg.Field59BeneficiaryIban ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F59Name", (object?)msg.Field59BeneficiaryName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F59Addr", (object?)msg.Field59BeneficiaryAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F70", (object?)msg.Field70RemittanceInfo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@F71A", (object?)msg.Field71AChargesCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B5Mac", (object?)msg.Block5Mac ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B5Chk", (object?)msg.Block5Chk ?? DBNull.Value);

        var id = Convert.ToInt32(cmd.ExecuteScalar());
        _logger.LogInformation("Saved MT103 message with Id={Id}", id);
        return id;
    }

    public List<Mt103Message> GetAllMessages()
    {
        var messages = new List<Mt103Message>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Mt103Messages ORDER BY Id DESC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            messages.Add(MapRow(reader));
        }
        return messages;
    }

    private static Mt103Message MapRow(SqliteDataReader r)
    {
        string? Get(string col) => r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetString(r.GetOrdinal(col));

        return new Mt103Message
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            ParsedAt = DateTime.Parse(r.GetString(r.GetOrdinal("ParsedAt"))),
            RawContent = r.GetString(r.GetOrdinal("RawContent")),
            Block1ApplicationId = Get("Block1ApplicationId"),
            Block1ServiceId = Get("Block1ServiceId"),
            Block1LogicalTerminal = Get("Block1LogicalTerminal"),
            Block1SessionNumber = Get("Block1SessionNumber"),
            Block1SequenceNumber = Get("Block1SequenceNumber"),
            Block2Direction = Get("Block2Direction"),
            Block2MessageType = Get("Block2MessageType"),
            Block2ReceiverAddress = Get("Block2ReceiverAddress"),
            Block2Priority = Get("Block2Priority"),
            Block3ValidationFlag = Get("Block3ValidationFlag"),
            Field20TransactionRef = Get("Field20TransactionRef"),
            Field23BankOperationCode = Get("Field23BankOperationCode"),
            Field32AValueDate = Get("Field32AValueDate"),
            Field32ACurrency = Get("Field32ACurrency"),
            Field32AAmount = Get("Field32AAmount"),
            Field33BCurrency = Get("Field33BCurrency"),
            Field33BAmount = Get("Field33BAmount"),
            Field50KOrderingCustomerIban = Get("Field50KOrderingCustomerIban"),
            Field50KOrderingCustomerName = Get("Field50KOrderingCustomerName"),
            Field50KOrderingCustomerAddress = Get("Field50KOrderingCustomerAddress"),
            Field57AAccountWithInstitution = Get("Field57AAccountWithInstitution"),
            Field59BeneficiaryIban = Get("Field59BeneficiaryIban"),
            Field59BeneficiaryName = Get("Field59BeneficiaryName"),
            Field59BeneficiaryAddress = Get("Field59BeneficiaryAddress"),
            Field70RemittanceInfo = Get("Field70RemittanceInfo"),
            Field71AChargesCode = Get("Field71AChargesCode"),
            Block5Mac = Get("Block5Mac"),
            Block5Chk = Get("Block5Chk"),
        };
    }
}
