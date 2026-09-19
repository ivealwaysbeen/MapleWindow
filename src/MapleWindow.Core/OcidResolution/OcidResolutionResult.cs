namespace MapleWindow.Core.OcidResolution;

public enum OcidResolutionStatus { Found, NotFound }

public sealed record OcidResolutionResult(OcidResolutionStatus Status, string? Ocid);
