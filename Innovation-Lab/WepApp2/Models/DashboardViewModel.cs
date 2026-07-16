namespace WepApp2.Models;

    public class DashboardViewModel
    {
        public List<ServiceUsageDto> ServiceUsages { get; set; }
        public List<TimePeakDto> TimePeaks { get; set; }
    public List<TimePeakDto> MaintenancePeaks { get; set; } = new();

    public List<UserPreferenceDto> UserPreferences { get; set; }
        public List<DeviceAvailabilityDto> DeviceAvailabilities { get; set; }
        public MaintenanceSummaryDto MaintenanceSummary { get; set; }
    }

    public class ServiceUsageDto
    {
        public string ServiceName { get; set; }
        public int UsageCount { get; set; }
    }

    public class TimePeakDto
    {
        public string TimePeriod { get; set; }
        public int UsageCount { get; set; }
    }

    public class UserPreferenceDto
    {
        public string DeviceName { get; set; }
        public int PreferencePercentage { get; set; }
    }

    public class DeviceAvailabilityDto
    {
        public string DeviceName { get; set; }
        public string Status { get; set; } // Available / In use / Maintenance etc.
    }

    public class MaintenanceSummaryDto
    {
        public int SuccessRate { get; set; }
        public double AverageRepairTimeDays { get; set; }
        public int CompletedRequests { get; set; }
    }

