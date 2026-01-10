namespace SmartDonationSystem.Models
{
    public class SidebarConfig
    {
        public string Role { get; set; } = string.Empty;
        public string HeaderIcon { get; set; } = string.Empty;
        public string HeaderTitle { get; set; } = string.Empty;
        public List<SidebarSection>? Sections { get; set; }
        public List<SidebarItem>? Items { get; set; }
        public List<ActivePathHandler>? ActivePathHandlers { get; set; }
    }

    public class SidebarSection
    {
        public string? Title { get; set; }
        public List<SidebarItem> Items { get; set; } = new();
    }

    public class SidebarItem
    {
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public bool HasBadge { get; set; }
        public string? BadgeText { get; set; }
        public string? BadgeClass { get; set; }
        public string? BadgeId { get; set; }
        public bool HasContentWrapper { get; set; }
    }

    public class ActivePathHandler
    {
        public string Name { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string NavId { get; set; } = string.Empty;
    }
}
