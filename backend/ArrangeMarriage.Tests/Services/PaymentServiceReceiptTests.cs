using System.Text;
using ArrangeMarriage.Domain.Entities;
using ArrangeMarriage.Infrastructure.Services;
using ArrangeMarriage.Tests.Support;
using PdfSharpCore.Pdf.IO;

namespace ArrangeMarriage.Tests.Services
{
    /// <summary>
    /// Exercises real PDF generation. These guard the PDF dependency (PdfSharpCore and its transitive
    /// packages): if an upgrade or override breaks rendering at runtime, these fail in CI.
    /// </summary>
    public class PaymentServiceReceiptTests
    {
        private static Payment NewPayment() => new()
        {
            UserId = Guid.NewGuid(),
            Amount = 1500.50m,
            Type = PaymentType.UPI,
            Status = PaymentStatus.Paid,
            TransactionId = "txn_test_123",
            FeeType = "Registration",
            PaymentDate = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        };

        [Fact]
        public async Task GenerateReceiptPdfAsync_ReturnsParseableSinglePagePdf()
        {
            await using var context = TestDb.Create();
            var payment = NewPayment();
            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            var pdf = await new PaymentService(context).GenerateReceiptPdfAsync(payment.PaymentId);

            Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
            Assert.Contains("%%EOF", Encoding.ASCII.GetString(pdf, pdf.Length - 16, 16));

            // Re-open the bytes with the PDF reader: proves the output is structurally valid, not just non-empty.
            using var stream = new MemoryStream(pdf);
            using var reopened = PdfReader.Open(stream, PdfDocumentOpenMode.InformationOnly);
            Assert.Equal(1, reopened.PageCount);
        }

        [Fact]
        public async Task GenerateReceiptPdfAsync_WithMissingOptionalFields_StillRenders()
        {
            await using var context = TestDb.Create();
            var payment = NewPayment();
            payment.TransactionId = null;
            payment.FeeType = null;
            payment.PaymentDate = null;
            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            var pdf = await new PaymentService(context).GenerateReceiptPdfAsync(payment.PaymentId);

            Assert.True(pdf.Length > 0);
        }

        [Fact]
        public async Task GenerateReceiptPdfAsync_UnknownPayment_ThrowsKeyNotFound()
        {
            await using var context = TestDb.Create();

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => new PaymentService(context).GenerateReceiptPdfAsync(Guid.NewGuid()));
        }
    }
}
