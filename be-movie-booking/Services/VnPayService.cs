using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace be_movie_booking.Services;

public interface IVnPayService
{
    /// <summary>
    /// Creates a VNPay payment URL with proper signature
    /// </summary>
    /// <param name="orderId">Unique order ID (usually payment ID)</param>
    /// <param name="amount">Amount in major currency units (e.g., 100000 for 100,000 VND)</param>
    /// <param name="orderDescription">Order description</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="returnUrl">Return URL after payment</param>
    /// <returns>VNPay payment URL</returns>
    string CreatePaymentUrl(string orderId, double amount, string orderDescription, string ipAddress, string returnUrl);

    /// <summary>
    /// Validates VNPay payment response signature
    /// </summary>
    /// <param name="query">Query string collection from VNPay callback</param>
    /// <returns>True if signature is valid</returns>
    bool ValidatePaymentResponse(IQueryCollection query);

    /// <summary>
    /// Extracts response data from VNPay callback query string
    /// </summary>
    /// <param name="query">Query string collection from VNPay callback</param>
    /// <returns>Dictionary of response parameters</returns>
    Dictionary<string, string> GetResponseData(IQueryCollection query);
}

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _configuration;
    private readonly string _tmnCode;
    private readonly string _hashSecret;
    private readonly string _paymentUrl;
    private readonly string _returnUrl;

    public VnPayService(IConfiguration configuration)
    {
        _configuration = configuration;
        _tmnCode = _configuration["VNPay:vnp_TmnCode"] ?? throw new InvalidOperationException("VNPay:TmnCode is not configured");
        _hashSecret = _configuration["VNPay:vnp_HashSecret"] ?? throw new InvalidOperationException("VNPay:HashSecret is not configured");
        _paymentUrl = _configuration["VNPay:vnp_Url"] ?? throw new InvalidOperationException("VNPay:Url is not configured");
        _returnUrl = _configuration["VNPay:vnp_ReturnUrl"] ?? throw new InvalidOperationException("VNPay:ReturnUrl is not configured");
    }

    public string CreatePaymentUrl(string orderId, double amount, string orderDescription, string ipAddress, string returnUrl)
    {
        // VNPay requires amount in VND as an integer
        // The amount parameter is already in VND (e.g., 100000 for 100,000 VND)
        // Convert to long integer
        var vnp_Amount = (long)amount;

        // Build payment data dictionary
        var vnpayData = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", _tmnCode },
            { "vnp_Amount", vnp_Amount.ToString() },
            { "vnp_CurrCode", "VND" },
            { "vnp_TxnRef", orderId },
            { "vnp_OrderInfo", orderDescription },
            { "vnp_OrderType", "other" },
            { "vnp_Locale", "vn" },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_IpAddr", ipAddress },
            { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") }
        };

        // Build query string
        var queryString = new StringBuilder();
        foreach (var kvp in vnpayData)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                queryString.Append(WebUtility.UrlEncode(kvp.Key) + "=" + WebUtility.UrlEncode(kvp.Value) + "&");
            }
        }

        // Remove trailing &
        if (queryString.Length > 0)
        {
            queryString.Length--;
        }

        // Generate secure hash: append secret and compute SHA256
        // VNPay pattern: queryString + "&" + hashSecret, then SHA256
        var signData = queryString.ToString() + "&" + _hashSecret;
        var vnp_SecureHash = ComputeSha256Hash(signData);

        // Build final payment URL
        return $"{_paymentUrl}?{queryString}&vnp_SecureHash={vnp_SecureHash}";
    }

    public bool ValidatePaymentResponse(IQueryCollection query)
    {
        var vnp_SecureHash = query["vnp_SecureHash"].ToString();
        if (string.IsNullOrEmpty(vnp_SecureHash))
        {
            return false;
        }

        // Extract all parameters except vnp_SecureHash and vnp_SecureHashType
        var vnpayData = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kvp in query)
        {
            if (!string.IsNullOrEmpty(kvp.Value) && 
                kvp.Key != "vnp_SecureHash" && 
                kvp.Key != "vnp_SecureHashType")
            {
                vnpayData.Add(kvp.Key, kvp.Value.ToString());
            }
        }

        // Build query string for hash calculation
        var queryString = new StringBuilder();
        foreach (var kvp in vnpayData)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                queryString.Append(WebUtility.UrlEncode(kvp.Key) + "=" + WebUtility.UrlEncode(kvp.Value) + "&");
            }
        }

        // Remove trailing &
        if (queryString.Length > 0)
        {
            queryString.Length--;
        }

        // Generate hash and compare: append secret and compute SHA256
        var signData = queryString.ToString() + "&" + _hashSecret;
        var computedHash = ComputeSha256Hash(signData);

        return computedHash.Equals(vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
    }

    public Dictionary<string, string> GetResponseData(IQueryCollection query)
    {
        var data = new Dictionary<string, string>();
        foreach (var kvp in query)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                data[kvp.Key] = kvp.Value.ToString();
            }
        }
        return data;
    }

    /// <summary>
    /// Computes SHA256 hash of the input string
    /// VNPay uses: SHA256(queryString + "&" + hashSecret)
    /// </summary>
    private static string ComputeSha256Hash(string rawData)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return BitConverter.ToString(bytes).Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
    }
}
