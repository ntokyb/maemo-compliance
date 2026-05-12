namespace MaemoCompliance.Application.Dashboard;

public class DashboardSummaryDto
{
    public int TotalDocuments { get; set; }
    public int ActiveDocuments { get; set; }
    public int TotalNcrs { get; set; }
    public int OpenNcrs { get; set; }
    public int OverdueNcrs { get; set; }
    
    // Phase 2: Risk metrics
    public int TotalRisks { get; set; }
    public int HighRisks { get; set; }
    public int MediumRisks { get; set; }
    public int LowRisks { get; set; }
}

