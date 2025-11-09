using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace be_movie_booking.Helpers;

/// <summary>
/// VNPay Library helper class for generating payment URLs and validating responses
/// </summary>
public class VnPayLibrary
{
    private readonly SortedDictionary<string, string> _requestData = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, string> _responseData = new(StringComparer.Ordinal);

    /// <summary>
    /// Add request data for payment URL generation
    /// </summary>
    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _requestData[key] = value;
        }
    }

    /// <summary>
    /// Add response data from VNPay callback
    /// </summary>
    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _responseData[key] = value;
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
    /// Get all response data as dictionary
    /// </summary>
    public Dictionary<string, string> GetAllResponseData()
    {
        return _responseData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Create payment URL with secure hash using HMAC-SHA512 (VNPay standard)
    /// VNPay pattern: HMAC-SHA512(hashSecret, queryString)
    /// Parameters must be sorted alphabetically and URL-encoded
    /// </summary>
    public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
    {
        // Build query string with URL-encoded parameters (sorted alphabetically by SortedDictionary)
        var queryParts = new List<string>();
        foreach (var kvp in _requestData.Where(kvp => !string.IsNullOrEmpty(kvp.Value)))
        {
            var encodedKey = WebUtility.UrlEncode(kvp.Key);
            var encodedValue = WebUtility.UrlEncode(kvp.Value);
            queryParts.Add($"{encodedKey}={encodedValue}");
        }

        // Join with & to create query string
        var queryString = string.Join("&", queryParts);

        // Calculate hash: HMAC-SHA512(hashSecret, queryString)
        var vnp_SecureHash = ComputeHmacSha512(vnp_HashSecret, queryString);

        // Build final URL with hash appended
        return $"{baseUrl}?{queryString}&vnp_SecureHash={vnp_SecureHash}";
    }

    /// <summary>
    /// Validate response signature from VNPay using HMAC-SHA512
    /// </summary>
    public bool ValidateSignature(string vnp_HashSecret)
    {
        var vnp_SecureHash = GetResponseData("vnp_SecureHash");
        if (string.IsNullOrEmpty(vnp_SecureHash))
        {
            return false;
        }

        // Build query string excluding vnp_SecureHash and vnp_SecureHashType (sorted alphabetically)
        var queryParts = new List<string>();
        foreach (var kvp in _responseData.Where(kvp => 
            !string.IsNullOrEmpty(kvp.Value) && 
            kvp.Key != "vnp_SecureHash" && 
            kvp.Key != "vnp_SecureHashType"))
        {
            var encodedKey = WebUtility.UrlEncode(kvp.Key);
            var encodedValue = WebUtility.UrlEncode(kvp.Value);
            queryParts.Add($"{encodedKey}={encodedValue}");
        }

        // Join with & to create query string
        var queryString = string.Join("&", queryParts);

        // Calculate hash: HMAC-SHA512(hashSecret, queryString)
        var checkSum = ComputeHmacSha512(vnp_HashSecret, queryString);

        return checkSum.Equals(vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Computes HMAC-SHA512 hash
    /// VNPay uses: HMAC-SHA512(hashSecret, queryString)
    /// </summary>
    private static string ComputeHmacSha512(string key, string inputData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        
        using var hmac = new HMACSHA512(keyBytes);
        var hashValue = hmac.ComputeHash(inputBytes);
        
        var hash = new StringBuilder();
        foreach (var theByte in hashValue)
        {
            hash.Append(theByte.ToString("x2"));
        }
        
        return hash.ToString();
    }
}

