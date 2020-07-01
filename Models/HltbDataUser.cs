namespace HowLongToBeat.Models
{
    public class HltbDataUser
    {
        public long UserMainStory { get; set; } = 0;
        public long UserMainExtra { get; set; } = 0;
        public long UserCompletionist { get; set; } = 0;
        public long UserSolo { get; set; } = 0;
        public long UserCoOp { get; set; } = 0;
        public long UserVs { get; set; } = 0;
        public HltbData GameHltbData { get; set; }
    }
}
