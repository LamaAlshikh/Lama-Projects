namespace WepApp2.Models
{
    // 🧠 في مجلد Models
    public class TechnologyPageViewModel
    {
        public List<Technology> Technologies { get; set; } = new();
        public Technology Technology { get; set; } = new();
        public bool IsEdit { get; set; } = false;
    }

}
