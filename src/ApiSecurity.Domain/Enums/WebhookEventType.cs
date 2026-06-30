namespace ApiSecurity.Domain.Enums;

[Flags]
public enum WebhookEventType
{
    None = 0,
    ProductCreated = 1,
    ProductUpdated = 2,
    ProductDeleted = 4,
    All = ProductCreated | ProductUpdated | ProductDeleted
}
