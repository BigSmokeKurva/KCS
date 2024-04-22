using System.Text.Json.Serialization;

namespace KCS.Server.Follow
{
    public struct GetMe
    {
        [JsonPropertyName("subscription")] public object Subscription { get; set; }

        [JsonPropertyName("is_super_admin")] public bool IsSuperAdmin { get; set; }

        [JsonPropertyName("is_following")] public bool IsFollowing { get; set; }

        [JsonPropertyName("following_since")] public object FollowingSince { get; set; }

        [JsonPropertyName("is_broadcaster")] public bool IsBroadcaster { get; set; }

        [JsonPropertyName("is_moderator")] public bool IsModerator { get; set; }

        [JsonPropertyName("banned")] public object Banned { get; set; }

        [JsonPropertyName("has_notifications")]
        public bool HasNotifications { get; set; }
    }
}