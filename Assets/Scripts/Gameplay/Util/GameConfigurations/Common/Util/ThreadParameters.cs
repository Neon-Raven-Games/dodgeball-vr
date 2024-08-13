using System.Collections.Generic;


    public enum VoteStatus
    {
        None,
        UpVote,
        DownVote
    }

    public class VotableItem
    {
        public string Name { get; set; } // file/config name
        public VoteStatus UserStatus { get; set; }
        public string Category { get; set; } // sound/throw
        public string Type { get; set; }  // Sound.Hit, Sound.Miss, ThrowConfig
        public string Data { get; set; } // added a payload
        public VoteInfo VoteData { get; set; } = new(); // upvoters and downvoters
    }

    public class VoteInfo
    {
        public HashSet<string> Upvoters { get; set; } = new();
        public HashSet<string> Downvoters { get; set; } = new();
    }
