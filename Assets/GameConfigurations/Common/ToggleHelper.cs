using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleHelper : MonoBehaviour
{
    private int _index;
    public Toggle toggle;
    [SerializeField] private TextMeshProUGUI textLabel;
    [SerializeField] private Button upvoteButton;
    [SerializeField] private Button downvoteButton;
    [SerializeField] private bool isSound;
    private SoundIndex soundIndex;
    
    public void SetSoundIndex(SoundIndex index)
    {
        isSound = true;
        soundIndex = index;
    }
    private void Start()
    {
        upvoteButton.onClick.AddListener(ShipUpDoot);
        downvoteButton.onClick.AddListener(ShipDownVote);
    }
    public void SetText(string text) => 
        textLabel.text = text;

    public void SetIndex(int index)
    {
        _index = index;
    }

    public void ShipUpDoot()
    {
        Debug.Log("Upvoting post");
        upvoteButton.interactable = false;
        downvoteButton.interactable = true;
        if (isSound) ConfigurationAPI.ShipUpvote(soundIndex.ToString(), _index.ToString());
        else ConfigurationAPI.ShipUpvote(_index.ToString(), ConfigurationManager.GetThrowConfiguration(_index).ToJson());
    }

    public void ShipDownVote()
    {
        upvoteButton.interactable = true;
        downvoteButton.interactable = false;
        Debug.Log("Downvoting post");
        if (isSound) ConfigurationAPI.ShipDownvote(soundIndex.ToString(), _index.ToString());
        else ConfigurationAPI.ShipDownvote(_index.ToString(), ConfigurationManager.GetThrowConfiguration(_index).ToJson());
    }
}
