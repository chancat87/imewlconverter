namespace Studyzy.IMEWLConverter.Entities;

public class LlmConfig
{
    public string ApiEndpoint { get; set; } = "https://api.deepseek.com";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "deepseek-v4-flash";
}
