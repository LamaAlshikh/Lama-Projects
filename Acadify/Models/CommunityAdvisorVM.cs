namespace Acadify.Models
{
    public class CommunityAdvisorVM
    {
        public List<CommunityMessageVM> Messages { get; set; } = new();
        public List<CommunityMemberVM> Members { get; set; } = new();
    }
}
