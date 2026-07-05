using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] Payment payment)
        {
            var result = await _paymentService.ProcessPaymentAsync(payment);
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPayments(Guid userId)
        {
            var payments = await _paymentService.GetUserPaymentsAsync(userId);
            return Ok(payments);
        }

        [HttpPut("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] PaymentStatusUpdate request)
        {
            var result = await _paymentService.UpdatePaymentStatusAsync(request.PaymentId, request.Status, request.TransactionId);
            if (!result) return NotFound();
            return Ok("Payment status updated successfully");
        }

        [HttpGet("receipt/{paymentId}")]
        public async Task<IActionResult> DownloadReceipt(Guid paymentId)
        {
            var pdf = await _paymentService.GenerateReceiptPdfAsync(paymentId);
            return File(pdf, "application/pdf", $"receipt_{paymentId}.pdf");
        }
    }

    public class PaymentStatusUpdate
    {
        public Guid PaymentId { get; set; }
        public PaymentStatus Status { get; set; }
        public string? TransactionId { get; set; }
    }
}
