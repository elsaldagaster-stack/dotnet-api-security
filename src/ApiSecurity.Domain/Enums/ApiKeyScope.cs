namespace ApiSecurity.Domain.Enums;

[Flags]
public enum ApiKeyScope
{
    None = 0,
    ReadProducts = 1,
    WriteProducts = 2,
    ManageApiKeys = 4,
    All = ReadProducts | WriteProducts | ManageApiKeys
}
