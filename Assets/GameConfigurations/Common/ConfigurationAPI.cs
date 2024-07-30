using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TMPro;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

public static class ConfigurationAPI 
{
   private static HttpClient _client = new();
   private static string _baseUrl = "https://api.example.com";
   public static TextMeshProUGUI connectionStatus;
   
   // gets the throw config suggestions from the api
   public static async Task<List<ThrowConfiguration>> GetThrowConfigurations()
   {
       try
       {
           var response = await _client.GetAsync($"{_baseUrl}/throw-configurations");
           if (response.IsSuccessStatusCode)
           {
               var content = await response.Content.ReadAsStringAsync();
               return JsonUtility.FromJson<List<ThrowConfiguration>>(content);
           }
           else
           {
               Debug.LogError("Failed to get throw configurations");
               return null;
           }
       }
       catch
       {
           connectionStatus.text = "Failed to connect to server";
           return null;
       }
   }
   public static async Task ShipUpvote(string throwConfigIndex, string json)
   {
       try
       {
           // can we post to the api to upvote a throw config?
           var content = new StringContent(json);
           content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
           var response = await _client.PostAsync($"{_baseUrl}/upvote/{throwConfigIndex}", content);
           
           if (response.IsSuccessStatusCode)
           {
               Debug.Log("Successfully upvoted post");
           }
           else
           {
               Debug.LogError("Failed to upvote post");
           }
       }
       catch
       {
           connectionStatus.text = "Failed to connect to server";
       }
   }
   
    public static async Task ShipDownvote(string throwConfigIndex, string json)
    {
        try
        {
            var content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = await _client.PostAsync($"{_baseUrl}/downvote/{throwConfigIndex}", content);
            if (response.IsSuccessStatusCode)
            {
                Debug.Log("Successfully downvoted post");
            }
            else
            {
                Debug.LogError("Failed to downvote post");
            }
        }
        catch
        {
            connectionStatus.text = "Failed to connect to server";
        }
    }
    
    public static async Task ShipThrowConfiguration(ThrowConfiguration throwConfig)
    {
        try
        {
            var json = JsonUtility.ToJson(throwConfig);
            var content = new StringContent(json);
            var response = await _client.PostAsync($"{_baseUrl}/throw-configurations", content);
            if (response.IsSuccessStatusCode)
            {
                Debug.Log("Successfully shipped throw configuration");
            }
            else
            {
                Debug.LogError("Failed to ship throw configuration");
            }
        }
        catch
        {
            
        }
    }
}
