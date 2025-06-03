using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Web;
using ECommercePlatform.Data;

namespace ECommercePlatform.Controllers
{
    [Route("Payment")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpPost("Notify")]
        public IActionResult Notify([FromForm] string TradeInfo)
        {
            var helper = new Helpers.NewebPayHelper();
            // 解密 TradeInfo
            var json = DecryptAES(TradeInfo, helper.HashKey, helper.HashIV);
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            var result = data.ContainsKey("Status") ? data["Status"].ToString() : null;
            var merchantOrderNo = data.ContainsKey("Result") && data["Result"] is JsonElement element &&
                                  element.TryGetProperty("MerchantOrderNo", out var orderNoElem)
                                  ? orderNoElem.GetString()
                                  : null;
            if (result == "SUCCESS" && int.TryParse(merchantOrderNo, out int orderId))
            {
                var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
                if (order != null)
                {
                    order.PaymentVerified = true;
                    _context.SaveChanges();
                }
            }
            return Content("OK");
        }
        private static string DecryptAES(string cipherHex, string key, string iv)
        {
            var aes = Aes.Create();
            var decryptor = aes.CreateDecryptor();
            var cipherBytes = Enumerable.Range(0, cipherHex.Length / 2)
                    .Select(x => Convert.ToByte(cipherHex.Substring(x * 2, 2), 16)).ToArray();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(iv);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}