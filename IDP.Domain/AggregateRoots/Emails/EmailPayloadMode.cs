namespace IDP.Domain.AggregateRoots.Emails;

public enum EmailPayloadMode : byte
{
    RenderedBodies = 0, //rendered the email at enqueue time, Worker just sends it, No template lookup needed
    TemplateRef = 1, //The email only stores a reference to a template + model: EmailMessage.TemplateKey, EmailMessage.TemplateModelJson
    Hybrid = 2 //Template reference, AND rendered bodies
}
