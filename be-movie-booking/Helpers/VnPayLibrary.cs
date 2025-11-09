using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace be_movie_booking.Helpers;

/// <summary>
/// VNPay Library helper class for generating payment URLs and validating responses
/// </summary>
public class VnPayLibrary
{
    private readonly SortedList<string, string> _requestData = new();
    private readonly SortedList<string, string> _responseData = new();

    /// <summary>
    /// Add request data for payment URL generation
    /// </summary>
    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _requestData.Add(key, value);
        }
    }

    /// <summary>
    /// Add response data from VNPay callback
    /// </summary>
    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _responseData.Add(key, value);
        }
    }

    /// <summary>
    /// Get response data by key
    /// </summary>
    public string GetResponseData(string key)
    {
        return _responseData.TryGetValue(key, out var value) ? value : string.Empty;
    }

    /// <summary>
    /// Create payment URL with secure hash
    /// </summary>
    public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
    {
        var data = new StringBuilder();
        foreach (var kvp in _requestData.Where(kvp => !string.IsNullOrEmpty(kvp.Value)))
        {
            data.Append(WebUtility.UrlEncode(kvp.Key) + "=" + WebUtility.UrlEncode(kvp.Value) + "&");
        }

        var queryString = data.ToString();
        var vnp_SecureHash = HmacSHA512(vnp_HashSecret, queryString);
        return baseUrl + "?" + queryString + "vnp_SecureHash=" + vnp_SecureHash;
    }

    /// <summary>
    /// Validate response signature from VNPay
    /// </summary>
    public bool ValidateSignature(string vnp_HashSecret)
    {
        var data = new StringBuilder();
        var vnp_SecureHash = GetResponseData("vnp_SecureHash");
        
        foreach (var kvp in _responseData.Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Key != "vnp_SecureHash"))
        {
            data.Append(WebUtility.UrlEncode(kvp.Key) + "=" + WebUtility.UrlEncode(kvp.Value) + "&");
        }

        var checkSum = HmacSHA512(vnp_HashSecret, data.ToString());
        return checkSum.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Generate HMAC SHA512 hash
    /// </summary>
    private string HmacSHA512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        
        using (var hmac = new HMACSHA512(keyBytes))
        {
            var hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }

        return hash.ToString();
    }
}

