namespace MaemoCompliance.Application.Consultants.Dtos;

public class ConsultantDashboardSummaryDto
{
    public int TotalClients { get; set; }
    public int TotalOpenNcrs { get; set; }
    public int TotalHighSeverityNcrs { get; set; }
    public int TotalHighRisks { get; set; }
    public int UpcomingDocumentReviews { get; set; }
}

