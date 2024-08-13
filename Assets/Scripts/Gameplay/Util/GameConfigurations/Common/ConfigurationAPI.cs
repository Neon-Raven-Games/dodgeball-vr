using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;


public static class ConfigurationAPI
{
    private static readonly HttpClient _client;
    private static string guid;
    public static string Guid => guid;

    private static readonly string _baseUrl = "https://api.neonraven.org:5002/api/votableitems";
    public static TextMeshProUGUI connectionStatus;

    private static bool connected = true;

    private static readonly HttpClientHandler _SHandler = new()
    {
        ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
        {
            // implement auth from guid, server side encryption needed b4 save to db
            return true;
        }
    };


    public static async Task<VotableItem> GetVotableItem(string type, string category, string item)
    {
        if (!connected) return null;
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/{type}/{category}/{item}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userVotableCallback = JsonConvert.DeserializeObject<VotableItem>(content);
                return userVotableCallback;
            }
            else
            {
                var newItem = new VotableItem()
                {
                    Category = category,
                    Name = item,
                    Data = "",
                    UserStatus = VoteStatus.None,
                    Type = type,
                    VoteData = new VoteInfo()
                };
                await ShipItem(newItem);
                return newItem;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching item: {e.Message}");
            return null;
        }
    }

    static ConfigurationAPI()
    {
        _client = new HttpClient(_SHandler);
        guid = PlayerPrefs.GetString("idx", "");
        if (guid == "")
        {
            guid = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("idx", guid);
        }
    }

    public static void GetItemThreaded(string type, string category, string item, Action<VotableItem> callback)
    {
        if (!connected) return;
        Task.Run(async () =>
        {
            var result = await GetVotableItem(type, category, item);
            if (result != null) callback(result);
            else
            {
                Debug.Log("disconnecting from server");
                connected = false;
            }
        });
    }


    public static async Task<List<VotableItem>> GetItems(string type)
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/{type}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonUtility.FromJson<List<VotableItem>>(content);
            }

            Debug.LogError($"Failed to get items: {response.ReasonPhrase}");
            return null;
        }
        catch (HttpRequestException e)
        {
            Debug.LogError($"Request error: {e.Message}");
            connectionStatus.text = "Failed to connect to server";
            return null;
        }
    }

    public static async Task ShipVote(string type, string category, string item, bool isUpvote)
    {
        try
        {
            var content = new StringContent($"{{ \"guid\": \"{guid}\" }}", System.Text.Encoding.UTF8,
                "application/json");
            var endpoint = isUpvote ? "upvote" : "downvote";
            var response = await _client.PostAsync($"{_baseUrl}/{endpoint}/{type}/{category}/{item}", content);
            Debug.Log($"Posting to {_baseUrl}/{endpoint}/{type}/{category}/{item}");
            if (response.IsSuccessStatusCode)
            {
                Debug.Log($"Successfully {endpoint}d post");
            }
            else
            {
                Debug.LogError($"Failed to {endpoint} post: {response.ReasonPhrase}");
            }
        }
        catch (HttpRequestException e)
        {
            Debug.LogError($"Request error: {e.Message}");
            connectionStatus.text = "Failed to connect to server";
        }
    }

    public static void ShipNewData()
    {
        var item = new VotableItem()
        {
            Category = "ThrowConfig" + DateTime.Now,
            Name = DateTime.Now + Guid,
            Data = ConfigurationManager.GetThrowConfiguration().ToJson(),
            UserStatus = VoteStatus.None,
            Type = "User",
            VoteData = new VoteInfo()
        };

#pragma warning disable CS4014
        ShipItem(item);
#pragma warning restore CS4014
    }

    public static async Task ShipItem(VotableItem item)
    {
        try
        {
            var json = JsonConvert.SerializeObject(item);
            var content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = await _client.PostAsync(_baseUrl, content);
            if (response.IsSuccessStatusCode)
            {
                Debug.Log("Successfully shipped item");
            }
            else
            {
                Debug.LogError($"Failed to ship item: {response.ReasonPhrase}");
            }
        }
        catch (HttpRequestException e)
        {
            Debug.LogError($"Request error: {e.Message}");
            connectionStatus.text = "Failed to connect to server";
        }
    }
}