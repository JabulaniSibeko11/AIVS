namespace AIVS.Models.Configuration;

public class DemoQaSettings
{
    public bool Enabled { get; set; } = false;
    public bool ForceAllQa { get; set; } = false;
    public bool AutoAssignQaToTestUser { get; set; } = false;
    public bool AllowTestUserAllQaRoles { get; set; } = false;
    public string TestUserWindowsUsername { get; set; } = @"JOBURG\10112533";
    public string TestUserDisplayName { get; set; } = "Jabulani Sibeko";
    public string TestUserEmail { get; set; } = "JabulaniSib@joburg.org.za";
}
