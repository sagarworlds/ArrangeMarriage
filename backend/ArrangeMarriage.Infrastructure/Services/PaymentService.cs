using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;
using ArrangeMarriage.Infrastructure.Persistence;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace ArrangeMarriage.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> ProcessPaymentAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(Guid userId)
        {
            return await _context.Payments
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status, string? transactionId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return false;

            payment.Status = status;
            payment.TransactionId = transactionId;
            payment.PaymentDate = DateTime.UtcNow;
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<byte[]> GenerateReceiptPdfAsync(Guid paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) throw new Exception("Payment not found");

            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            var fontTitle = new XFont("Arial", 20, XFontStyle.Bold);
            var fontHeader = new XFont("Arial", 12, XFontStyle.Bold);
            var fontRegular = new XFont("Arial", 10, XFontStyle.Regular);

            // Title
            gfx.DrawString("MARRIAGE ARRANGER RECEIPT", fontTitle, XBrushes.Navy, new XRect(0, 40, page.Width, 40), XStringFormats.TopCenter);

            // Divider Line
            gfx.DrawLine(new XPen(XColors.Navy, 2), 50, 90, page.Width - 50, 90);

            // Receipt Info Table/Labels
            int yPos = 120;
            int xLabel = 70;
            int xVal = 200;

            void DrawRow(string label, string value)
            {
                gfx.DrawString(label, fontHeader, XBrushes.DarkSlateGray, xLabel, yPos);
                gfx.DrawString(value, fontRegular, XBrushes.Black, xVal, yPos);
                yPos += 25;
            }

            DrawRow("Receipt ID:", payment.PaymentId.ToString());
            DrawRow("User ID:", payment.UserId.ToString());
            DrawRow("Transaction ID:", payment.TransactionId ?? "N/A");
            DrawRow("Fee Type:", payment.FeeType ?? "Membership Package Fee");
            DrawRow("Amount Paid:", $"INR {payment.Amount:F2}");
            DrawRow("Payment Type:", payment.Type.ToString());
            DrawRow("Status:", payment.Status.ToString());
            DrawRow("Date & Time:", payment.PaymentDate?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));

            // Footer
            gfx.DrawLine(new XPen(XColors.LightGray, 1), 50, yPos + 10, page.Width - 50, yPos + 10);
            gfx.DrawString("Thank you for your business. For support, contact admin@marriagearranger.com", 
                new XFont("Arial", 8, XFontStyle.Italic), 
                XBrushes.Gray, 
                new XRect(0, yPos + 25, page.Width, 20), 
                XStringFormats.TopCenter);

            using (var stream = new MemoryStream())
            {
                document.Save(stream);
                return stream.ToArray();
            }
        }
    }
}
