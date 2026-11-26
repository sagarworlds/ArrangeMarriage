using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using ArrangeMarriage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>Payment endpoints: ownership rules, staff-only status changes, receipts and the signed webhook.</summary>
    public class PaymentApiTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public PaymentApiTests(ApiFactory factory) => _factory = factory;

        private Task<Guid> SeedPaymentAsync(Guid userId, PaymentStatus status = PaymentStatus.Pending) =>
            _factory.WithDbAsync(async db =>
            {
                var payment = new Payment { UserId = userId, Amount = 2500m, Type = PaymentType.UPI, Status = status, FeeType = "Registration" };
                db.Payments.Add(payment);
                await db.SaveChangesAsync();
                return payment.PaymentId;
            });

        private Task<Payment> GetPaymentAsync(Guid paymentId) =>
            _factory.WithDbAsync(db => db.Payments.AsNoTracking().SingleAsync(p => p.PaymentId == paymentId));

        private static string Sign(string body, string secret) =>
            Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        private static HttpRequestMessage WebhookRequest(object payload, string? signature)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/payment/webhook")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            if (signature != null) request.Headers.Add("X-Signature", signature);
            return request;
        }

        private static string BodyOf(object payload) => System.Text.Json.JsonSerializer.Serialize(payload);

        // ---- creating payments ------------------------------------------------------------------------------

        [Fact]
        public async Task ProcessPayment_ByTheOwner_AlwaysStartsAsPending()
        {
            var member = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(member);

            // A client must not be able to declare its own payment as paid.
            var response = await client.PostAsJsonAsync("/api/payment/process", new
            {
                userId = member.UserId,
                amount = 500m,
                type = PaymentType.UPI,
                status = PaymentStatus.Paid,
                transactionId = "forged-txn"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var created = (await response.Content.ReadFromJsonAsync<Payment>())!;
            var stored = await GetPaymentAsync(created.PaymentId);
            Assert.Equal(PaymentStatus.Pending, stored.Status);
            Assert.Null(stored.TransactionId);
            Assert.Null(stored.PaymentDate);
        }

        [Fact]
        public async Task ProcessPayment_ForAnotherUser_IsForbidden()
        {
            var member = await _factory.CreateUserAsync();
            var other = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(member);

            var response = await client.PostAsJsonAsync("/api/payment/process", new { userId = other.UserId, amount = 500m, type = PaymentType.Cash });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ProcessPayment_ByStaff_CanRecordAnOfflinePaymentAsPaid()
        {
            var member = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Staff);
            using var client = _factory.ClientFor(staff);

            var response = await client.PostAsJsonAsync("/api/payment/process",
                new { userId = member.UserId, amount = 1000m, type = PaymentType.Cash, status = PaymentStatus.Paid });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var created = (await response.Content.ReadFromJsonAsync<Payment>())!;
            Assert.Equal(PaymentStatus.Paid, (await GetPaymentAsync(created.PaymentId)).Status);
        }

        // ---- reading / status ----------------------------------------------------------------------------------

        [Fact]
        public async Task GetUserPayments_OnlyForTheOwnerOrStaff()
        {
            var owner = await _factory.CreateUserAsync();
            var stranger = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Admin);
            await SeedPaymentAsync(owner.UserId);

            using var asOwner = _factory.ClientFor(owner);
            using var asStranger = _factory.ClientFor(stranger);
            using var asStaff = _factory.ClientFor(staff);

            var ownerResponse = await asOwner.GetAsync($"/api/payment/user/{owner.UserId}");
            Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
            Assert.Single((await ownerResponse.Content.ReadFromJsonAsync<List<Payment>>())!);
            Assert.Equal(HttpStatusCode.Forbidden, (await asStranger.GetAsync($"/api/payment/user/{owner.UserId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await asStaff.GetAsync($"/api/payment/user/{owner.UserId}")).StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_RequiresStaff()
        {
            var owner = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Staff);
            var paymentId = await SeedPaymentAsync(owner.UserId);
            var update = new { paymentId, status = PaymentStatus.Paid, transactionId = "txn-1" };

            using var asOwner = _factory.ClientFor(owner);
            using var asStaff = _factory.ClientFor(staff);

            Assert.Equal(HttpStatusCode.Forbidden, (await asOwner.PutAsJsonAsync("/api/payment/status", update)).StatusCode);
            Assert.Equal(PaymentStatus.Pending, (await GetPaymentAsync(paymentId)).Status);

            Assert.Equal(HttpStatusCode.OK, (await asStaff.PutAsJsonAsync("/api/payment/status", update)).StatusCode);
            Assert.Equal(PaymentStatus.Paid, (await GetPaymentAsync(paymentId)).Status);
        }

        // ---- receipts ------------------------------------------------------------------------------------------

        [Fact]
        public async Task Receipt_IsAPdfForTheOwnerAndStaff_AndNotFoundForOthers()
        {
            var owner = await _factory.CreateUserAsync();
            var stranger = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Staff);
            var paymentId = await SeedPaymentAsync(owner.UserId, PaymentStatus.Paid);

            using var asOwner = _factory.ClientFor(owner);
            using var asStranger = _factory.ClientFor(stranger);
            using var asStaff = _factory.ClientFor(staff);

            var ownerResponse = await asOwner.GetAsync($"/api/payment/receipt/{paymentId}");
            Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
            Assert.Equal("application/pdf", ownerResponse.Content.Headers.ContentType?.MediaType);
            Assert.StartsWith("%PDF-", Encoding.ASCII.GetString((await ownerResponse.Content.ReadAsByteArrayAsync()).AsSpan(0, 5)));

            // 404 rather than 403 so receipts of other people's payments can't be probed for existence.
            Assert.Equal(HttpStatusCode.NotFound, (await asStranger.GetAsync($"/api/payment/receipt/{paymentId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await asStaff.GetAsync($"/api/payment/receipt/{paymentId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await asStaff.GetAsync($"/api/payment/receipt/{Guid.NewGuid()}")).StatusCode);
        }

        // ---- webhook -------------------------------------------------------------------------------------------

        [Fact]
        public async Task Webhook_WithValidSignature_MarksThePaymentPaid_WithoutAJwt()
        {
            var owner = await _factory.CreateUserAsync();
            var paymentId = await SeedPaymentAsync(owner.UserId);
            var payload = new { @event = "payment.succeeded", paymentId = paymentId.ToString(), transactionId = "txn_webhook_1" };
            using var client = _factory.ClientFor(null);

            var response = await client.SendAsync(WebhookRequest(payload, Sign(BodyOf(payload), ApiFactory.WebhookSecret)));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var stored = await GetPaymentAsync(paymentId);
            Assert.Equal(PaymentStatus.Paid, stored.Status);
            Assert.Equal("txn_webhook_1", stored.TransactionId);
            Assert.NotNull(stored.PaymentDate);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-hex")]
        [InlineData("deadbeef")]
        public async Task Webhook_WithMissingOrWrongSignature_IsRejected_AndChangesNothing(string? signature)
        {
            var owner = await _factory.CreateUserAsync();
            var paymentId = await SeedPaymentAsync(owner.UserId);
            var payload = new { @event = "payment.succeeded", paymentId = paymentId.ToString(), transactionId = "forged" };
            using var client = _factory.ClientFor(null);

            var response = await client.SendAsync(WebhookRequest(payload, signature));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(PaymentStatus.Pending, (await GetPaymentAsync(paymentId)).Status);
        }

        [Fact]
        public async Task Webhook_SignedWithTheWrongSecret_IsRejected()
        {
            var owner = await _factory.CreateUserAsync();
            var paymentId = await SeedPaymentAsync(owner.UserId);
            var payload = new { @event = "payment.succeeded", paymentId = paymentId.ToString(), transactionId = "x" };
            using var client = _factory.ClientFor(null);

            var response = await client.SendAsync(WebhookRequest(payload, Sign(BodyOf(payload), "some-other-secret")));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Webhook_SignatureForADifferentBody_IsRejected()
        {
            var owner = await _factory.CreateUserAsync();
            var paymentId = await SeedPaymentAsync(owner.UserId);
            var signedPayload = new { @event = "payment.pending", paymentId = paymentId.ToString(), transactionId = "x" };
            var sentPayload = new { @event = "payment.succeeded", paymentId = paymentId.ToString(), transactionId = "x" };
            using var client = _factory.ClientFor(null);

            // Replaying a valid signature on a tampered body must not work.
            var response = await client.SendAsync(WebhookRequest(sentPayload, Sign(BodyOf(signedPayload), ApiFactory.WebhookSecret)));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(PaymentStatus.Pending, (await GetPaymentAsync(paymentId)).Status);
        }

        [Fact]
        public async Task Webhook_MalformedPaymentId_Returns400_NotAServerError()
        {
            var payload = new { @event = "payment.succeeded", paymentId = "not-a-guid", transactionId = "x" };
            using var client = _factory.ClientFor(null);

            var response = await client.SendAsync(WebhookRequest(payload, Sign(BodyOf(payload), ApiFactory.WebhookSecret)));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Webhook_MalformedJson_Returns400()
        {
            const string body = "{ this is not json";
            using var client = _factory.ClientFor(null);
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/payment/webhook")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Signature", Sign(body, ApiFactory.WebhookSecret));

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Webhook_UnknownPayment_Returns404_AndOtherEventsAreAcknowledgedWithoutChange()
        {
            var owner = await _factory.CreateUserAsync();
            var paymentId = await SeedPaymentAsync(owner.UserId);
            using var client = _factory.ClientFor(null);

            var unknown = new { @event = "payment.succeeded", paymentId = Guid.NewGuid().ToString(), transactionId = "x" };
            Assert.Equal(HttpStatusCode.NotFound,
                (await client.SendAsync(WebhookRequest(unknown, Sign(BodyOf(unknown), ApiFactory.WebhookSecret)))).StatusCode);

            var other = new { @event = "payment.failed", paymentId = paymentId.ToString(), transactionId = "x" };
            Assert.Equal(HttpStatusCode.OK,
                (await client.SendAsync(WebhookRequest(other, Sign(BodyOf(other), ApiFactory.WebhookSecret)))).StatusCode);
            Assert.Equal(PaymentStatus.Pending, (await GetPaymentAsync(paymentId)).Status);
        }
    }
}
