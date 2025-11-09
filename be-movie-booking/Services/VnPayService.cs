using be_movie_booking.Helpers;
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

        // Use VnPayLibrary to build payment URL
        var vnpay = new VnPayLibrary();
        
        // Add all required VNPay parameters
        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", _tmnCode);
        vnpay.AddRequestData("vnp_Amount", vnp_Amount.ToString());
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_TxnRef", orderId);
        vnpay.AddRequestData("vnp_OrderInfo", orderDescription);
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnpay.AddRequestData("vnp_IpAddr", ipAddress);
        vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

        // Generate payment URL with secure hash
        return vnpay.CreateRequestUrl(_paymentUrl, _hashSecret);
    }

    public bool ValidatePaymentResponse(IQueryCollection query)
    {
        // Use VnPayLibrary to validate signature
        var vnpay = new VnPayLibrary();
        
        // Add all response parameters from VNPay callback
        foreach (var kvp in query)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                vnpay.AddResponseData(kvp.Key, kvp.Value.ToString());
            }
        }

        // Validate signature using VnPayLibrary
        return vnpay.ValidateSignature(_hashSecret);
    }

    public Dictionary<string, string> GetResponseData(IQueryCollection query)
    {
        // Use VnPayLibrary to extract response data
        var vnpay = new VnPayLibrary();
        
        // Add all response parameters from VNPay callback
        foreach (var kvp in query)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                vnpay.AddResponseData(kvp.Key, kvp.Value.ToString());
            }
        }

        // Return all response data as dictionary
        return vnpay.GetAllResponseData();
    }
}
