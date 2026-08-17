namespace ECommerce.Domain;

public partial class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Method { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? TransactionCode { get; set; }

    /// <summary>vnp_PayDate từ callback VNPay, format yyyyMMddHHmmss (GMT+7).</summary>
    public string? TransactionDate { get; set; }

    /// <summary>Mã phản hồi hoàn tiền từ VNPay (vnp_ResponseId).</summary>
    public string? RefundResponseId { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
    public string? GatewayPaymentIntentId { get; set; }
}