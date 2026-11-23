using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using ArrangeMarriage.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] Payment payment)
        {
            if (!User.CanAccessUser(payment.UserId)) return Forbid();

            // Clients can only create pending payments; "Paid" is set by the verified webhook or staff.
            if (!User.IsStaff())
            {
                payment.Status = PaymentStatus.Pending;
                payment.TransactionId = null;
                payment.PaymentDate = null;
            }

            var result = await _paymentService.ProcessPaymentAsync(payment);
            return Ok(result);
        }

        [HttpPost("create-intent")]
        public IActionResult CreateIntent([FromBody] PaymentIntentRequest request)
        {
            var intentId = "pi_" + Guid.NewGuid().ToString("N").Substring(0, 16);
            return Ok(new 
            { 
                ClientSecret = intentId + "_secret_12345",
                Amount = request.Amount,
                Status = "RequiresPaymentMethod"
            });
        }

        /// <summary>Payment-provider callback. Authenticated by an HMAC-SHA256 signature of the raw body, not a JWT.</summary>
        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var secret = _configuration["Payments:WebhookSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("Payments:WebhookSecret is not configured; rejecting webhook.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Webhook not configured");
            }

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            if (!IsValidSignature(body, Request.Headers["X-Signature"].ToString(), secret))
            {
                _logger.LogWarning("Rejected payment webhook with invalid signature.");
                return Unauthorized("Invalid signature");
            }

            WebhookPayload? payload;
            try
            {
                payload = System.Text.Json.JsonSerializer.Deserialize<WebhookPayload>(body,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Malformed webhook JSON.");
                return BadRequest("Invalid webhook payload");
            }

            if (payload == null || string.IsNullOrEmpty(payload.Event) || !Guid.TryParse(payload.PaymentId, out var paymentId))
            {
                return BadRequest("Invalid webhook payload");
            }

            if (payload.Event == "payment.succeeded")
            {
                var result = await _paymentService.UpdatePaymentStatusAsync(paymentId, PaymentStatus.Paid, payload.TransactionId);
                if (!result) return NotFound("Payment record not found");
            }

            return Ok(new { Received = true });
        }

        private static bool IsValidSignature(string body, string signatureHex, string secret)
        {
            var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body));
            try
            {
                return CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(signatureHex));
            }
            catch (FormatException)
            {
                return false;
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPayments(Guid userId)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            var payments = await _paymentService.GetUserPaymentsAsync(userId);
            return Ok(payments);
        }

        [Authorize(Roles = "Admin,Staff")]
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
            if (!User.IsStaff())
            {
                var mine = await _paymentService.GetUserPaymentsAsync(User.GetUserId() ?? Guid.Empty);
                if (!mine.Any(p => p.PaymentId == paymentId)) return NotFound();
            }

            byte[] pdf;
            try
            {
                pdf = await _paymentService.GenerateReceiptPdfAsync(paymentId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return File(pdf, "application/pdf", $"receipt_{paymentId}.pdf");
        }
    }

    public class PaymentStatusUpdate
    {
        public Guid PaymentId { get; set; }
        public PaymentStatus Status { get; set; }
        public string? TransactionId { get; set; }
    }

    public class PaymentIntentRequest
    {
        public decimal Amount { get; set; }
    }

    public class WebhookPayload
    {
        public string Event { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
    }
}
