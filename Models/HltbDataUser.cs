namespace HowLongToBeat.Models
{
    public class HltbDataUser
    {
        public long UserMainStory { get; set; } = 0;
        public long UserMaintExtra { get; set; } = 0;
        public long UserCompletionist { get; set; } = 0;
        public HltbData GameHltbData { get; set; }
    }
}
