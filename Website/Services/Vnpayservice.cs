using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Website.Services
{
    public class VnpayService
    {
        public (string transactionId, bool success) SimulatePayment(int orderId, decimal amount)
        {
            // Tạo mã giao dịch giả
            var transactionId = $"{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            return (transactionId, true);
        }

        public bool ValidateCallback(IQueryCollection query, out string responseCode, out string txnRef)
        {
            responseCode = query.TryGetValue("vnp_ResponseCode", out var rc) ? rc.ToString() : string.Empty;
            txnRef = query.TryGetValue("vnp_TxnRef", out var tr) ? tr.ToString() : string.Empty;

            // In production validate signature (vnp_SecureHash); this is a simple simulation:
            return !string.IsNullOrEmpty(txnRef);
        }
    }
}
