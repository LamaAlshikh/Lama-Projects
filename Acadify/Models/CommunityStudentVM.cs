namespace Acadify.Models
{
    public class CommunityStudentVM
    {
        public List<CommunityMessageVM> Messages { get; set; } = new();
        public List<CommunityMemberVM> Members { get; set; } = new();
    }
}
