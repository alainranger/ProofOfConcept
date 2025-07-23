using DateTimeUtcNowTestable;
using DateTimeUtcNowTestable.Services;

var reportService = new ReportService();
var report = reportService.CreateReport("Monthly Report", "This is the content of the monthly report.");

Console.WriteLine($"Report created with title: {report.Title} at {report.CreatedAtUtc}.");