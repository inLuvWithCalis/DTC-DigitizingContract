namespace ContractManagement.API.Domains.Interfaces.Authentication;

public enum ProfileImageKind
{
    Avatar,
    Cover
}

public sealed record ProfileImageFile(
    Stream Content,
    string ContentType);
