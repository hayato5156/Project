using ECommercePlatform.Services;
using Xunit;

namespace ECommercePlatform.Tests.Services
{
    public class PaymentServiceTests
    {
        [Fact]
        public void ProcessCreditCardPayment_ReturnsTrue_WithValidInfo()
        {
            var service = new PaymentService();
            var result = service.ProcessCreditCardPayment("1234567890123456", "12/30", "123", 100);
            Assert.True(result);
        }

        [Fact]
        public void ProcessCreditCardPayment_ReturnsFalse_WithInvalidInfo()
        {
            var service = new PaymentService();
            var result = service.ProcessCreditCardPayment("", "", "", 0);
            Assert.False(result);
        }
    }
}
