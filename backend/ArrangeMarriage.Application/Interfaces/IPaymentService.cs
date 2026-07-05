using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<Payment> ProcessPaymentAsync(Payment payment);
        Task<IEnumerable<Payment>> GetUserPaymentsAsync(Guid userId);
        Task<bool> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status, string? transactionId);
        Task<byte[]> GenerateReceiptPdfAsync(Guid paymentId);
    }
}
