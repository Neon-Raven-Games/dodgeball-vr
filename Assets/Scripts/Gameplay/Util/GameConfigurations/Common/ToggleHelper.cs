using System;
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
    [SerializeField] private TextMeshProUGUI upvoteCount;
    [SerializeField] private TextMeshProUGUI downVoteCount;
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
        var rect = GetComponent<RectTransform>();
        var rectPos = rect.localPosition;
        rectPos.z = 0;
        rect.localPosition = rectPos;
    }

    public void SetText(string text) =>
        textLabel.text = text;

    public void SetIndex(int index)
    {
        _index = index;
    }

    private bool _updateCount;
    public void Update()
    {
        if (_updateCount)
        {
            Debug.Log("updating vote count");
            upvoteCount.text = upvotes.ToString();
            downVoteCount.text = downvotes.ToString();
            _updateCount = false;
        upvoteButton.targetGraphic.color =
            _userStatus == VoteStatus.UpVote ? toggleColor : Color.white;
        downvoteButton.targetGraphic.color =
            _userStatus == VoteStatus.DownVote ? toggleColor : Color.white;
        }
    }

    private Color toggleColor = Color.gray;
    private int upvotes;
    private int downvotes;
    public void InitializeFromDatabase(VotableItem item)
    {
        upvotes = item.VoteData.Upvoters.Count;
        downvotes = item.VoteData.Downvoters.Count;
        if (item.VoteData.Downvoters.Contains(ConfigurationAPI.Guid))
        {
            _userStatus= VoteStatus.DownVote;
        }
        else if (item.VoteData.Upvoters.Contains(ConfigurationAPI.Guid))
        {
            _userStatus = VoteStatus.UpVote;
        }
        else
        {
            _userStatus = VoteStatus.None;
        }
        _updateCount = true;
    }
    private VoteStatus _userStatus;

    // can we make an editor button for the up/downvote functions for testing?
    public void ShipUpDoot()
    {
        if (_userStatus == VoteStatus.DownVote)
        {
            downvotes--;
            upvotes++;
            _userStatus = VoteStatus.UpVote;
        }
        else if (_userStatus == VoteStatus.UpVote)
        {
            upvotes--;
            _userStatus = VoteStatus.None;
        }
        else if (_userStatus == VoteStatus.None)
        {
            upvotes++;
            _userStatus = VoteStatus.UpVote;
        }
        
        _updateCount = true;
        var type = isSound ? "Sound" : "ThrowConfig";
        var id = isSound ? soundIndex.ToString() : "Configurations";
        var index = isSound
            ? ConfigurationManager.GetIndexedSound(soundIndex, _index).name
            : ConfigurationManager.GetThrowConfiguration(_index).name;
#pragma warning disable CS4014
        ConfigurationAPI.ShipVote(type, id, index, true);
#pragma warning restore CS4014
    }

    public void ShipDownVote()
    {
        if (_userStatus == VoteStatus.UpVote)
        {
            upvotes--;
            downvotes++;
            _userStatus = VoteStatus.DownVote;
        }
        else if (_userStatus == VoteStatus.DownVote)
        {
            downvotes--;
            _userStatus = VoteStatus.None;
        }
        else if (_userStatus == VoteStatus.None)
        {
            downvotes++;
            _userStatus = VoteStatus.DownVote;
        }
        _updateCount = true;
        Debug.Log("Downvoting post");
        var type = isSound ? "Sound" : "ThrowConfig";
        var id = isSound ? soundIndex.ToString() : "Configurations";
        var index = isSound
            ? ConfigurationManager.GetIndexedSound(soundIndex, _index).name
            : ConfigurationManager.GetThrowConfiguration(_index).name;
#pragma warning disable CS4014
        ConfigurationAPI.ShipVote(type, id, index, false);
#pragma warning restore CS4014
    }
}