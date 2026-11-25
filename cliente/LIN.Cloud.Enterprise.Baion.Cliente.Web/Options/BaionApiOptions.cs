namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Options;

public class BaionApiOptions
{
    public const string SectionName = "BaionApi";

    public string BaseAddress { get; set; } = "http://localhost:5199";

    public int TimeoutSeconds { get; set; } = 30;
}
