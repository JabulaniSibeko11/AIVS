namespace AIVS.Workflow;

/// <summary>
/// Single source of truth for AIVS workflow status values and user-facing labels.
/// Database values remain compact/stable; UI must use Display().
/// </summary>
public static class AttributeWorkflowStatus
{
    public static class Attribute
    {
        public const string Submitted = "Submitted";
        public const string SectorInbox = "SectorInbox";
        public const string Claimed = "Claimed";
        public const string ValuerReview = "ValuerReview";
        public const string InspectionRequired = "InspectionRequired";
        public const string InspectionConfirmed = "InspectionConfirmed";
        public const string InspectionDetailsSent = "InspectionDetailsSent";
        public const string InspectionCompleted = "InspectionCompleted";
        public const string InspectionExpired = "InspectionExpired";
        public const string ReturnedToClient = "ReturnedToClient";
        public const string Resubmitted = "Resubmitted";
        public const string ReturnedToValuer = "ReturnedToValuer";
        public const string SectorManagerQa = "SectorManagerQa";
        public const string SeniorManagerQa = "SeniorManagerQa";
        public const string Approved = "Approved";
        public const string ReadyForOvvioExtract = "ReadyForOvvioExtract";
        public const string OvvioInserted = "OvvioInserted";
        public const string OvvioExtracted = "OvvioExtracted";
        public const string Rejected = "Rejected";
    }

    public static class Review
    {
        public const string InProgress = "InProgress";
        public const string InspectionRequired = "InspectionRequired";
        public const string InspectionConfirmed = "InspectionConfirmed";
        public const string ReturnedToClient = "ReturnedToClient";
        public const string ReturnedToValuer = "ReturnedToValuer";
        public const string ReturnedToSectorManager = "ReturnedToSectorManager";
        public const string SubmittedForSectorManagerQa = "SubmittedForSectorManagerQa";
        public const string SubmittedForSeniorManagerQa = "SubmittedForSeniorManagerQa";
        public const string Completed = "Completed";
    }

    public static class Qa
    {
        public const string Pending = "Pending";
        public const string InProgress = "InProgress";
        public const string Approved = "Approved";
        public const string ReturnedToValuer = "ReturnedToValuer";
        public const string ReturnedToSectorManager = "ReturnedToSectorManager";
    }

    public static class Ovvio
    {
        public const string Pending = "Pending";
        public const string Inserted = "Inserted";
        public const string Extracted = "Extracted";
        public const string Failed = "Failed";
    }

    public static string Display(string? status)
    {
        var value = status?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return "Not Started";

        return value switch
        {
            Attribute.Submitted => "Submitted",
            Attribute.SectorInbox => "Awaiting Assignment",
            Attribute.Claimed => "Assigned to Processor",
            Attribute.ValuerReview => "Processor Review in Progress",
            Attribute.InspectionRequired => "Physical Inspection Required",
            Attribute.InspectionConfirmed => "Inspection Date Confirmed",
            Attribute.InspectionDetailsSent => "Inspection Details Sent",
            Attribute.InspectionCompleted => "Physical Inspection Completed",
            Attribute.InspectionExpired => "Inspection Request Expired",
            Attribute.ReturnedToClient => "Returned to Client for Correction",
            Attribute.Resubmitted => "Client Resubmitted",
            Attribute.ReturnedToValuer => "Returned to Processor for Correction",
            Attribute.SectorManagerQa => "Sector Manager QA",
            Attribute.SeniorManagerQa => "Senior Manager QA",
            Attribute.Approved => "Final Approval Completed",
            Attribute.ReadyForOvvioExtract => "Approved – Ready for OVVIO",
            Attribute.OvvioInserted => "Approved – Inserted to OVVIO",
            Attribute.OvvioExtracted => "OVVIO Extract Completed",
            Attribute.Rejected => "Rejected",

            Review.InProgress => "Review in Progress",
            Review.SubmittedForSectorManagerQa => "Submitted to Sector Manager QA",
            Review.SubmittedForSeniorManagerQa => "Submitted to Senior Manager QA",
            Review.ReturnedToSectorManager => "Returned to Sector Manager",
            Review.Completed => "Review Completed",

            _ => SplitPascalCase(value)
        };
    }

    public static string QaDisplay(string? status, bool senior = false)
    {
        var value = status?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return "Not Started";

        return value switch
        {
            Qa.Pending => senior ? "Pending Senior Manager QA" : "Pending Sector Manager QA",
            Qa.InProgress => senior ? "Senior Manager QA in Progress" : "Sector Manager QA in Progress",
            Qa.Approved => senior ? "Senior Manager Approved" : "Sector Manager Approved",
            Qa.ReturnedToValuer => "Returned to Processor",
            Qa.ReturnedToSectorManager => "Returned to Sector Manager",
            _ => Display(value)
        };
    }

    public static string OvvioDisplay(string? status)
    {
        var value = status?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return "Not Sent to OVVIO";
        return value switch
        {
            Ovvio.Pending => "Pending OVVIO Insert",
            Ovvio.Inserted => "Inserted to OVVIO",
            Ovvio.Extracted => "OVVIO Extracted",
            Ovvio.Failed => "OVVIO Insert Failed",
            _ => Display(value)
        };
    }

    public static int Priority(string? status) => status?.Trim() switch
    {
        Attribute.ReturnedToValuer => 0,
        Attribute.ReturnedToClient => 1,
        Attribute.Resubmitted => 2,
        Attribute.InspectionExpired => 3,
        Attribute.InspectionRequired => 4,
        Attribute.InspectionConfirmed => 5,
        Attribute.InspectionDetailsSent => 6,
        Attribute.InspectionCompleted => 7,
        Attribute.Claimed => 8,
        Attribute.ValuerReview => 9,
        Attribute.SectorManagerQa => 10,
        Attribute.SeniorManagerQa => 11,
        Attribute.Approved => 12,
        Attribute.ReadyForOvvioExtract => 13,
        Attribute.OvvioInserted => 14,
        Attribute.OvvioExtracted => 15,
        _ => 50
    };

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return System.Text.RegularExpressions.Regex.Replace(value, "(?<=[a-z0-9])(?=[A-Z])", " ");
    }
}
