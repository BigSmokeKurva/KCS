namespace KCS.Server.Follow
{
    public class PersonalSectionsJson
    {
        public class Title
        {
            public string localizedFallback { get; set; }
            public List<LocalizedToken> localizedTokens { get; set; }
            public string __typename { get; set; }
        }

        public class User
        {
            public string id { get; set; }
            public string login { get; set; }
            public string displayName { get; set; }
            public string profileImageURL { get; set; }
            public string primaryColorHex { get; set; }
            public BroadcastSettings broadcastSettings { get; set; }
            public string __typename { get; set; }
        }
        public class Broadcaster
        {
            public string id { get; set; }
            public BroadcastSettings broadcastSettings { get; set; }
            public string __typename { get; set; }
        }

        public class BroadcastSettings
        {
            public string id { get; set; }
            public string title { get; set; }
            public string __typename { get; set; }
        }

        public class Content
        {
            public string id { get; set; }
            public string previewImageURL { get; set; }
            public Broadcaster broadcaster { get; set; }
            public int viewersCount { get; set; }
            public Game game { get; set; }
            public string type { get; set; }
            public string __typename { get; set; }
        }

        public class Data
        {
            public List<PersonalSection> personalSections { get; set; }
        }

        public class Extensions
        {
            public int durationMilliseconds { get; set; }
            public string operationName { get; set; }
            public string requestID { get; set; }
        }

        public class Game
        {
            public string id { get; set; }
            public string slug { get; set; }
            public string displayName { get; set; }
            public string name { get; set; }
            public string __typename { get; set; }
        }

        public class Item
        {
            public string trackingID { get; set; }
            public string promotionsCampaignID { get; set; }
            public User user { get; set; }
            public string label { get; set; }
            public Content content { get; set; }
            public string __typename { get; set; }
        }

        public class LocalizedToken
        {
            public string value { get; set; }
            public string __typename { get; set; }
        }

        public class PersonalSection
        {
            public string type { get; set; }
            public Title title { get; set; }
            public List<Item> items { get; set; }
            public string __typename { get; set; }
        }

        public Data data { get; set; }
        public Extensions extensions { get; set; }
    }

}
